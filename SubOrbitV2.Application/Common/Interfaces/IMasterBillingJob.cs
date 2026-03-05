namespace SubOrbitV2.Application.Common.Interfaces;

public interface IMasterBillingJob
{
    // Gece 12'de çalışacak ana tetikleyici
    Task TriggerAllProjectsAsync();
}
