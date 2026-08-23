using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Catalog;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Application.Patients;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class SqlServerSchedulingSmokeTests
{
    [Fact]
    public async Task HoldAndBooking_UseSqlServerTransactionalLocksAndRejectOverlap()
    {
        var connectionString = Environment.GetEnvironmentVariable("BOOKDOC_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var provider = BuildProvider(connectionString);
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientService>();
        var scheduling = scope.ServiceProvider.GetRequiredService<SchedulingService>();
        var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await using var transaction = await db.Database.BeginTransactionAsync();

        var suffix = Guid.NewGuid().ToString("N")[..10];
        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await foundation.SubmitApplicationAsync(new($"SQL Schedule {suffix}", $"sql-schedule-{suffix}",
            $"schedule-{suffix}@example.invalid", "Delhi", $"S{suffix[..5]}"), "Test", default);
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var tenantResponse = await foundation.ApproveApplicationAsync(
            publicIds.Decode(PublicIdKind.TenantApplication, application.Id), new(application.Version), default);
        var tenant = TestPublicIds.DecodeTenant(scope.ServiceProvider, tenantResponse);
        execution.BecomeTenant(tenant.TenantId, [tenant.BranchId], FoundationPermissions.CatalogManage,
            FoundationPermissions.ResourcesManage, FoundationPermissions.PatientsRegister,
            FoundationPermissions.SchedulingAvailabilityManage, FoundationPermissions.SchedulingHoldsCreate,
            FoundationPermissions.SchedulingBookingsConfirm);
        var category = await catalog.CreateCategoryAsync(tenant.BranchId, new(null, "BED", "Bed", "Bed"), default);
        var clinicalService = await catalog.CreateServiceAsync(tenant.BranchId, new("IP-STAY", "Inpatient Stay", null, 60), default);
        var resource = await catalog.CreateResourceAsync(tenant.BranchId,
            new(category.Id, "BED-01", "Bed 01", "Exclusive", 1, "Asia/Kolkata", null), default);
        await catalog.AddCapabilityAsync(
            tenant.BranchId,
            publicIds.Decode(PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new(clinicalService.Id, null, 1),
            default);
        await catalog.AddRequirementAsync(
            tenant.BranchId,
            publicIds.Decode(PublicIdKind.ClinicalService, clinicalService.Id, tenant.TenantId),
            new(category.Id, "Bed", 1, false),
            default);
        var patient = await patients.RegisterAsync(tenant.BranchId, new(Guid.NewGuid(),
            new(null, "SQL", null, "Patient", new DateOnly(1990, 1, 1), false, "Other"), null,
            [new("Mobile", "9876500000", true)], [], [], [], null), default);
        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(3).AddHours(5), TimeSpan.Zero);
        var local = TimeZoneInfo.ConvertTime(start, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        await scheduling.CreateRuleAsync(tenant.BranchId, new(resource.Id, clinicalService.Id, local.DayOfWeek.ToString(),
            new(0, 0), new(23, 59), DateOnly.FromDateTime(local.DateTime), null, 15, 1), default);
        var hold = await scheduling.CreateHoldAsync(tenant.BranchId, new(Guid.NewGuid(), patient.Id, clinicalService.Id,
            start, start.AddMinutes(30), 10, [new(resource.Id, 1, "Bed")]), default);
        Assert.Equal("Active", hold.Status);
        await Assert.ThrowsAsync<BookDoc2026.Domain.Common.ConcurrencyConflictException>(() => scheduling.CreateHoldAsync(
            tenant.BranchId, new(Guid.NewGuid(), patient.Id, clinicalService.Id, start, start.AddMinutes(30), 10,
                [new(resource.Id, 1)]), default));
        var booking = await scheduling.ConfirmHoldAsync(
            tenant.BranchId,
            publicIds.Decode(PublicIdKind.SchedulingHold, hold.Id, tenant.TenantId),
            new ConfirmSchedulingHoldRequest(hold.Version),
            default);
        Assert.Equal("Confirmed", booking.Status);
        Assert.False(booking.IsReplay);
        Assert.Single(await db.Bookings.IgnoreQueryFilters().ToListAsync());
        await transaction.RollbackAsync();
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var execution = new TestExecutionContext();
        services.AddLogging();
        services.AddSingleton(execution);
        services.AddSingleton<ICurrentActor>(execution);
        services.AddSingleton<ICorrelationContext>(execution);
        services.AddBookDocApplication();
        services.AddBookDocInfrastructureForTesting(configuration, options => options.UseSqlServer(connectionString));
        return services.BuildServiceProvider(validateScopes: true);
    }
}
