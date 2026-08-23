using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Communications;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class CommunicationsApiTests
{
    private const string CallbackKey = "bookdoc-tests-only-callback-key-32-bytes-minimum";

    [Fact]
    public async Task PreferenceApi_StoresAppendOnlyStakeholderEvidenceWithSeparatePermissions()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);
        SetTenantHeaders(client, tenant, FoundationPermissions.StakeholdersManage);
        var stakeholderResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/stakeholders/persons",
            new CreatePersonStakeholderRequest(
                new(null, "Consent", null, "Example", null, false, "Female"),
                [], [], [], []));
        var stakeholderEnvelope = await stakeholderResponse.Content.ReadFromJsonAsync<ApiEnvelope<StakeholderResponse>>();
        var stakeholder = Assert.IsType<ApiEnvelope<StakeholderResponse>>(stakeholderEnvelope).Data;
        var request = new RecordCommunicationPreferenceRequest(
            "Appointment.Reminder",
            "Transactional",
            "WhatsApp",
            "Allowed",
            new TimeOnly(21, 0),
            new TimeOnly(8, 0),
            "Asia/Kolkata",
            "StaffRecorded",
            "CONSENT-TEST-1");

        SetTenantHeaders(client, tenant, FoundationPermissions.CommunicationPreferencesView);
        var forbidden = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/stakeholders/{stakeholder.Id}/preferences",
            request);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        SetTenantHeaders(client, tenant,
            FoundationPermissions.CommunicationPreferencesView,
            FoundationPermissions.CommunicationPreferencesManage);
        var recordedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/stakeholders/{stakeholder.Id}/preferences",
            request);
        Assert.Equal(HttpStatusCode.OK, recordedResponse.StatusCode);
        var recordedEnvelope = await recordedResponse.Content.ReadFromJsonAsync<ApiEnvelope<CommunicationPreferenceResponse>>();
        var recorded = Assert.IsType<ApiEnvelope<CommunicationPreferenceResponse>>(recordedEnvelope).Data;
        Assert.Equal("APPOINTMENT.REMINDER", recorded.PurposeCode);
        Assert.Equal("Allowed", recorded.Decision);

        var historyResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/stakeholders/{stakeholder.Id}/preferences");
        var historyEnvelope = await historyResponse.Content.ReadFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<CommunicationPreferenceResponse>>>();
        Assert.Single(Assert.IsType<
            ApiEnvelope<IReadOnlyCollection<CommunicationPreferenceResponse>>>(historyEnvelope).Data);
    }

    [Fact]
    public async Task CallbackApi_RejectsBadSignature_DeduplicatesAndReconcilesThroughWorker()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);
        using (var scope = factory.Services.CreateScope())
        {
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(default));
        }
        SetTenantHeaders(client, tenant, FoundationPermissions.MessageDeliveriesView);
        var attemptsEnvelope = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<MessageDeliveryAttemptResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/communications/delivery-attempts");
        var attempt = Assert.Single(Assert.IsType<
            ApiEnvelope<IReadOnlyCollection<MessageDeliveryAttemptResponse>>>(attemptsEnvelope).Data);
        Assert.NotNull(attempt.ProviderMessageId);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventId = "callback-event-1",
            providerMessageId = attempt.ProviderMessageId,
            status = "delivered",
            occurredUtc = DateTimeOffset.Parse("2026-08-18T04:30:00+00:00")
        });

        var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/v1/communications/callbacks/development-email")
        {
            Content = new ByteArrayContent(payload)
        };
        invalid.Headers.Add("X-BookDoc-Signature", "sha256=" + new string('0', 64));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(invalid)).StatusCode);

        var first = await SendCallbackAsync(client, payload);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        var firstEnvelope = await first.Content.ReadFromJsonAsync<ApiEnvelope<ProviderCallbackAcceptedResponse>>();
        Assert.False(Assert.IsType<ApiEnvelope<ProviderCallbackAcceptedResponse>>(firstEnvelope).Data.IsReplay);
        var replay = await SendCallbackAsync(client, payload);
        var replayEnvelope = await replay.Content.ReadFromJsonAsync<ApiEnvelope<ProviderCallbackAcceptedResponse>>();
        Assert.True(Assert.IsType<ApiEnvelope<ProviderCallbackAcceptedResponse>>(replayEnvelope).Data.IsReplay);
        var conflictingPayload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventId = "callback-event-1",
            providerMessageId = attempt.ProviderMessageId,
            status = "failed",
            occurredUtc = DateTimeOffset.Parse("2026-08-18T04:30:00+00:00")
        });
        Assert.Equal(HttpStatusCode.Conflict, (await SendCallbackAsync(client, conflictingPayload)).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(default));
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            Assert.Single(await db.MessageDeliveryStatusEvents.IgnoreQueryFilters().ToListAsync());
        }

        SetTenantHeaders(client, tenant);
        var forbiddenList = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/provider-callbacks");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenList.StatusCode);
        SetTenantHeaders(client, tenant, FoundationPermissions.ProviderCallbacksView);
        var callbacksEnvelope = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<ProviderCallbackInboxResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/communications/provider-callbacks");
        var callback = Assert.Single(Assert.IsType<
            ApiEnvelope<IReadOnlyCollection<ProviderCallbackInboxResponse>>>(callbacksEnvelope).Data);
        Assert.Equal("Processed", callback.InboxStatus);
        Assert.Equal("Delivered", callback.DeliveryStatus);

        var otherTenant = await ProvisionAsync(client);
        SetTenantHeaders(client, otherTenant, FoundationPermissions.ProviderCallbacksView);
        var otherCallbacks = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<ProviderCallbackInboxResponse>>>(
            $"/api/v1/branches/{otherTenant.BranchId}/communications/provider-callbacks");
        Assert.Empty(Assert.IsType<
            ApiEnvelope<IReadOnlyCollection<ProviderCallbackInboxResponse>>>(otherCallbacks).Data);
    }

    private static async Task<HttpResponseMessage> SendCallbackAsync(HttpClient client, byte[] payload)
    {
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(CallbackKey), payload));
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/communications/callbacks/development-email")
        {
            Content = new ByteArrayContent(payload)
        };
        request.Headers.Add("X-BookDoc-Signature", $"sha256={signature}");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task TemplateApi_EnforcesSeparatePermissionsAndBranchVersionConcurrency()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);

        SetTenantHeaders(client, tenant, FoundationPermissions.MessageTemplatesView);
        var initialListResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates");
        Assert.Equal(HttpStatusCode.OK, initialListResponse.StatusCode);
        var initialList = await initialListResponse.Content.ReadFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<MessageTemplateResponse>>>();
        var inheritedTemplates = Assert.IsType<
            ApiEnvelope<IReadOnlyCollection<MessageTemplateResponse>>>(initialList).Data;
        Assert.Equal(5, inheritedTemplates.Count);
        var inherited = Assert.Single(inheritedTemplates, template =>
            template.Key == "Booking.Confirmed.Patient");
        Assert.Equal("Tenant", inherited.Scope);

        var request = new CreateMessageTemplateVersionRequest(
            "Appointment.Confirmed",
            "Email",
            "en-IN",
            "Html",
            "Hello {{PatientName}}",
            "Appointment for {{PatientName}}");
        var forbiddenCreate = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/versions",
            request);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);

        SetTenantHeaders(client, tenant,
            FoundationPermissions.MessageTemplatesView,
            FoundationPermissions.MessageTemplatesManage);
        var createdResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/versions",
            request);
        Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
        var createdEnvelope = await createdResponse.Content.ReadFromJsonAsync<ApiEnvelope<MessageTemplateResponse>>();
        var created = Assert.IsType<ApiEnvelope<MessageTemplateResponse>>(createdEnvelope).Data;
        Assert.Equal("Branch", created.Scope);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(1, created.Version);

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/{created.Id}/preview",
            new PreviewMessageTemplateRequest(new Dictionary<string, string?> { ["PatientName"] = "A&B" }));
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewEnvelope = await previewResponse.Content.ReadFromJsonAsync<ApiEnvelope<MessageTemplatePreviewResponse>>();
        Assert.Equal("Hello A&amp;B", Assert.IsType<ApiEnvelope<MessageTemplatePreviewResponse>>(previewEnvelope).Data.Body);

        var forbiddenPublish = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/{created.Id}/publish",
            new ChangeMessageTemplateStatusRequest(created.Revision));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPublish.StatusCode);

        SetTenantHeaders(client, tenant,
            FoundationPermissions.MessageTemplatesView,
            FoundationPermissions.MessageTemplatesManage,
            FoundationPermissions.MessageTemplatesPublish);
        var publishedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/{created.Id}/publish",
            new ChangeMessageTemplateStatusRequest(created.Revision));
        Assert.Equal(HttpStatusCode.OK, publishedResponse.StatusCode);
        var publishedEnvelope = await publishedResponse.Content.ReadFromJsonAsync<ApiEnvelope<MessageTemplateResponse>>();
        var published = Assert.IsType<ApiEnvelope<MessageTemplateResponse>>(publishedEnvelope).Data;
        Assert.Equal("Published", published.Status);

        var staleRetire = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/{created.Id}/retire",
            new ChangeMessageTemplateStatusRequest(created.Revision));
        Assert.Equal(HttpStatusCode.Conflict, staleRetire.StatusCode);

        var retiredResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/templates/{created.Id}/retire",
            new ChangeMessageTemplateStatusRequest(published.Revision));
        Assert.Equal(HttpStatusCode.OK, retiredResponse.StatusCode);

        using var auditScope = factory.Services.CreateScope();
        var db = auditScope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var audits = await db.AuditEvents
            .Where(audit => audit.EntityType == "MessageTemplate")
            .Select(audit => new { audit.Action, audit.DataJson })
            .ToListAsync();
        Assert.Contains(audits, audit => audit.Action == "MessageTemplate.DraftCreated");
        Assert.Contains(audits, audit => audit.Action == "MessageTemplate.Published");
        Assert.Contains(audits, audit => audit.Action == "MessageTemplate.Retired");
        Assert.DoesNotContain(audits, audit => audit.DataJson.Contains("Hello", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeliveryStatusApi_RequiresPermissionAndReturnsOnlySafeMetadata()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);
        using (var scope = factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
            Assert.Equal(1, await processor.ProcessBatchAsync(default));
        }

        SetTenantHeaders(client, tenant);
        var forbidden = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/delivery-attempts");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        SetTenantHeaders(client, tenant, FoundationPermissions.MessageDeliveriesView);
        var response = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/communications/delivery-attempts?take=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<MessageDeliveryAttemptResponse>>>();
        var attempt = Assert.Single(Assert.IsType<
            ApiEnvelope<IReadOnlyCollection<MessageDeliveryAttemptResponse>>>(envelope).Data);
        Assert.Equal("Accepted", attempt.Status);
        Assert.Equal("development-email", attempt.ProviderCode);
        Assert.Equal("Tenant.Approved.Contact", attempt.TemplateKey);
        Assert.DoesNotContain("communications-api@", attempt.RecipientHint, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync(
            "/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest(
                "Communications API Clinic",
                $"communications-api-{Guid.NewGuid():N}",
                "communications-api@example.invalid",
                "Delhi Main",
                "DEL01"));
        var submitted = await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>();
        Assert.NotNull(submitted);

        ClearSecurityHeaders(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "7001");
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
        client.DefaultRequestHeaders.Add("X-User-Id", "7002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
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
