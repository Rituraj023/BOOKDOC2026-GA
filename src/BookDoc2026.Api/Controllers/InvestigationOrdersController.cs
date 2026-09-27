using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Clinical;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/encounters/{encounterId}/investigation-orders")]
public sealed class InvestigationOrdersController(
    InvestigationService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.InvestigationOrdersCreate)]
    [HttpGet("catalog-options")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<InvestigationServiceOptionResponse>>>>
        ListCatalogOptions(string branchId, string encounterId, CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.ListCatalogOptionsAsync(branch,
            ids.Tenant(PublicIdKind.Encounter, encounterId), cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<InvestigationServiceOptionResponse>>(
            response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.InvestigationsView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<InvestigationOrderResponse>>>> List(
        string branchId, string encounterId, CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.ListAsync(branch,
            ids.Tenant(PublicIdKind.Encounter, encounterId), cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<InvestigationOrderResponse>>(
            response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.InvestigationOrdersCreate)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<InvestigationOrderResponse>>> Create(
        string branchId, string encounterId, CreateInvestigationOrderRequest request,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.CreateAsync(branch,
            ids.Tenant(PublicIdKind.Encounter, encounterId), request, cancellationToken);
        var envelope = new ApiEnvelope<InvestigationOrderResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/encounters/{encounterId}/investigation-orders/{response.Id}",
                envelope);
    }

    [Authorize(Policy = FoundationPermissions.InvestigationQueueHandoff)]
    [HttpPost("{orderId}/queue-handoffs")]
    public async Task<ActionResult<ApiEnvelope<InvestigationOrderResponse>>> Handoff(
        string branchId, string encounterId, string orderId, HandoffInvestigationOrderRequest request,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.HandoffToQueueAsync(branch,
            ids.Tenant(PublicIdKind.Encounter, encounterId),
            ids.Tenant(PublicIdKind.InvestigationOrder, orderId), request, cancellationToken);
        var envelope = new ApiEnvelope<InvestigationOrderResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/queues/tickets/{response.QueueTicket!.Id}", envelope);
    }
}
