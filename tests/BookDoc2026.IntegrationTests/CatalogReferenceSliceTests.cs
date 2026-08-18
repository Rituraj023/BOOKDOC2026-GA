using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Catalog;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class CatalogReferenceSliceTests
{
    [Fact]
    public async Task CtCatalog_ConnectsRequirementsCapabilitiesAndAuditedStatus()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var tenant = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "ct-catalog");
        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.CatalogView,
            FoundationPermissions.CatalogManage,
            FoundationPermissions.ResourcesView,
            FoundationPermissions.ResourcesManage,
            FoundationPermissions.ResourceStatusManage);

        var category = await catalog.CreateCategoryAsync(
            tenant.BranchId,
            new CreateResourceCategoryRequest(null, "CT", "CT Scanner", "ImagingModality"),
            default);
        var service = await catalog.CreateServiceAsync(
            tenant.BranchId,
            new CreateServiceRequest("CT-HEAD", "CT Head", "Non-contrast CT head", 30),
            default);
        var requirement = await catalog.AddRequirementAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.ClinicalService, service.Id, tenant.TenantId),
            new AddServiceResourceRequirementRequest(category.Id, "SCANNER", 1, false),
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
        var capability = await catalog.AddCapabilityAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new AddResourceCapabilityRequest(service.Id, 25, 1),
            default);
        var maintenance = await catalog.ChangeResourceStatusAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new ChangeResourceStatusRequest(resource.Version, "Maintenance", "Scheduled tube inspection"),
            default);
        var listed = await catalog.ListResourcesAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.ResourceCategory, category.Id, tenant.TenantId),
            false,
            default);

        Assert.Equal("SCANNER", requirement.RoleCode);
        Assert.Equal(25, capability.DurationOverrideMinutes);
        Assert.Equal("Maintenance", maintenance.OperationalStatus);
        Assert.Equal("Asia/Kolkata", maintenance.TimeZoneId);
        Assert.Single(listed);
        Assert.Equal(1, await dbContext.ClinicalServices.CountAsync());
        Assert.Equal(1, await dbContext.ResourceCategories.CountAsync());
        Assert.Equal(1, await dbContext.BookableResources.CountAsync());
        Assert.Equal(1, await dbContext.ResourceCapabilities.CountAsync());
        Assert.Equal(1, await dbContext.ServiceResourceRequirements.CountAsync());
        Assert.Equal(1, await dbContext.ResourceStatusEvents.CountAsync());
        Assert.Equal(8, await dbContext.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task Catalog_RejectsDuplicateCodeStaleStatusAndMissingPermission()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var tenant = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "catalog-rules");
        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.CatalogManage,
            FoundationPermissions.ResourcesManage,
            FoundationPermissions.ResourceStatusManage);
        var category = await catalog.CreateCategoryAsync(
            tenant.BranchId,
            new CreateResourceCategoryRequest(null, "ROOM", "Consultation Room", "Space"),
            default);
        await Assert.ThrowsAsync<DomainRuleException>(() => catalog.CreateCategoryAsync(
            tenant.BranchId,
            new CreateResourceCategoryRequest(null, "room", "Duplicate Room", "Space"),
            default));
        var resource = await catalog.CreateResourceAsync(
            tenant.BranchId,
            new CreateBookableResourceRequest(category.Id, "R-01", "Room 1", "Exclusive", 1, null, null),
            default);
        await catalog.ChangeResourceStatusAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new ChangeResourceStatusRequest(resource.Version, "Cleaning", "Turnaround cleaning"),
            default);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => catalog.ChangeResourceStatusAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new ChangeResourceStatusRequest(resource.Version, "Available", "Cleaning complete"),
            default));

        execution.BecomeTenant(tenant.TenantId, [tenant.BranchId]);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            catalog.ListResourcesAsync(tenant.BranchId, null, false, default));
    }

    [Fact]
    public async Task TenantFilter_BlocksCatalogAccessWithForgedBranchClaim()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var first = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "catalog-first");
        var second = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "catalog-second");

        execution.BecomeTenant(second.TenantId, [first.BranchId], FoundationPermissions.CatalogView);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            catalog.ListServicesAsync(first.BranchId, false, default));
    }

    [Fact]
    public async Task ActiveAssignments_BlockUnsafeCategoryRetirementAndCapacityReduction()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var foundation = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var tenant = await ProvisionAsync(scope.ServiceProvider, execution, foundation, "catalog-integrity");
        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.CatalogManage,
            FoundationPermissions.ResourcesManage);
        var category = await catalog.CreateCategoryAsync(
            tenant.BranchId,
            new CreateResourceCategoryRequest(null, "THERAPY-CHAIR", "Therapy Chair", "Bed"),
            default);
        var service = await catalog.CreateServiceAsync(
            tenant.BranchId,
            new CreateServiceRequest("THERAPY", "Therapy Session", null, 45),
            default);
        var resource = await catalog.CreateResourceAsync(
            tenant.BranchId,
            new CreateBookableResourceRequest(category.Id, "CHAIR-POOL", "Therapy Chairs", "Pooled", 10, null, null),
            default);
        await catalog.AddCapabilityAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new AddResourceCapabilityRequest(service.Id, null, 8),
            default);

        await Assert.ThrowsAsync<DomainRuleException>(() => catalog.UpdateResourceAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.BookableResource, resource.Id, tenant.TenantId),
            new UpdateBookableResourceRequest(resource.Version, resource.Name, "Pooled", 5, true),
            default));
        await Assert.ThrowsAsync<DomainRuleException>(() => catalog.UpdateCategoryAsync(
            tenant.BranchId,
            TestPublicIds.Decode(scope.ServiceProvider, PublicIdKind.ResourceCategory, category.Id, tenant.TenantId),
            new UpdateResourceCategoryRequest(category.Version, category.Name, false),
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
            options => options.UseInMemoryDatabase($"bookdoc-catalog-{Guid.NewGuid():N}"));
        return services.BuildServiceProvider(validateScopes: true);
    }
}
