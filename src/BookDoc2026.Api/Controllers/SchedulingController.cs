using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/scheduling")]
public sealed class SchedulingController(SchedulingService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityManage)]
    [HttpPost("availability-rules")]
    public async Task<ActionResult<ApiEnvelope<AvailabilityRuleResponse>>> CreateRule(string branchId,
        CreateAvailabilityRuleRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateRuleAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/scheduling/availability-rules/{response.Id}",
            new ApiEnvelope<AvailabilityRuleResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityManage)]
    [HttpPost("availability-exceptions")]
    public async Task<ActionResult<ApiEnvelope<AvailabilityExceptionResponse>>> CreateException(string branchId,
        CreateAvailabilityExceptionRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateExceptionAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/scheduling/availability-exceptions/{response.Id}",
            new ApiEnvelope<AvailabilityExceptionResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityView)]
    [HttpGet("availability")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<AvailabilityResourceResponse>>>> Search(
        string branchId, [FromQuery] string serviceId, [FromQuery] DateTimeOffset startUtc,
        [FromQuery] DateTimeOffset endUtc, [FromQuery] int quantity = 1, CancellationToken cancellationToken = default)
    {
        var response = await service.SearchAvailabilityAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.ClinicalService, serviceId), startUtc, endUtc, quantity, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<AvailabilityResourceResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingHoldsCreate)]
    [HttpPost("holds")]
    public async Task<ActionResult<ApiEnvelope<SchedulingHoldResponse>>> CreateHold(string branchId,
        CreateSchedulingHoldRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateHoldAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/scheduling/holds/{response.Id}",
            new ApiEnvelope<SchedulingHoldResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityView)]
    [HttpGet("holds/{holdId}")]
    public async Task<ActionResult<ApiEnvelope<SchedulingHoldResponse>>> GetHold(string branchId, string holdId, CancellationToken cancellationToken)
    {
        var response = await service.GetHoldAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.SchedulingHold, holdId), cancellationToken);
        return Ok(new ApiEnvelope<SchedulingHoldResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingHoldsRelease)]
    [HttpPost("holds/{holdId}/release")]
    public async Task<ActionResult<ApiEnvelope<SchedulingHoldResponse>>> ReleaseHold(string branchId, string holdId,
        ReleaseSchedulingHoldRequest request, CancellationToken cancellationToken)
    {
        var response = await service.ReleaseHoldAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.SchedulingHold, holdId), request, cancellationToken);
        return Ok(new ApiEnvelope<SchedulingHoldResponse>(response, HttpContext.TraceIdentifier));
    }
}
