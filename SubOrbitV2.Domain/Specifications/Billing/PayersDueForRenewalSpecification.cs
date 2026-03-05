using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Specifications.Billing;

/// <summary>
/// Gece yarısı Hangfire motoru (Recurring Engine) tarafından kullanılır.
/// Sadece durumu Aktif olan, kayıtlı kartı (NexiCustomerId) bulunan ve
/// faturası kesilecek en az 1 aktif aboneliği olan müşterileri (Payer) getirir.
/// </summary>
public class PayersDueForRenewalSpecification : BaseSpecification<Payer>
{
    public PayersDueForRenewalSpecification(Guid projectId, DateTime targetDate)
        : base(p => p.ProjectId == projectId
                 && p.Status == PayerStatus.Active
                 && !string.IsNullOrEmpty(p.NexiCustomerId)
                 && p.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active && s.NextBillingDate.Date <= targetDate.Date))
    {
        // İlgili abonelikleri hafızaya alıyoruz (Fatura kalemi oluşturmak için şart)
        AddInclude(p => p.Subscriptions);

        // Aboneliklerin bağlı olduğu Price (Fiyat/Döngü) bilgilerini de alıyoruz
        AddInclude($"{nameof(Payer.Subscriptions)}.{nameof(Subscription.Price)}");

        // Kuponları temizlemek ve indirimleri hesaplamak için ActiveCoupon bilgisini de çekiyoruz
        AddInclude($"{nameof(Payer.Subscriptions)}.{nameof(Subscription.ActiveCoupon)}");
    }
}