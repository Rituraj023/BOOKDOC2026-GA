using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Contracts.Workforce;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BookDoc2026.IntegrationTests;

public sealed class DoctorSlotAndBookingRequestApiTests
{
    [Fact]
    public async Task DoctorSlots_GenerateWithBreaks_And_BookDirectInstantConfirmation()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient();
        var tenant = await ProvisionAsync(client);

        var practitioner = await CreatePractitionerAsync(client, tenant);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingAvailabilityManage, FoundationPermissions.SchedulingAvailabilityView);

        var testDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        var generateRequest = new GenerateDoctorSlotsRequest(
            BranchId: tenant.BranchId,
            PractitionerId: practitioner.Id,
            ServiceId: null,
            StartDate: testDate,
            EndDate: testDate,
            DaysOfWeek: [testDate.DayOfWeek],
            ShiftStart: new TimeOnly(9, 0),
            ShiftEnd: new TimeOnly(11, 0),
            SlotDurationMinutes: 15,
            MaxCapacityPerSlot: 1,
            Breaks: [new ShiftBreakWindow(new TimeOnly(10, 0), new TimeOnly(10, 15), "Tea Break")]);

        var genResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/doctor-slots/generate", generateRequest);
        Assert.Equal(HttpStatusCode.OK, genResponse.StatusCode);

        var generatedSlots = (await genResponse.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>>())!.Data;

        // 9:00-11:00 is 8 15-min intervals, minus 1 break interval (10:00-10:15) = 7 slots
        Assert.Equal(7, generatedSlots.Count);
        Assert.DoesNotContain(generatedSlots, s => s.StartUtc.ToString("HH:mm") == "10:00");

        // Public portal queries available slots
        SetPatientHeaders(client, tenant);
        var availableResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/doctor-slots/available?fromDate={testDate:yyyy-MM-dd}&toDate={testDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, availableResponse.StatusCode);
        var availableSlots = (await availableResponse.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>>())!.Data;
        Assert.Equal(7, availableSlots.Count);

        // Instant pre-approved booking
        var targetSlot = availableSlots.First();
        var bookRequest = new BookSlotDirectRequest(
            SlotId: targetSlot.Id,
            PatientId: null,
            PatientFullName: "Anand Verma",
            PatientPhone: "+919876543210",
            PatientEmail: "anand@example.com",
            Notes: "Routine consultation");

        var bookResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/doctor-slots/book-direct", bookRequest);
        Assert.Equal(HttpStatusCode.OK, bookResponse.StatusCode);

        var confirmation = (await bookResponse.Content.ReadFromJsonAsync<ApiEnvelope<DirectSlotBookingConfirmationResponse>>())!.Data;
        Assert.Equal("Confirmed", confirmation.Status);
        Assert.Equal("Anand Verma", confirmation.PatientFullName);
        Assert.False(string.IsNullOrWhiteSpace(confirmation.TokenCode));
        Assert.False(string.IsNullOrWhiteSpace(confirmation.BookingNumber));
    }

    [Fact]
    public async Task BookingRequests_SubmitFromPortal_And_ApproveInERPQueue()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient();
        var tenant = await ProvisionAsync(client);

        var practitioner = await CreatePractitionerAsync(client, tenant);

        // 1. Patient submits booking request from public portal (No auth required)
        SetPatientHeaders(client, tenant);
        var preferredDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3));
        var submitRequest = new SubmitBookingRequest(
            BranchId: tenant.BranchId,
            PatientId: null,
            PatientFullName: "Sunita Roy",
            PatientPhone: "+919123456780",
            PatientEmail: "sunita@example.com",
            PreferredPractitionerId: practitioner.Id,
            ServiceId: null,
            PreferredDate: preferredDate,
            PreferredTimeSlot: "Morning (09:00 AM - 12:00 PM)",
            ReasonForVisit: "Specialist consultation for migraine");

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/booking-requests", submitRequest);
        Assert.Equal(HttpStatusCode.Created, submitResponse.StatusCode);

        var submittedData = (await submitResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingRequestResponse>>())!.Data;
        Assert.Equal("PendingApproval", submittedData.Status);
        Assert.Equal("Sunita Roy", submittedData.PatientFullName);

        // 2. Clinic staff in ERP opens approval queue
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsView, FoundationPermissions.SchedulingBookingsConfirm);

        var queueResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/booking-requests?status=PendingApproval");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);

        var pendingRequests = (await queueResponse.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyCollection<BookingRequestResponse>>>())!.Data;
        var queued = Assert.Single(pendingRequests, r => r.PatientPhone == submitRequest.PatientPhone);

        // 3. Clinic staff approves request
        var approveCommand = new ApproveBookingRequest(
            AssignedPractitionerId: practitioner.Id,
            ConfirmedBookingId: null,
            Notes: "Approved. Please report to Room 201 at 09:30 AM.");

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/booking-requests/{queued.Id}/approve", approveCommand);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approvedData = (await approveResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingRequestResponse>>())!.Data;
        Assert.Equal("Approved", approvedData.Status);
        Assert.Equal("Approved. Please report to Room 201 at 09:30 AM.", approvedData.ReviewNotes);
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest("Doctor Slot Clinic", $"clinic-{Guid.NewGuid():N}", "admin@example.invalid", "Bangalore", "BLR01"));
        var submitted = (await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "5001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var response = await client.PostAsJsonAsync($"/api/v1/platform/tenant-applications/{submitted.Data.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Data.Version));
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static async Task<PractitionerResponse> CreatePractitionerAsync(HttpClient client, TenantProvisioningResponse tenant)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8201");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.UsersManage);
        var identity = await PostData<IdentityUserResponse>(client, "/api/v1/platform/identity/users",
            new CreateIdentityUserRequest("Dr Ramesh Gupta", $"doctor-{Guid.NewGuid():N}@example.invalid", null, "BookDoc!2026-Test"));

        SetHeaders(client, tenant, FoundationPermissions.StakeholdersManage);
        var stakeholder = await PostData<StakeholderResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/stakeholders/persons",
            new CreatePersonStakeholderRequest(
                new StakeholderPersonRequest("Dr", "Ramesh", null, "Gupta", new DateOnly(1980, 5, 10), false, "Male"),
                [], [], [], []));

        SetHeaders(client, tenant, FoundationPermissions.PractitionersManage);
        var practitioner = await PostData<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners",
            new CreatePractitionerRequest(stakeholder.Id, identity.SubjectId, $"DR-{Guid.NewGuid():N}"[..10], "DOCTOR"));

        return practitioner;
    }

    private static async Task<T> PostData<T>(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        Assert.True(response.IsSuccessStatusCode, $"POST to {url} failed with status {response.StatusCode}");
        var env = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>();
        return env!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "5002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void SetPatientHeaders(HttpClient client, TenantProvisioningResponse tenant)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "9001");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[] { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
