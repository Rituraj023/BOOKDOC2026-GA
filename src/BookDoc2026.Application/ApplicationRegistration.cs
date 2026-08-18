using BookDoc2026.Application.Catalog;
using BookDoc2026.Application.Communications;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Application.Patients;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Application.Stakeholders;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddBookDocApplication(this IServiceCollection services)
    {
        services.AddScoped<FoundationService>();
        services.AddScoped<PatientService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<StakeholderService>();
        services.AddScoped<SchedulingService>();
        services.AddScoped<CommunicationService>();
        services.AddScoped<ProviderCallbackIngressService>();
        return services;
    }
}
