using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Application.Patients;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class SqlServerPatientSmokeTests
{
    [Fact]
    public async Task PatientFlow_ExecutesAgainstConfiguredSqlServer()
    {
        var connectionString = Environment.GetEnvironmentVariable("BOOKDOC_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var provider = BuildProvider(connectionString);
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        Assert.True(await dbContext.Database.CanConnectAsync());
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var suffix = Guid.NewGuid().ToString("N")[..10];
        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await foundation.SubmitApplicationAsync(
            new SubmitTenantApplicationRequest(
                $"SQL Test Clinic {suffix}",
                $"sql-test-{suffix}",
                $"sql-{suffix}@example.invalid",
                "Delhi Main",
                $"D{suffix[..5].ToUpperInvariant()}"),
            "ClinicApplicant",
            default);
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var tenantResponse = await foundation.ApproveApplicationAsync(
            publicIds.Decode(PublicIdKind.TenantApplication, application.Id), new(application.Version), default);
        var tenant = TestPublicIds.DecodeTenant(scope.ServiceProvider, tenantResponse);
        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.PatientsRegister,
            FoundationPermissions.PatientsSearch);
        var registered = await patients.RegisterAsync(
            tenant.BranchId,
            new RegisterPatientRequest(
                Guid.NewGuid(),
                new StakeholderPersonRequest(
                    null,
                    "SQL",
                    null,
                    "Patient",
                    new DateOnly(1990, 1, 1),
                    false,
                    "Female"),
                "O+",
                [new StakeholderContactRequest("Mobile", $"98{Random.Shared.NextInt64(10000000, 99999999)}", true)],
                [new StakeholderIdentifierRequest("MRN", suffix, "Clinic")],
                [new StakeholderAddressRequest("PRIMARY", "1 SQL Test Road", null, "Delhi", "DL", "110001", true)],
                [],
                null),
            default);
        var search = await patients.SearchAsync(tenant.BranchId, suffix.ToUpperInvariant(), default);

        Assert.Equal(registered.Id, Assert.Single(search).Id);
        await transaction.RollbackAsync();
    }

    private static ServiceProvider BuildProvider(string connectionString)
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
            options => options.UseSqlServer(connectionString));
        return services.BuildServiceProvider(validateScopes: true);
    }
}
