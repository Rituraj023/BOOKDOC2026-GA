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
[Route("api/v1/branches/{branchId}")]
public sealed class VitalsController(IVitalsService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.EncounterDraftsManage)]
    [HttpPost("patients/{patientId}/vitals")]
    public async Task<ActionResult<ApiEnvelope<VitalSignsResponse>>> Record(
        string branchId,
        string patientId,
        RecordVitalSignsRequest request,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var result = await service.RecordVitalsAsync(branch, patientId, request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/patients/{patientId}/vitals/{result.Id}",
            new ApiEnvelope<VitalSignsResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersView)]
    [HttpGet("patients/{patientId}/vitals/latest")]
    public async Task<ActionResult<ApiEnvelope<VitalSignsResponse?>>> GetLatest(
        string branchId,
        string patientId,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var result = await service.GetLatestVitalsAsync(branch, patientId, cancellationToken);
        return Ok(new ApiEnvelope<VitalSignsResponse?>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersView)]
    [HttpGet("patients/{patientId}/vitals/history")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<VitalSignsResponse>>>> GetHistory(
        string branchId,
        string patientId,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var result = await service.ListVitalsHistoryAsync(branch, patientId, take, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<VitalSignsResponse>>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersView)]
    [HttpGet("vitals/by-booking/{bookingId}")]
    public async Task<ActionResult<ApiEnvelope<VitalSignsResponse?>>> GetByBooking(
        string branchId,
        string bookingId,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var result = await service.GetVitalsByBookingAsync(branch, bookingId, cancellationToken);
        return Ok(new ApiEnvelope<VitalSignsResponse?>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.EncountersView)]
    [HttpGet("vitals/by-encounter/{encounterId}")]
    public async Task<ActionResult<ApiEnvelope<VitalSignsResponse?>>> GetByEncounter(
        string branchId,
        string encounterId,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var result = await service.GetVitalsByEncounterAsync(branch, encounterId, cancellationToken);
        return Ok(new ApiEnvelope<VitalSignsResponse?>(result, HttpContext.TraceIdentifier));
    }
}
