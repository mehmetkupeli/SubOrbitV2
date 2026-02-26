namespace SubOrbitV2.Domain.Enums;

public enum CouponDuration
{
    Once = 1,      // Sadece ilk faturada
    Repeating = 2, // Belirli bir ay boyunca
    Forever = 3    // İptal edilene kadar her faturada
}