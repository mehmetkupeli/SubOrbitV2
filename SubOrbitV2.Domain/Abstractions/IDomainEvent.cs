using MediatR;

namespace SubOrbitV2.Domain.Abstractions;

/// <summary>
/// Sistem genelindeki tüm Domain Event'lerin (İş Alanı Olayları) atasıdır.
/// MediatR kütüphanesi ile haberleşebilmesi için INotification arayüzünden türer.
/// Bu arayüzü uygulayan sınıflar, sistem içinde fırlatılabilir olaylar haline gelir.
/// </summary>
public interface IDomainEvent : INotification
{
    // Marker Interface: Şu an için içi boş, imza niyetine kullanıyoruz.
}