using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models.Billing;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Infrastructure.Services.Billing;

public class PricingCalculatorService : IPricingCalculatorService
{
    public PricingResult Calculate(PricingRequest request, DateTime? referenceDate = null)
    {
        // Zaman dilimi sorunları yaşamamak için UTC ve gün başlangıcını baz alıyoruz
        DateTime now = (referenceDate ?? DateTime.UtcNow).Date;

        DateTime nextBillingDate;
        decimal proratedAmount = request.BaseAmount;

        #region 1. Alignment (Hizalama) ve Kıstelyevm (Proration) Hesaplaması
        if (request.AlignmentStrategy == BillingAlignmentStrategy.None || request.Interval == BillingInterval.OneTime)
        {
            nextBillingDate = CalculateStandardNextDate(now, request.Interval, request.IntervalCount);
        }
        else
        {
            nextBillingDate = GetNextAlignmentDate(now, request.AlignmentStrategy, request.AlignmentDay);
            DateTime standardNext = CalculateStandardNextDate(now, request.Interval, request.IntervalCount);
            double standardTotalDays = (standardNext - now).TotalDays;
            double daysUntilNext = (nextBillingDate - now).TotalDays;

            if (daysUntilNext > 0 && standardTotalDays > 0)
            {
                decimal ratio = (decimal)(daysUntilNext / standardTotalDays);
                ratio = Math.Min(ratio, 1.0m);
                proratedAmount = request.BaseAmount * ratio;
            }
        }

        // ÇÖZÜM 1: Kıstelyevm tutarını ANINDA 2 haneye yuvarla
        proratedAmount = Math.Round(proratedAmount, 2, MidpointRounding.AwayFromZero);
        #endregion

        #region 2. İndirim (Coupon) Hesaplaması
        decimal discountAmount = 0;
        if (request.CouponType.HasValue && request.CouponValue.HasValue)
        {
            if (request.CouponType.Value == CouponDiscountType.Percentage)
            {
                discountAmount = proratedAmount * (request.CouponValue.Value / 100m);
            }
            else if (request.CouponType.Value == CouponDiscountType.FixedAmount)
            {
                discountAmount = request.CouponValue.Value;
            }
        }

        discountAmount = Math.Min(discountAmount, proratedAmount);

        // ÇÖZÜM 2: İndirim tutarını ANINDA 2 haneye yuvarla
        discountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero);

        // Ara toplam artık tamamen yuvarlanmış temiz 2 sayıdan oluşuyor
        decimal subTotal = proratedAmount - discountAmount;
        #endregion

        #region 3. Vergi (VAT) Hesaplaması
        // ÇÖZÜM 3: Vergiyi ham tutar üzerinden değil, yuvarlanmış subTotal üzerinden hesapla ve anında yuvarla
        decimal taxAmount = Math.Round(subTotal * (request.VatRate / 100m), 2, MidpointRounding.AwayFromZero);
        #endregion

        #region 4. Yuvarlama (Nihai Toplam)
        // ÇÖZÜM 4: Alt toplam ve vergi zaten 2 hane olduğu için direkt topla. Kuruş kayması ihtimali SIFIR.
        decimal finalTotal = subTotal + taxAmount;
        #endregion

        // Nesneye gönderirken tekrar Math.Round yapmaya gerek kalmadı, sayılarımız jilet gibi
        return new PricingResult(
            BaseAmount: request.BaseAmount,
            ProratedAmount: proratedAmount,
            DiscountAmount: discountAmount,
            SubTotal: subTotal,
            TaxAmount: taxAmount,
            FinalTotal: finalTotal,
            NextBillingDate: nextBillingDate
        );
    }

    /// <summary>
    /// Standart döngüler için sonraki tarihi hesaplar (Hizalama olmayan durumlar).
    /// </summary>
    private DateTime CalculateStandardNextDate(DateTime now, BillingInterval interval, int intervalCount)
    {
        return interval switch
        {
            BillingInterval.Month => now.AddMonths(intervalCount),
            BillingInterval.Year => now.AddYears(intervalCount),
            _ => now // OneTime
        };
    }

    /// <summary>
    /// Hizalama (Alignment) stratejisine göre sıradaki ilk fatura tarihini hesaplar.
    /// </summary>
    private DateTime GetNextAlignmentDate(DateTime now, BillingAlignmentStrategy strategy, int fixedDay)
    {
        return strategy switch
        {
            BillingAlignmentStrategy.FixedDay => CalculateFixedDay(now, fixedDay),

            BillingAlignmentStrategy.CalendarQuarter => CalculateCalendarQuarter(now),

            BillingAlignmentStrategy.CalendarHalfYear => now.Month < 7
                ? new DateTime(now.Year, 7, 1, 0, 0, 0, DateTimeKind.Utc)
                : new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),

            BillingAlignmentStrategy.CalendarYear => new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),

            _ => now
        };
    }

    private DateTime CalculateFixedDay(DateTime now, int fixedDay)
    {
        // Eğer bugünün tarihi, hedeflenen günden küçükse bu ayın o günü fatura keseriz.
        // Örn: Bugün ayın 5'i, hedef 15'i ise -> Bu ayın 15'i
        if (now.Day < fixedDay)
        {
            int targetDay = Math.Min(fixedDay, DateTime.DaysInMonth(now.Year, now.Month));
            return new DateTime(now.Year, now.Month, targetDay, 0, 0, 0, DateTimeKind.Utc);
        }

        // Eğer bugünü geçtiysek, sonraki ayın o günü fatura keseriz.
        DateTime nextMonth = now.AddMonths(1);
        int nextTargetDay = Math.Min(fixedDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        return new DateTime(nextMonth.Year, nextMonth.Month, nextTargetDay, 0, 0, 0, DateTimeKind.Utc);
    }

    private DateTime CalculateCalendarQuarter(DateTime now)
    {
        int currentQuarter = (now.Month - 1) / 3 + 1;
        int nextQuarterMonth = (currentQuarter * 3) + 1; // 1, 4, 7, 10

        if (nextQuarterMonth > 12)
            return new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return new DateTime(now.Year, nextQuarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}