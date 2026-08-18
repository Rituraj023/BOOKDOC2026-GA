using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;

namespace BookDoc2026.IntegrationTests;

public sealed class FoundationApiTests
{
    [Fact]
    public async Task ApiFlow_EnforcesPermissionAndTenantIsolation()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var applicationRequest = new SubmitTenantApplicationRequest(
            "API Clinic",
            "api-clinic",
            "owner@example.invalid",
            "Delhi Main",
            "DEL01");

        var submitResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications", applicationRequest);
        Assert.Equal(HttpStatusCode.Created, submitResponse.StatusCode);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>();
        Assert.NotNull(submitted);

        SetPlatformHeaders(client, FoundationPermissions.TenantsApprove);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Data.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Data.Version));
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var provisioned = await approveResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>();
        Assert.NotNull(provisioned);

        SetTenantHeaders(
            client,
            provisioned.Data.TenantId,
            provisioned.Data.BranchId,
            FoundationPermissions.BranchesView);
        var ownBranchResponse = await client.GetAsync(
            $"/api/v1/branches/{provisioned.Data.BranchId}/configuration");
        Assert.Equal(HttpStatusCode.OK, ownBranchResponse.StatusCode);

        SetTenantHeaders(
            client,
            provisioned.Data.TenantId + "tampered",
            provisioned.Data.BranchId,
            FoundationPermissions.BranchesView);
        var forgedTenantResponse = await client.GetAsync(
            $"/api/v1/branches/{provisioned.Data.BranchId}/configuration");
        Assert.Equal(HttpStatusCode.Unauthorized, forgedTenantResponse.StatusCode);

        SetTenantHeaders(client, provisioned.Data.TenantId, provisioned.Data.BranchId);
        var missingPermissionResponse = await client.GetAsync(
            $"/api/v1/branches/{provisioned.Data.BranchId}/configuration");
        Assert.Equal(HttpStatusCode.Forbidden, missingPermissionResponse.StatusCode);
    }

    private static void SetPlatformHeaders(HttpClient client, params string[] permissions)
    {
        ClearSecurityHeaders(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "1001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void SetTenantHeaders(
        HttpClient client,
        string tenantId,
        string branchId,
        params string[] permissions)
    {
        ClearSecurityHeaders(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "1002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", branchId);
        if (permissions.Length > 0)
        {
            client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
        }
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
