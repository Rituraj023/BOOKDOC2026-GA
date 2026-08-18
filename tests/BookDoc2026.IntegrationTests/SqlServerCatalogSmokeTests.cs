using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Catalog;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class SqlServerCatalogSmokeTests
{
    [Fact]
    public async Task CtResourceFlow_ExecutesAgainstConfiguredSqlServer()
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
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        Assert.True(await dbContext.Database.CanConnectAsync());
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var suffix = Guid.NewGuid().ToString("N")[..10];
        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await foundation.SubmitApplicationAsync(
            new SubmitTenantApplicationRequest(
                $"SQL Catalog Clinic {suffix}",
                $"sql-catalog-{suffix}",
                $"catalog-{suffix}@example.invalid",
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
            FoundationPermissions.CatalogManage,
            FoundationPermissions.ResourcesManage,
            FoundationPermissions.ResourcesView,
            FoundationPermissions.ResourceStatusManage);
        var category = await catalog.CreateCategoryAsync(
            tenant.BranchId,
            new CreateResourceCategoryRequest(null, "CT", "CT Scanner", "ImagingModality"),
            default);
        var service = await catalog.CreateServiceAsync(
            tenant.BranchId,
            new CreateServiceRequest("CT-HEAD", "CT Head", null, 30),
            default);
        var resource = await catalog.CreateResourceAsync(
            tenant.BranchId,
            new CreateBookableResourceRequest(
                category.Id,
                "CT-01",
                "CT Scanner 1",
                "Exclusive",
                1,
                null,
                null),
            default);
        await catalog.AddCapabilityAsync(
            tenant.BranchId,
            publicIds.Decode(PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new AddResourceCapabilityRequest(service.Id, 25, 1),
            default);
        var maintenance = await catalog.ChangeResourceStatusAsync(
            tenant.BranchId,
            publicIds.Decode(PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new ChangeResourceStatusRequest(resource.Version, "Maintenance", "SQL smoke inspection"),
            default);
        var listed = await catalog.ListResourcesAsync(
            tenant.BranchId,
            publicIds.Decode(PublicIdKind.ResourceCategory, category.Id, tenant.TenantId),
            false,
            default);

        Assert.Equal("Maintenance", maintenance.OperationalStatus);
        Assert.Equal(resource.Id, Assert.Single(listed).Id);
        Assert.Equal(1, await dbContext.ResourceStatusEvents.CountAsync());
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
