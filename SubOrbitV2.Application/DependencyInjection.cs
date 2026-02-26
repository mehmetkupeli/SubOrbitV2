using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SubOrbitV2.Application.Common.Behaviors;
using System.Reflection;

namespace SubOrbitV2.Application;

/// <summary>
/// Application katmanındaki servislerin, MediatR yapılandırmalarının ve
/// diğer bağımlılıkların merkezi olarak kaydedildiği sınıftır.
/// </summary>
public static class DependencyInjection
{
    #region Application Services Registration

    /// <summary>
    /// Application katmanına ait tüm servisleri DI konteynerine ekler.
    /// </summary>
    /// <param name="services">.NET servis koleksiyonu.</param>
    /// <returns>Yapılandırılmış servis koleksiyonu.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Çalışmakta olan Assembly'i (SubOrbitV2.Application) alıyoruz.
        var assembly = Assembly.GetExecutingAssembly();

        #region MediatR Configuration

        // MediatR servisini ve bu Assembly içindeki tüm Handler'ları (Command/Query/Event) otomatik bulup kaydeder.
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        #endregion

        #region AutoMapper & Validators

        services.AddValidatorsFromAssembly(assembly);

        #endregion

        return services;
    }

    #endregion
}