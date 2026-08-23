using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Communications;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class BookingApiTests
{
    [Fact]
    public async Task BookingConfirmation_IsAtomicIdempotentRequirementAwareAndPreferenceControlled()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);

        SetHeaders(client, tenant, FoundationPermissions.CatalogManage, FoundationPermissions.ResourcesManage);
        var category = await PostCreated<ResourceCategoryResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "CT", "CT Scanner", "ImagingModality"));
        var service = await PostCreated<ServiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("CT-HEAD", "CT Head", null, 30));
        var resource = await PostCreated<BookableResourceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(
                category.Id, "CT-01", "CT Scanner 1", "Exclusive", 1, "Asia/Kolkata", null));
        _ = await PostCreated<ResourceCapabilityResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Id}/capabilities",
            new AddResourceCapabilityRequest(service.Id, null, 1));
        _ = await PostCreated<ServiceResourceRequirementResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services/{service.Id}/resource-requirements",
            new AddServiceResourceRequirementRequest(category.Id, "Scanner", 1, false));

        SetHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var patient = await PostCreated<PatientResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/patients",
            new RegisterPatientRequest(
                Guid.NewGuid(),
                new(null, "Booking", null, "Patient", new DateOnly(1990, 1, 1), false, "Other"),
                null,
                [new("Email", "booking.patient@example.invalid", true)],
                [], [], [], null));

        await RecordPreferenceAsync(client, tenant, patient.StakeholderId, "Allowed");
        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(5), TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(30);
        var local = TimeZoneInfo.ConvertTime(startUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingAvailabilityManage);
        _ = await PostCreated<AvailabilityRuleResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/availability-rules",
            new CreateAvailabilityRuleRequest(
                resource.Id,
                service.Id,
                local.DayOfWeek.ToString(),
                new TimeOnly(0, 0),
                new TimeOnly(23, 59),
                DateOnly.FromDateTime(local.DateTime),
                null,
                15,
                1));

        var firstHold = await CreateHoldAsync(client, tenant, patient, service, resource, startUtc);
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsView);
        var forbidden = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{firstHold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(firstHold.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        SetHeaders(client, tenant,
            FoundationPermissions.SchedulingBookingsConfirm,
            FoundationPermissions.SchedulingBookingsView);
        var confirmedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{firstHold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(firstHold.Version));
        Assert.Equal(HttpStatusCode.Created, confirmedResponse.StatusCode);
        var confirmed = (await confirmedResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingResponse>>())!.Data;
        Assert.Equal("Confirmed", confirmed.Status);
        Assert.False(confirmed.IsReplay);
        Assert.True(confirmed.NotificationQueued);
        Assert.Equal("SCANNER", Assert.Single(confirmed.Resources).RequirementRoleCode);

        var replayResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{firstHold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(firstHold.Version));
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replay = (await replayResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingResponse>>())!.Data;
        Assert.True(replay.IsReplay);
        Assert.Equal(confirmed.BookingNumber, replay.BookingNumber);

        var getResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/bookings/{confirmed.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
            Assert.Equal(2, await processor.ProcessBatchAsync(default));
            Assert.Equal(0, await processor.ProcessBatchAsync(default));
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            Assert.Single(await db.Bookings.IgnoreQueryFilters().ToListAsync());
            Assert.Single(await db.BookingResourceAllocations.IgnoreQueryFilters().ToListAsync());
            Assert.Single(await db.OutboxMessages.IgnoreQueryFilters()
                .Where(message => message.MessageType == BookingConfirmedOutboxPayload.MessageType)
                .ToListAsync());
            var bookingAttempt = Assert.Single(await db.MessageDeliveryAttempts.IgnoreQueryFilters()
                .Where(attempt => attempt.TemplateKey == BookingConfirmedOutboxPayload.TemplateKey)
                .ToListAsync());
            Assert.Equal(MessageDeliveryStatus.Accepted, bookingAttempt.Status);
            Assert.DoesNotContain("booking.patient", bookingAttempt.RecipientHint, StringComparison.OrdinalIgnoreCase);
        }

        await RecordPreferenceAsync(client, tenant, patient.StakeholderId, "Allowed", "Booking.Updates");
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsView);
        var forbiddenCancel = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/bookings/{confirmed.Id}/cancel",
            new CancelBookingRequest(confirmed.Version, "Patient requested cancellation"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCancel.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsCancel);
        var cancelledResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/bookings/{confirmed.Id}/cancel",
            new CancelBookingRequest(confirmed.Version, "Patient requested cancellation"));
        Assert.Equal(HttpStatusCode.OK, cancelledResponse.StatusCode);
        var cancelled = (await cancelledResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingResponse>>())!.Data;
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(2, cancelled.Version);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingWaitlistManage);
        var waitlist = await PostCreated<BookingWaitlistResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist",
            new CreateBookingWaitlistRequest(patient.Id, service.Id, startUtc, startUtc, 1, "Cancellation opening requested"));
        var withdrawable = await PostCreated<BookingWaitlistResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist",
            new CreateBookingWaitlistRequest(patient.Id, service.Id, startUtc.AddHours(4), startUtc.AddHours(5), 3,
                "Backup request no longer needed"));
        var withdrawnResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist/{withdrawable.Id}/withdraw",
            new WithdrawBookingWaitlistRequest(withdrawable.Version, "Patient declined the backup request"));
        Assert.Equal(HttpStatusCode.OK, withdrawnResponse.StatusCode);
        var withdrawn = (await withdrawnResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingWaitlistResponse>>())!.Data;
        Assert.Equal("Withdrawn", withdrawn.Status);
        var staleWithdrawal = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist/{withdrawable.Id}/withdraw",
            new WithdrawBookingWaitlistRequest(withdrawable.Version, "Duplicate stale withdrawal"));
        Assert.Equal(HttpStatusCode.Conflict, staleWithdrawal.StatusCode);
        var promotionHold = await CreateHoldAsync(client, tenant, patient, service, resource, startUtc);
        SetHeaders(client, tenant, FoundationPermissions.SchedulingWaitlistView);
        var forbiddenPromotion = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist/{waitlist.Id}/promote",
            new PromoteBookingWaitlistRequest(promotionHold.Id, waitlist.Version, promotionHold.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPromotion.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.SchedulingWaitlistManage);
        var promotedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist/{waitlist.Id}/promote",
            new PromoteBookingWaitlistRequest(promotionHold.Id, waitlist.Version, promotionHold.Version));
        Assert.Equal(HttpStatusCode.Created, promotedResponse.StatusCode);
        var promoted = (await promotedResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingResponse>>())!.Data;
        Assert.False(string.IsNullOrWhiteSpace(promoted.WaitlistEntryId));
        var promotionReplay = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/waitlist/{waitlist.Id}/promote",
            new PromoteBookingWaitlistRequest(promotionHold.Id, waitlist.Version, promotionHold.Version));
        Assert.Equal(HttpStatusCode.OK, promotionReplay.StatusCode);

        var replacementHold = await CreateHoldAsync(client, tenant, patient, service, resource, startUtc.AddHours(3));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsView);
        var forbiddenReschedule = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/bookings/{promoted.Id}/reschedule",
            new RescheduleBookingRequest(replacementHold.Id, promoted.Version, replacementHold.Version,
                "Patient requested a later time"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenReschedule.StatusCode);
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsReschedule);
        var rescheduledResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/bookings/{promoted.Id}/reschedule",
            new RescheduleBookingRequest(replacementHold.Id, promoted.Version, replacementHold.Version,
                "Patient requested a later time"));
        Assert.Equal(HttpStatusCode.Created, rescheduledResponse.StatusCode);
        var rescheduled = (await rescheduledResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingResponse>>())!.Data;
        Assert.False(string.IsNullOrWhiteSpace(rescheduled.PreviousBookingId));
        Assert.Equal("Confirmed", rescheduled.Status);
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsView);
        var oldBookingResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/bookings/{promoted.Id}");
        var oldBooking = (await oldBookingResponse.Content.ReadFromJsonAsync<ApiEnvelope<BookingResponse>>())!.Data;
        Assert.Equal("Cancelled", oldBooking.Status);
        Assert.False(string.IsNullOrWhiteSpace(oldBooking.ReplacedByBookingId));

        using (var scope = factory.Services.CreateScope())
        {
            Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(default));
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            var lifecycleAttempts = await db.MessageDeliveryAttempts.IgnoreQueryFilters()
                .Where(attempt => attempt.TemplateKey == BookingLifecycleOutboxPayload.CancelledTemplateKey
                    || attempt.TemplateKey == BookingLifecycleOutboxPayload.RescheduledTemplateKey
                    || attempt.TemplateKey == BookingLifecycleOutboxPayload.WaitlistPromotedTemplateKey)
                .ToListAsync();
            Assert.Equal(3, lifecycleAttempts.Count);
            Assert.All(lifecycleAttempts, attempt => Assert.Equal(MessageDeliveryStatus.Accepted, attempt.Status));
        }

        await RecordPreferenceAsync(client, tenant, patient.StakeholderId, "Denied");
        var deniedHold = await CreateHoldAsync(client, tenant, patient, service, resource, startUtc.AddHours(1));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsConfirm);
        var deniedBookingResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{deniedHold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(deniedHold.Version));
        Assert.Equal(HttpStatusCode.Created, deniedBookingResponse.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>()
                .ProcessBatchAsync(default));
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            var suppressed = Assert.Single(await db.MessageDeliveryAttempts.IgnoreQueryFilters()
                .Where(attempt => attempt.Status == MessageDeliveryStatus.Suppressed)
                .ToListAsync());
            Assert.Equal("preference_denied", suppressed.ErrorCode);
            Assert.Equal("policy", suppressed.ProviderCode);
        }

        SetHeaders(client, tenant, FoundationPermissions.CatalogManage);
        _ = await PostCreated<ServiceResourceRequirementResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services/{service.Id}/resource-requirements",
            new AddServiceResourceRequirementRequest(category.Id, "Assistant", 1, false));
        var incompleteHold = await CreateHoldAsync(client, tenant, patient, service, resource, startUtc.AddHours(2));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsConfirm);
        var incomplete = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{incompleteHold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(incompleteHold.Version));
        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            Assert.Equal(4, await db.Bookings.IgnoreQueryFilters().CountAsync());
            var internalTenant = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>()
                .Decode(PublicIdKind.Tenant, tenant.TenantId);
            var internalHold = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>()
                .Decode(PublicIdKind.SchedulingHold, incompleteHold.Id, internalTenant);
            Assert.Equal(
                SchedulingHoldStatus.Active,
                (await db.SchedulingHolds.IgnoreQueryFilters().SingleAsync(hold => hold.Id == internalHold)).Status);
            Assert.Equal(2, await db.OutboxMessages.IgnoreQueryFilters()
                .CountAsync(message => message.MessageType == BookingConfirmedOutboxPayload.MessageType));
        }
    }

    private static async Task<SchedulingHoldResponse> CreateHoldAsync(
        HttpClient client,
        TenantProvisioningResponse tenant,
        PatientResponse patient,
        ServiceResponse service,
        BookableResourceResponse resource,
        DateTimeOffset startUtc)
    {
        SetHeaders(client, tenant, FoundationPermissions.SchedulingHoldsCreate);
        return await PostCreated<SchedulingHoldResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds",
            new CreateSchedulingHoldRequest(
                Guid.NewGuid(),
                patient.Id,
                service.Id,
                startUtc,
                startUtc.AddMinutes(30),
                10,
                [new HoldResourceRequest(resource.Id, 1, "Scanner")]));
    }

    private static async Task RecordPreferenceAsync(
        HttpClient client,
        TenantProvisioningResponse tenant,
        string stakeholderId,
        string decision,
        string purpose = "Booking.Confirmation")
    {
        SetHeaders(client, tenant, FoundationPermissions.CommunicationPreferencesManage);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/stakeholders/{stakeholderId}/preferences",
            new RecordCommunicationPreferenceRequest(
                purpose,
                "Transactional",
                "Email",
                decision,
                null,
                null,
                "Asia/Kolkata",
                "IntegrationTest",
                null));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<T> PostCreated<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>())!.Data;
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync(
            "/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest(
                "Booking Clinic",
                $"booking-{Guid.NewGuid():N}",
                "owner@example.invalid",
                "Delhi Main",
                "DEL01"));
        var submitted = (await submittedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!.Data;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var approved = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Version));
        return (await approved.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(
        HttpClient client,
        TenantProvisioningResponse tenant,
        params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        if (permissions.Length > 0)
            client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[]
                 {
                     "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator"
                 })
            client.DefaultRequestHeaders.Remove(header);
    }
}
