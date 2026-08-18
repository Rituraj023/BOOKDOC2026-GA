using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class StakeholderApiTests
{
    [Fact]
    public async Task CorporateStakeholder_UsesSharedDetailsAndRequiresDocumentPermission()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient();
        var tenant = await ProvisionAsync(client);
        using var scope = factory.Services.CreateScope();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var internalTenantId = publicIds.Decode(PublicIdKind.Tenant, tenant.TenantId);
        var documentTypeId = publicIds.Encode(PublicIdKind.DocumentType, NumericId.Next(), internalTenantId);
        var storedFileId = publicIds.Encode(PublicIdKind.StoredFile, NumericId.Next(), internalTenantId);
        var request = new StakeholderCorporateRequest("Acme Diagnostics Pvt Ltd", "Acme Diagnostics", "CIN-TEST-1",
            [new("Mobile", "9876543210", true)], [new("GSTIN", "07TEST1234A1Z1", "GST")],
            [new("REGISTERED", "1 Test Road", null, "Delhi", "DL", "110001", true)],
            [new(documentTypeId, storedFileId, "DOC-1", null, null)]);

        SetTenantHeaders(client, tenant, FoundationPermissions.StakeholdersManage);
        var forbidden = await client.PostAsJsonAsync($"/api/v1/branches/{tenant.BranchId}/stakeholders/corporates", request);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        SetTenantHeaders(client, tenant, FoundationPermissions.StakeholdersManage, FoundationPermissions.StakeholderDocumentsManage);
        var createdResponse = await client.PostAsJsonAsync($"/api/v1/branches/{tenant.BranchId}/stakeholders/corporates", request);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ApiEnvelope<StakeholderResponse>>();
        Assert.NotNull(created);
        Assert.Equal("Corporate", created.Data.Type);
        Assert.Null(created.Data.Person);
        Assert.NotNull(created.Data.Corporate);
        Assert.Single(created.Data.Contacts);
        Assert.Single(created.Data.Identifiers);
        Assert.Single(created.Data.Addresses);
        Assert.Single(created.Data.Documents);

        SetTenantHeaders(client, tenant, FoundationPermissions.StakeholdersView);
        var readResponse = await client.GetAsync($"/api/v1/branches/{tenant.BranchId}/stakeholders/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest("Stakeholder Clinic", $"stakeholder-{Guid.NewGuid():N}", "owner@example.invalid", "Delhi", "DEL01"));
        var submitted = (await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "2001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var response = await client.PostAsJsonAsync($"/api/v1/platform/tenant-applications/{submitted.Data.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Data.Version));
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetTenantHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "2002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[] { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
