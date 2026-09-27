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
[Route("api/v1/branches/{branchId}/doctor-slots")]
public sealed class DoctorSlotsController(ISlotManagementService slotService) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityManage)]
    [HttpPost("generate")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>>> GenerateSlots(
        string branchId,
        GenerateDoctorSlotsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await slotService.GenerateSlotsAsync(request, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>>> GetSlots(
        string branchId,
        [FromQuery] string? practitionerId = null,
        [FromQuery] string? serviceId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await slotService.GetSlotsAsync(branchId, practitionerId, serviceId, fromDate, toDate, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>(result, HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpGet("available")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>>> GetAvailableSlots(
        string branchId,
        [FromQuery] string? practitionerId = null,
        [FromQuery] string? serviceId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await slotService.GetAvailableSlotsAsync(branchId, practitionerId, serviceId, fromDate, toDate, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<DoctorSlotResponse>>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityManage)]
    [HttpPost("{slotId}/block")]
    public async Task<ActionResult<ApiEnvelope<DoctorSlotResponse>>> BlockSlot(
        string branchId,
        string slotId,
        BlockDoctorSlotRequest request,
        CancellationToken cancellationToken)
    {
        var result = await slotService.BlockSlotAsync(branchId, slotId, request, cancellationToken);
        return Ok(new ApiEnvelope<DoctorSlotResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityManage)]
    [HttpPost("{slotId}/unblock")]
    public async Task<ActionResult<ApiEnvelope<DoctorSlotResponse>>> UnblockSlot(
        string branchId,
        string slotId,
        CancellationToken cancellationToken)
    {
        var result = await slotService.UnblockSlotAsync(branchId, slotId, cancellationToken);
        return Ok(new ApiEnvelope<DoctorSlotResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingAvailabilityManage)]
    [HttpPost("{slotId}/cancel")]
    public async Task<ActionResult<ApiEnvelope<DoctorSlotResponse>>> CancelSlot(
        string branchId,
        string slotId,
        CancellationToken cancellationToken)
    {
        var result = await slotService.CancelSlotAsync(branchId, slotId, cancellationToken);
        return Ok(new ApiEnvelope<DoctorSlotResponse>(result, HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpPost("book-direct")]
    public async Task<ActionResult<ApiEnvelope<DirectSlotBookingConfirmationResponse>>> BookSlotDirect(
        string branchId,
        BookSlotDirectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await slotService.BookSlotDirectAsync(branchId, request, cancellationToken);
        return Ok(new ApiEnvelope<DirectSlotBookingConfirmationResponse>(result, HttpContext.TraceIdentifier));
    }
}
