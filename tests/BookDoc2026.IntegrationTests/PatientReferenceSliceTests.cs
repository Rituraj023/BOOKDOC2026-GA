using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Application.Patients;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Domain.Common;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class PatientReferenceSliceTests
{
    [Fact]
    public async Task Registration_IsIdempotentSearchIsMaskedAndUpdateIsVersioned()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var tenant = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "patient-clinic");
        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.PatientsRegister,
            FoundationPermissions.PatientsSearch,
            FoundationPermissions.PatientsView,
            FoundationPermissions.PatientsUpdate);

        var request = NewPatient(Guid.NewGuid(), "9876543210", "clinic-number", "1001");
        var registered = await patients.RegisterAsync(tenant.BranchId, request, default);
        var replay = await patients.RegisterAsync(tenant.BranchId, request, default);
        var changedReplay = request with { Person = request.Person with { FamilyName = "Different" } };
        await Assert.ThrowsAsync<DomainRuleException>(() =>
            patients.RegisterAsync(tenant.BranchId, changedReplay, default));
        var search = await patients.SearchAsync(tenant.BranchId, "9876", default);
        var updated = await patients.UpdateDemographicsAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.Patient, registered.Id, tenant.TenantId),
            new UpdatePatientDemographicsRequest(
                registered.Version,
                registered.StakeholderVersion,
                registered.Person.Version,
                new StakeholderPersonRequest(
                    "Ms",
                    "Ananya",
                    null,
                    "Sharma",
                    new DateOnly(1992, 5, 1),
                    false,
                    "Female"),
                "B+"),
            default);

        Assert.Equal(
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.Patient, registered.Id, tenant.TenantId),
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.Patient, replay.Id, tenant.TenantId));
        Assert.Single(search);
        Assert.Equal("******3210", search.Single().MaskedMobile);
        Assert.Equal(2, updated.Version);
        Assert.Equal(2, updated.StakeholderVersion);
        Assert.Equal(2, updated.Person.Version);
        Assert.Equal(1, await dbContext.Patients.CountAsync());
        Assert.Equal(1, await dbContext.Stakeholders.CountAsync());
        Assert.Equal(1, await dbContext.StakeholderPersons.CountAsync());
        Assert.Equal(2, await dbContext.StakeholderContactPoints.CountAsync());
        Assert.Equal(1, await dbContext.StakeholderIdentifiers.CountAsync());
        Assert.Equal(1, await dbContext.StakeholderAddresses.CountAsync());
        Assert.Equal(2, await dbContext.AuditEvents.CountAsync(audit => audit.Action.StartsWith("Patient")));
    }

    [Fact]
    public async Task PossibleDuplicate_RequiresReasonAndNeverAutoMerges()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientService>();
        var tenant = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "duplicate-clinic");
        execution.BecomeTenant(tenant.TenantId, [tenant.BranchId], FoundationPermissions.PatientsRegister);

        await patients.RegisterAsync(
            tenant.BranchId,
            NewPatient(Guid.NewGuid(), "9999999999", "mrn", "A-1"),
            default);
        var duplicate = NewPatient(Guid.NewGuid(), "9999999999", "mrn", "A-2");
        await Assert.ThrowsAsync<DomainRuleException>(() => patients.RegisterAsync(tenant.BranchId, duplicate, default));
        var overridden = duplicate with { DuplicateOverrideReason = "Verified as a different person at reception." };
        var second = await patients.RegisterAsync(tenant.BranchId, overridden, default);

        Assert.False(string.IsNullOrWhiteSpace(second.Id));
    }

    [Fact]
    public async Task TenantFilter_BlocksPatientFromForgedCrossTenantBranchClaim()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientService>();
        var first = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "patient-first");
        var second = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "patient-second");
        execution.BecomeTenant(first.TenantId, [first.BranchId], FoundationPermissions.PatientsRegister);
        var patient = await patients.RegisterAsync(
            first.BranchId,
            NewPatient(Guid.NewGuid(), "9888888888", "mrn", "F-1"),
            default);

        execution.BecomeTenant(second.TenantId, [first.BranchId], FoundationPermissions.PatientsView);
        await Assert.ThrowsAsync<NotFoundException>(() => patients.GetAsync(
            first.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.Patient, patient.Id, first.TenantId),
            default));
    }

    private static async Task<TestTenant> ProvisionAsync(
        IServiceProvider services,
        TestExecutionContext execution,
        FoundationService foundation,
        string slug)
    {
        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await foundation.SubmitApplicationAsync(
            new SubmitTenantApplicationRequest(
                $"{slug} Legal Name",
                slug,
                $"{slug}@example.invalid",
                "Delhi Main",
                $"D{slug[^3..].ToUpperInvariant()}"),
            "ClinicApplicant",
            default);
        var publicIds = services.GetRequiredService<IPublicIdCodec>();
        var response = await foundation.ApproveApplicationAsync(
            publicIds.Decode(PublicIdKind.TenantApplication, application.Id),
            new(application.Version),
            default);
        return TestPublicIds.DecodeTenant(services, response);
    }

    private static RegisterPatientRequest NewPatient(
        Guid requestId,
        string mobile,
        string identifierType,
        string identifierValue) =>
        new(
            requestId,
            new StakeholderPersonRequest(
                null,
                "Ananya",
                null,
                "Sharma",
                new DateOnly(1992, 5, 1),
                false,
                "Female"),
            "B+",
            [new StakeholderContactRequest("Mobile", mobile, true), new StakeholderContactRequest("Email", "ananya@example.invalid", true)],
            [new StakeholderIdentifierRequest(identifierType, identifierValue, "Clinic")],
            [new StakeholderAddressRequest("PRIMARY", "1 Test Road", null, "Delhi", "DL", "110001", true)],
            [],
            null);

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:BatchSize"] = "20",
                ["Outbox:MaxAttempts"] = "5",
                ["Outbox:LeaseDuration"] = "00:02:00",
                ["Outbox:PollInterval"] = "00:00:01"
            })
            .Build();
        var execution = new TestExecutionContext();
        services.AddLogging();
        services.AddSingleton(execution);
        services.AddSingleton<ICurrentActor>(execution);
        services.AddSingleton<ICorrelationContext>(execution);
        services.AddBookDocApplication();
        services.AddBookDocInfrastructureForTesting(
            configuration,
            options => options.UseInMemoryDatabase($"bookdoc-patient-{Guid.NewGuid():N}"));
        return services.BuildServiceProvider(validateScopes: true);
    }
}
