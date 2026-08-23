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
[Route("api/v1/branches/{branchId}/encounters")]
public sealed class EncounterController(EncounterService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.EncounterDraftsManage)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<EncounterResponse>>> Start(
        string branchId, StartEncounterRequest request, CancellationToken cancellationToken)
    {
        var response = await service.StartAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/encounters/{response.Id}",
            new ApiEnvelope<EncounterResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersView)]
    [HttpGet("{encounterId}")]
    public async Task<ActionResult<ApiEnvelope<EncounterResponse>>> Get(
        string branchId, string encounterId, CancellationToken cancellationToken)
    {
        var response = await service.GetAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Encounter, encounterId), cancellationToken);
        return Ok(new ApiEnvelope<EncounterResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncounterDraftsManage)]
    [HttpPost("{encounterId}/draft-revisions")]
    public async Task<ActionResult<ApiEnvelope<EncounterResponse>>> ReviseDraft(
        string branchId, string encounterId, ReviseEncounterDraftRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.ReviseDraftAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Encounter, encounterId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/encounters/{encounterId}/revisions/{response.LatestRevisionNumber}",
            new ApiEnvelope<EncounterResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersSign)]
    [HttpPost("{encounterId}/sign")]
    public async Task<ActionResult<ApiEnvelope<EncounterResponse>>> Sign(
        string branchId, string encounterId, SignEncounterRequest request, CancellationToken cancellationToken)
    {
        var response = await service.SignAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Encounter, encounterId), request, cancellationToken);
        return Ok(new ApiEnvelope<EncounterResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersAmend)]
    [HttpPost("{encounterId}/amendments")]
    public async Task<ActionResult<ApiEnvelope<EncounterResponse>>> Amend(
        string branchId, string encounterId, AmendEncounterRequest request, CancellationToken cancellationToken)
    {
        var response = await service.AmendAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Encounter, encounterId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/encounters/{encounterId}/revisions/{response.LatestRevisionNumber}",
            new ApiEnvelope<EncounterResponse>(response, HttpContext.TraceIdentifier));
    }
}
