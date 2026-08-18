using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;

namespace BookDoc2026.IntegrationTests;

public sealed class CatalogApiTests
{
    [Fact]
    public async Task CatalogApi_CreatesCtResourceListsItAndEnforcesStatusPermission()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);
        SetTenantHeaders(
            client,
            tenant,
            FoundationPermissions.CatalogManage,
            FoundationPermissions.ResourcesManage);
        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "CT", "CT Scanner", "ImagingModality"));
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<ApiEnvelope<ResourceCategoryResponse>>();
        Assert.NotNull(category);
        var serviceResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("CT-HEAD", "CT Head", null, 30));
        Assert.Equal(HttpStatusCode.Created, serviceResponse.StatusCode);
        var service = await serviceResponse.Content.ReadFromJsonAsync<ApiEnvelope<ServiceResponse>>();
        Assert.NotNull(service);
        var resourceResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(
                category.Data.Id,
                "CT-01",
                "CT Scanner 1",
                "Exclusive",
                1,
                null,
                null));
        Assert.Equal(HttpStatusCode.Created, resourceResponse.StatusCode);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookableResourceResponse>>();
        Assert.NotNull(resource);
        var capabilityResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Data.Id}/capabilities",
            new AddResourceCapabilityRequest(service.Data.Id, 25, 1));
        Assert.Equal(HttpStatusCode.Created, capabilityResponse.StatusCode);

        SetTenantHeaders(client, tenant, FoundationPermissions.ResourcesView);
        var listResponse = await client.GetAsync($"/api/v1/branches/{tenant.BranchId}/resources");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyCollection<BookableResourceResponse>>>();
        Assert.Equal("ImagingModality", Assert.Single(list!.Data).CategoryKind);
        var forbiddenStatus = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Data.Id}/status",
            new ChangeResourceStatusRequest(resource.Data.Version, "Maintenance", "Scheduled inspection"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenStatus.StatusCode);

        SetTenantHeaders(client, tenant, FoundationPermissions.ResourceStatusManage);
        var statusResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Data.Id}/status",
            new ChangeResourceStatusRequest(resource.Data.Version, "Maintenance", "Scheduled inspection"));
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var application = new SubmitTenantApplicationRequest(
            "Catalog API Clinic",
            "catalog-api-clinic",
            "owner@example.invalid",
            "Delhi Main",
            "DEL01");
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications", application);
        var submitted = await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>();
        Assert.NotNull(submitted);
        ClearSecurityHeaders(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "4001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var approvedResponse = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Data.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Data.Version));
        var approved = await approvedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>();
        return Assert.IsType<ApiEnvelope<TenantProvisioningResponse>>(approved).Data;
    }

    private static void SetTenantHeaders(
        HttpClient client,
        TenantProvisioningResponse tenant,
        params string[] permissions)
    {
        ClearSecurityHeaders(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "4002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId.ToString());
        client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void ClearSecurityHeaders(HttpClient client)
    {
        foreach (var header in new[]
        {
            "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator"
        })
        {
            client.DefaultRequestHeaders.Remove(header);
        }
    }
}
