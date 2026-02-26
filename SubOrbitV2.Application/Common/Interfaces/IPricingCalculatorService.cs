using SubOrbitV2.Application.Common.Models.Billing;

namespace SubOrbitV2.Application.Common.Interfaces;

public interface IPricingCalculatorService
{
    /// <summary>
    /// Sepetteki ürünün; hizalama (alignment), indirim (coupon) ve vergi (tax) 
    /// hesaplamalarını yaparak Nexi'ye gönderilecek nihai tutarı ve sonraki fatura tarihini hesaplar.
    /// </summary>
    PricingResult Calculate(PricingRequest request, DateTime? referenceDate = null);
}