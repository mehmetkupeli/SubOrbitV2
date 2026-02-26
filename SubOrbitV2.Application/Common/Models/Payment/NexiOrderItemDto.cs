namespace SubOrbitV2.Application.Common.Models.Payment;
public class NexiOrderItemDto
{
    public string NexiReference { get; set; } = string.Empty; // A reference to recognize the product, usually the SKU (stock keeping unit) of the product.
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = "pcs";

    public int UnitPrice { get; set; }        // KDV Hariç Birim Fiyat
    public int TaxRate { get; set; }          // KDV Oranı (Basis Points: %20 -> 2000)
    public int TaxAmount { get; set; }        // Toplam KDV Tutarı
    public int GrossTotalAmount { get; set; } // KDV Dahil Toplam Tutar (Ödenecek)
    public int NetTotalAmount { get; set; }   // KDV Hariç Toplam Tutar


    //Ek alanlar
    public string Currency { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public string TermsUrl { get; set; } = "";
    public string SubscriptionReference { get; set; } = "";

}