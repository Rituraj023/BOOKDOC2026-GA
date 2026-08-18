using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.ErrorHandling;

namespace BookDoc2026.IntegrationTests;

public sealed class PatientApiTests
{
    [Fact]
    public async Task PatientApi_RegistersSearchesReadsAndEnforcesPermissionAndVersion()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);
        var request = new RegisterPatientRequest(
            Guid.NewGuid(),
            new StakeholderPersonRequest(
                "Ms",
                "Ananya",
                null,
                "Sharma",
                new DateOnly(1992, 5, 1),
                false,
                "Female"),
            "B+",
            [new StakeholderContactRequest("Mobile", "9876543210", true)],
            [new StakeholderIdentifierRequest("MRN", "API-1001", "Clinic")],
            [new StakeholderAddressRequest("PRIMARY", "1 API Road", null, "Delhi", "DL", "110001", true)],
            [],
            null);

        SetTenantHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var registerResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/patients",
            request);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<ApiEnvelope<PatientResponse>>();
        Assert.NotNull(registered);

        SetTenantHeaders(client, tenant, FoundationPermissions.PatientsSearch);
        var searchResponse = await client.GetAsync($"/api/v1/branches/{tenant.BranchId}/patients?q=9876");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var search = await searchResponse.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyCollection<PatientSearchResponse>>>();
        Assert.Equal("******3210", Assert.Single(search!.Data).MaskedMobile);

        SetTenantHeaders(client, tenant);
        var forbidden = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/patients/{registered.Data.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        SetTenantHeaders(client, tenant, FoundationPermissions.PatientsUpdate);
        var stale = await client.PutAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/patients/{registered.Data.Id}/demographics",
            new UpdatePatientDemographicsRequest(
                99,
                registered.Data.StakeholderVersion,
                registered.Data.Person.Version,
                new StakeholderPersonRequest(
                    "Ms",
                    "Ananya",
                    null,
                    "Sharma",
                    new DateOnly(1992, 5, 1),
                    false,
                    "Female"),
                "B+"));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var staleError = await stale.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(staleError);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, staleError.Code);
        Assert.Equal(ErrorCategory.Conflict, staleError.Category);
        Assert.False(string.IsNullOrWhiteSpace(staleError.TraceId));
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var application = new SubmitTenantApplicationRequest(
            "Patient API Clinic",
            "patient-api-clinic",
            "owner@example.invalid",
            "Delhi Main",
            "DEL01");
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications", application);
        var submitted = await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>();
        Assert.NotNull(submitted);

        ClearSecurityHeaders(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "3001");
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
        client.DefaultRequestHeaders.Add("X-User-Id", "3002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId.ToString());
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
