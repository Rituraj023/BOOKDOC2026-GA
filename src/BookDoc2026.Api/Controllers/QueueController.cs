using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Queues;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/queues")]
public sealed class QueueController(QueueService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize]
    [HttpGet("imaging-service-points")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<ImagingServicePointResponse>>>> ListServicePoints(
        string branchId,
        CancellationToken cancellationToken)
    {
        var response = await service.ListServicePointsAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<ImagingServicePointResponse>>(
            response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.QueuesServicePointsManage)]
    [HttpPost("imaging-service-points")]
    public async Task<ActionResult<ApiEnvelope<ImagingServicePointResponse>>> CreateServicePoint(
        string branchId,
        CreateImagingServicePointRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateServicePointAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/queues/imaging-service-points/{response.Id}",
            new ApiEnvelope<ImagingServicePointResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.QueuesCheckIn)]
    [HttpPost("tickets")]
    public async Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> CheckIn(
        string branchId,
        CheckInQueueTicketRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CheckInAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        var envelope = new ApiEnvelope<QueueTicketResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/queues/tickets/{response.Id}", envelope);
    }

    [Authorize(Policy = FoundationPermissions.QueuesView)]
    [HttpGet("imaging-service-points/{servicePointId}/tickets")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<QueueTicketResponse>>>> List(
        string branchId,
        string servicePointId,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.ListAsync(branch,
            ids.Tenant(PublicIdKind.ImagingServicePoint, servicePointId), cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<QueueTicketResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.QueuesDisplayView)]
    [HttpGet("imaging-service-points/{servicePointId}/display")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<QueueDisplayTicketResponse>>>> Display(
        string branchId,
        string servicePointId,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.DisplayAsync(branch,
            ids.Tenant(PublicIdKind.ImagingServicePoint, servicePointId), cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<QueueDisplayTicketResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.QueuesCall)]
    [HttpPost("tickets/{ticketId}/call")]
    public Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Call(
        string branchId, string ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        Transition(branchId, ticketId, request, service.CallAsync, cancellationToken);

    [Authorize(Policy = FoundationPermissions.QueuesCall)]
    [HttpPost("tickets/{ticketId}/recall")]
    public Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Recall(
        string branchId, string ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        Transition(branchId, ticketId, request, service.RecallAsync, cancellationToken);

    [Authorize(Policy = FoundationPermissions.QueuesProgress)]
    [HttpPost("tickets/{ticketId}/prepare")]
    public Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Prepare(
        string branchId, string ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        Transition(branchId, ticketId, request, service.PrepareAsync, cancellationToken);

    [Authorize(Policy = FoundationPermissions.QueuesProgress)]
    [HttpPost("tickets/{ticketId}/start")]
    public Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Start(
        string branchId, string ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        Transition(branchId, ticketId, request, service.StartAsync, cancellationToken);

    [Authorize(Policy = FoundationPermissions.QueuesProgress)]
    [HttpPost("tickets/{ticketId}/complete")]
    public Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Complete(
        string branchId, string ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        Transition(branchId, ticketId, request, service.CompleteAsync, cancellationToken);

    [Authorize(Policy = FoundationPermissions.QueuesCancel)]
    [HttpPost("tickets/{ticketId}/cancel")]
    public Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Cancel(
        string branchId, string ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        Transition(branchId, ticketId, request, service.CancelAsync, cancellationToken);

    private async Task<ActionResult<ApiEnvelope<QueueTicketResponse>>> Transition(
        string branchId,
        string ticketId,
        QueueTransitionRequest request,
        Func<long, long, QueueTransitionRequest, CancellationToken, Task<QueueTicketResponse>> action,
        CancellationToken cancellationToken)
    {
        var response = await action(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.QueueTicket, ticketId),
            request,
            cancellationToken);
        return Ok(new ApiEnvelope<QueueTicketResponse>(response, HttpContext.TraceIdentifier));
    }
}
