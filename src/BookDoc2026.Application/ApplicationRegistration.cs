using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Billing;
using BookDoc2026.Application.Catalog;
using BookDoc2026.Application.Communications;
using BookDoc2026.Application.Clinical;
using BookDoc2026.Application.Contracts;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Application.Patients;
using BookDoc2026.Application.Queues;
using BookDoc2026.Application.Radiology;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Application.Stakeholders;
using BookDoc2026.Application.Workforce;
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
        services.AddScoped<SlotManagementService>();
        services.AddScoped<ISlotManagementService>(sp => sp.GetRequiredService<SlotManagementService>());
        services.AddScoped<BookingRequestService>();
        services.AddScoped<IBookingRequestService>(sp => sp.GetRequiredService<BookingRequestService>());
        services.AddScoped<QueueService>();
        services.AddScoped<CommunicationService>();
        services.AddScoped<ContractService>();
        services.AddScoped<EncounterService>();
        services.AddScoped<InvestigationService>();
        services.AddScoped<RadiologyStudyService>();
        services.AddScoped<PhysiotherapyService>();
        services.AddScoped<PractitionerService>();
        services.AddScoped<IPractitionerEligibility>(services => services.GetRequiredService<PractitionerService>());
        services.AddScoped<BillingService>();
        services.AddScoped<ProviderCallbackIngressService>();
        return services;
    }
}
