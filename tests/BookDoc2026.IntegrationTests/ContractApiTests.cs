using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Contracts;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;

namespace BookDoc2026.IntegrationTests;

public sealed class ContractApiTests
{
    [Fact]
    public async Task EntitlementLedger_IsAuthorizedIdempotentAndBookingAware()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);

        SetHeaders(client, tenant, FoundationPermissions.CatalogManage, FoundationPermissions.ResourcesManage);
        var category = await PostCreated<ResourceCategoryResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "PHYSIO", "Physiotherapy room", "Space"));
        var service = await PostCreated<ServiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("PHYSIO-SESSION", "Physiotherapy session", null, 30));
        var resource = await PostCreated<BookableResourceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(category.Id, "PHY-01", "Physio Room 1", "Exclusive", 1,
                "Asia/Kolkata", null));
        _ = await PostCreated<ResourceCapabilityResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Id}/capabilities",
            new AddResourceCapabilityRequest(service.Id, null, 1));
        _ = await PostCreated<ServiceResourceRequirementResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services/{service.Id}/resource-requirements",
            new AddServiceResourceRequirementRequest(category.Id, "TreatmentRoom", 1, false));

        SetHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var patient = await PostCreated<PatientResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/patients",
            new RegisterPatientRequest(Guid.NewGuid(),
                new(null, "Contract", null, "Patient", new DateOnly(1990, 1, 1), false, "Other"),
                null, [new("Email", "contract.patient@example.invalid", true)], [], [], [], null));
        var booking = await CreateBookingAsync(client, tenant, patient, service, resource);

        var create = new CreateContractRequest(patient.Id, "CNT-2026-0001", "PHYSIO",
            DateOnly.FromDateTime(DateTime.UtcNow.Date), DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(2)),
            5000m, "INR", "physio-v1", "Ten-session package",
            [new(service.Id, category.Id, 10, 500m, "physio-v1")]);

        SetHeaders(client, tenant, FoundationPermissions.ContractsView);
        var forbiddenCreate = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts", create);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.ContractsManage);
        var contract = await PostCreated<ContractResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/contracts", create);
        var entitlement = Assert.Single(contract.Entitlements);
        Assert.DoesNotMatch("^[0-9]+$", contract.Id);
        Assert.Equal(10, entitlement.AvailableUnits);

        var requestId = Guid.NewGuid();
        var reserveRequest = new ReserveEntitlementRequest(requestId, booking.Id, 2, entitlement.Version);
        SetHeaders(client, tenant, FoundationPermissions.ContractEntitlementsReserve);
        var reservedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts/{contract.Id}/entitlements/{entitlement.Id}/reservations",
            reserveRequest);
        Assert.Equal(HttpStatusCode.Created, reservedResponse.StatusCode);
        var reserved = (await reservedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<EntitlementReservationResponse>>())!.Data;
        Assert.Equal("Reserved", reserved.Status);
        Assert.Equal(8, reserved.Entitlement.AvailableUnits);

        var replayResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts/{contract.Id}/entitlements/{entitlement.Id}/reservations",
            reserveRequest);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.True((await replayResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<EntitlementReservationResponse>>())!.Data.IsReplay);

        var changedReplay = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts/{contract.Id}/entitlements/{entitlement.Id}/reservations",
            reserveRequest with { Units = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, changedReplay.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.ContractsView);
        var forbiddenConsume = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts/entitlement-reservations/{reserved.Id}/consume",
            new ConsumeEntitlementReservationRequest(reserved.Version, reserved.Entitlement.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenConsume.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.ContractEntitlementsConsume);
        var consumedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts/entitlement-reservations/{reserved.Id}/consume",
            new ConsumeEntitlementReservationRequest(reserved.Version, reserved.Entitlement.Version));
        Assert.Equal(HttpStatusCode.OK, consumedResponse.StatusCode);
        var consumed = (await consumedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<EntitlementReservationResponse>>())!.Data;
        Assert.Equal("Consumed", consumed.Status);
        Assert.Equal(2, consumed.Entitlement.ConsumedUnits);
        Assert.Equal(0, consumed.Entitlement.ReservedUnits);

        var staleConsume = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/contracts/entitlement-reservations/{reserved.Id}/consume",
            new ConsumeEntitlementReservationRequest(reserved.Version, reserved.Entitlement.Version));
        Assert.Equal(HttpStatusCode.Conflict, staleConsume.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.ContractsView);
        var loaded = await client.GetFromJsonAsync<ApiEnvelope<ContractResponse>>(
            $"/api/v1/branches/{tenant.BranchId}/contracts/{contract.Id}");
        Assert.Equal(2, Assert.Single(loaded!.Data.Entitlements).ConsumedUnits);
    }

    private static async Task<BookingResponse> CreateBookingAsync(HttpClient client,
        TenantProvisioningResponse tenant, PatientResponse patient, ServiceResponse service,
        BookableResourceResponse resource)
    {
        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(5), TimeSpan.Zero);
        var local = TimeZoneInfo.ConvertTime(startUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingAvailabilityManage);
        _ = await PostCreated<AvailabilityRuleResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/availability-rules",
            new CreateAvailabilityRuleRequest(resource.Id, service.Id, local.DayOfWeek.ToString(),
                new TimeOnly(0, 0), new TimeOnly(23, 59), DateOnly.FromDateTime(local.DateTime), null, 15, 1));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingHoldsCreate);
        var hold = await PostCreated<SchedulingHoldResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds",
            new CreateSchedulingHoldRequest(Guid.NewGuid(), patient.Id, service.Id, startUtc,
                startUtc.AddMinutes(30), 10, [new(resource.Id, 1, "TreatmentRoom")]));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsConfirm);
        return await PostCreated<BookingResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{hold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(hold.Version));
    }

    private static async Task<T> PostCreated<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>())!.Data;
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest("Contract Clinic", $"contract-{Guid.NewGuid():N}",
                "owner@example.invalid", "Delhi Main", "DEL01"));
        var submitted = (await submittedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!.Data;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8101");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var approved = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Version));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        return (await approved.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8102");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        if (permissions.Length > 0)
            client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[]
                 { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
