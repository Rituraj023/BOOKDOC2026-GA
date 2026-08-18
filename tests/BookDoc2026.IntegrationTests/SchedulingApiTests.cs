using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class SchedulingApiTests
{
    [Fact]
    public async Task SchedulingApi_FindsAvailabilityMakesIdempotentHoldPreventsOverlapAndReleases()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient();
        var tenant = await ProvisionAsync(client);
        using var scope = factory.Services.CreateScope();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var internalTenantId = publicIds.Decode(PublicIdKind.Tenant, tenant.TenantId);
        SetHeaders(client, tenant, FoundationPermissions.CatalogManage, FoundationPermissions.ResourcesManage);
        var category = await PostData<ResourceCategoryResponse>(client, $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "CT", "CT Scanner", "ImagingModality"));
        var service = await PostData<ServiceResponse>(client, $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("CT-HEAD", "CT Head", null, 30));
        var resource = await PostData<BookableResourceResponse>(client, $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(category.Id, "CT-01", "CT Scanner 1", "Exclusive", 1, "Asia/Kolkata", null));
        _ = await PostData<ResourceCapabilityResponse>(client, $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Id}/capabilities",
            new AddResourceCapabilityRequest(service.Id, null, 1));

        SetHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var patient = await PostData<PatientResponse>(client, $"/api/v1/branches/{tenant.BranchId}/patients",
            new RegisterPatientRequest(Guid.NewGuid(), new(null, "Test", null, "Patient", new DateOnly(1990, 1, 1), false, "Other"), null,
                [new("Mobile", "9876543210", true)], [], [], [], null));

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(5), TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(30);
        var local = TimeZoneInfo.ConvertTime(startUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingAvailabilityManage);
        _ = await PostData<AvailabilityRuleResponse>(client, $"/api/v1/branches/{tenant.BranchId}/scheduling/availability-rules",
            new CreateAvailabilityRuleRequest(resource.Id, service.Id, local.DayOfWeek.ToString(), new TimeOnly(0, 0), new TimeOnly(23, 59),
                DateOnly.FromDateTime(local.DateTime), null, 15, 1));

        SetHeaders(client, tenant, FoundationPermissions.SchedulingAvailabilityView);
        var query = $"?serviceId={service.Id}&startUtc={Uri.EscapeDataString(startUtc.ToString("O"))}&endUtc={Uri.EscapeDataString(endUtc.ToString("O"))}&quantity=1";
        var availabilityResponse = await client.GetAsync($"/api/v1/branches/{tenant.BranchId}/scheduling/availability{query}");
        Assert.Equal(HttpStatusCode.OK, availabilityResponse.StatusCode);
        var availability = await availabilityResponse.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyCollection<AvailabilityResourceResponse>>>();
        Assert.True(Assert.Single(availability!.Data).IsAvailable);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingHoldsCreate);
        var requestId = Guid.NewGuid();
        var request = new CreateSchedulingHoldRequest(requestId, patient.Id, service.Id, startUtc, endUtc, 10, [new(resource.Id, 1)]);
        var first = await PostData<SchedulingHoldResponse>(client, $"/api/v1/branches/{tenant.BranchId}/scheduling/holds", request);
        var replay = await PostData<SchedulingHoldResponse>(client, $"/api/v1/branches/{tenant.BranchId}/scheduling/holds", request);
        Assert.Equal(
            publicIds.Decode(PublicIdKind.SchedulingHold, first.Id, internalTenantId),
            publicIds.Decode(PublicIdKind.SchedulingHold, replay.Id, internalTenantId));
        var competing = await client.PostAsJsonAsync($"/api/v1/branches/{tenant.BranchId}/scheduling/holds",
            request with { RequestId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, competing.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingHoldsRelease);
        var releasedResponse = await client.PostAsJsonAsync($"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{first.Id}/release",
            new ReleaseSchedulingHoldRequest(first.Version));
        Assert.Equal(HttpStatusCode.OK, releasedResponse.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingHoldsCreate);
        var afterRelease = await client.PostAsJsonAsync($"/api/v1/branches/{tenant.BranchId}/scheduling/holds",
            request with { RequestId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, afterRelease.StatusCode);
    }

    private static async Task<T> PostData<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>())!.Data;
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest("Scheduling Clinic", $"scheduling-{Guid.NewGuid():N}", "owner@example.invalid", "Delhi", "DEL01"));
        var submitted = (await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "5001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var response = await client.PostAsJsonAsync($"/api/v1/platform/tenant-applications/{submitted.Data.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Data.Version));
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "5002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId.ToString());
        client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[] { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
