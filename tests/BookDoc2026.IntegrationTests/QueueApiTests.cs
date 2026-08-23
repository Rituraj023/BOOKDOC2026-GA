using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class QueueApiTests
{
    [Fact]
    public async Task ImagingQueue_IsAuthorizedIdempotentVersionedPrivateAndReconcilable()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client);

        SetHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var patient = await PostCreated<PatientResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/patients",
            new RegisterPatientRequest(
                Guid.NewGuid(), new(null, "Queue", null, "Patient", new DateOnly(1992, 2, 2), false, "Other"),
                null, [new("Email", "queue.patient@example.invalid", true)], [], [], [], null));

        SetHeaders(client, tenant, FoundationPermissions.QueuesView);
        var forbiddenPoint = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points",
            new CreateImagingServicePointRequest("XRAY-1", "X-ray Room 1", "XRay", null));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPoint.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.QueuesServicePointsManage);
        var servicePoint = await PostCreated<ImagingServicePointResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points",
            new CreateImagingServicePointRequest("XRAY-1", "X-ray Room 1", "XRay", null));
        Assert.Equal("XRay", servicePoint.Modality);

        var managerCatalog = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points");
        Assert.Equal(HttpStatusCode.OK, managerCatalog.StatusCode);
        SetHeaders(client, tenant);
        var catalogWithoutPermission = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points");
        Assert.Equal(HttpStatusCode.Forbidden, catalogWithoutPermission.StatusCode);
        SetHeaders(client, tenant, FoundationPermissions.QueuesView);
        var catalog = await client.GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<ImagingServicePointResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points");
        var catalogPoint = Assert.Single(catalog!.Data);
        Assert.Equal(servicePoint.Code, catalogPoint.Code);
        Assert.False(string.IsNullOrWhiteSpace(catalogPoint.Id));

        SetHeaders(client, tenant, FoundationPermissions.QueuesCheckIn);
        var receptionistCatalog = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points");
        Assert.Equal(HttpStatusCode.OK, receptionistCatalog.StatusCode);
        var urgentForbidden = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets",
            new CheckInQueueTicketRequest(Guid.NewGuid(), servicePoint.Id, patient.Id, null, "Urgent", "Clinical escalation"));
        Assert.Equal(HttpStatusCode.Forbidden, urgentForbidden.StatusCode);

        var requestId = Guid.NewGuid();
        var checkedInResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets",
            new CheckInQueueTicketRequest(requestId, servicePoint.Id, patient.Id, null, "Normal", null));
        Assert.Equal(HttpStatusCode.Created, checkedInResponse.StatusCode);
        var checkedIn = (await checkedInResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.Equal("Waiting", checkedIn.Status);
        Assert.StartsWith("Q-", checkedIn.DisplayToken, StringComparison.Ordinal);

        var replayResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets",
            new CheckInQueueTicketRequest(requestId, servicePoint.Id, patient.Id, null, "Normal", null));
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replay = (await replayResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.True(replay.IsReplay);
        Assert.Equal(checkedIn.DisplayToken, replay.DisplayToken);
        SetHeaders(client, tenant, FoundationPermissions.QueuesCheckIn, FoundationPermissions.QueuesPriorityManage);
        var alteredReplay = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets",
            new CheckInQueueTicketRequest(requestId, servicePoint.Id, patient.Id, null, "Urgent", "Changed replay payload"));
        Assert.Equal(HttpStatusCode.BadRequest, alteredReplay.StatusCode);

        using var anonymousClient = NewClient(factory);
        var anonymousNegotiate = await anonymousClient.PostAsync("/hubs/queue/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousNegotiate.StatusCode);
        SetHeaders(client, tenant, FoundationPermissions.QueuesView);
        var negotiate = await client.PostAsync("/hubs/queue/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, negotiate.StatusCode);

        var forbiddenCall = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{checkedIn.Id}/call",
            new QueueTransitionRequest(checkedIn.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCall.StatusCode);
        var called = await TransitionAsync(client, tenant, checkedIn, "call", FoundationPermissions.QueuesCall);
        Assert.Equal("Called", called.Status);
        Assert.Equal(1, called.CallCount);

        SetHeaders(client, tenant, FoundationPermissions.QueuesCall);
        var staleRecall = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{called.Id}/recall",
            new QueueTransitionRequest(checkedIn.Version, "Stale recall"));
        Assert.Equal(HttpStatusCode.Conflict, staleRecall.StatusCode);
        var recalled = await TransitionAsync(client, tenant, called, "recall", FoundationPermissions.QueuesCall,
            "Patient did not hear first call");
        Assert.Equal(2, recalled.CallCount);

        SetHeaders(client, tenant, FoundationPermissions.QueuesDisplayView);
        var displayEnvelope = await client.GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<QueueDisplayTicketResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points/{servicePoint.Id}/display");
        var display = Assert.Single(displayEnvelope!.Data);
        Assert.Equal(checkedIn.DisplayToken, display.DisplayToken);
        Assert.Equal("Called", display.Status);

        var prepared = await TransitionAsync(client, tenant, recalled, "prepare", FoundationPermissions.QueuesProgress);
        var started = await TransitionAsync(client, tenant, prepared, "start", FoundationPermissions.QueuesProgress);
        var completed = await TransitionAsync(client, tenant, started, "complete", FoundationPermissions.QueuesProgress);
        Assert.Equal("Completed", completed.Status);

        SetHeaders(client, tenant, FoundationPermissions.QueuesView);
        var list = await client.GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<QueueTicketResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points/{servicePoint.Id}/tickets");
        Assert.Single(list!.Data);
        Assert.Equal("Completed", list.Data.Single().Status);

        SetHeaders(client, tenant, FoundationPermissions.QueuesCheckIn, FoundationPermissions.QueuesPriorityManage);
        var urgent = await PostCreated<QueueTicketResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets",
            new CheckInQueueTicketRequest(Guid.NewGuid(), servicePoint.Id, patient.Id, null, "Urgent", "Clinician escalation"));
        Assert.Equal("Urgent", urgent.Priority);
        var cancellation = await TransitionAsync(client, tenant, urgent, "cancel", FoundationPermissions.QueuesCancel,
            "Investigation no longer required");
        Assert.Equal("Cancelled", cancellation.Status);

        var otherTenant = await ProvisionAsync(client);
        SetHeaders(client, otherTenant, FoundationPermissions.QueuesView);
        var crossTenant = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/queues/imaging-service-points/{servicePoint.Id}/tickets");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        Assert.Equal(2, await db.QueueTickets.IgnoreQueryFilters().CountAsync());
        Assert.Equal(8, await db.QueueTicketEvents.IgnoreQueryFilters().CountAsync());
        Assert.All(await db.QueueTicketEvents.IgnoreQueryFilters().ToListAsync(), item =>
            Assert.True(item.TicketVersion >= 1));
    }

    private static HttpClient NewClient(BookDocApiFactory factory) =>
        factory.CreateClient(new() { AllowAutoRedirect = false });

    private static async Task<QueueTicketResponse> TransitionAsync(
        HttpClient client,
        TenantProvisioningResponse tenant,
        QueueTicketResponse current,
        string action,
        string permission,
        string? reason = null)
    {
        SetHeaders(client, tenant, permission);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{current.Id}/{action}",
            new QueueTransitionRequest(current.Version, reason));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
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
            new SubmitTenantApplicationRequest("Queue Clinic", $"queue-{Guid.NewGuid():N}",
                "owner@example.invalid", "Delhi Main", "DEL01"));
        var submitted = (await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!.Data;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "9001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var approved = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Version));
        return (await approved.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "9002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        if (permissions.Length > 0) client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[] { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
