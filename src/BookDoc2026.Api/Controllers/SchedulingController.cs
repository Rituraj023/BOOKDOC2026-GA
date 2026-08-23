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

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsConfirm)]
    [HttpPost("holds/{holdId}/confirm")]
    public async Task<ActionResult<ApiEnvelope<BookingResponse>>> ConfirmHold(
        string branchId,
        string holdId,
        ConfirmSchedulingHoldRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.ConfirmHoldAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.SchedulingHold, holdId),
            request,
            cancellationToken);
        var envelope = new ApiEnvelope<BookingResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/scheduling/bookings/{response.Id}", envelope);
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsView)]
    [HttpGet("bookings/{bookingId}")]
    public async Task<ActionResult<ApiEnvelope<BookingResponse>>> GetBooking(
        string branchId,
        string bookingId,
        CancellationToken cancellationToken)
    {
        var response = await service.GetBookingAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Booking, bookingId),
            cancellationToken);
        return Ok(new ApiEnvelope<BookingResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsCancel)]
    [HttpPost("bookings/{bookingId}/cancel")]
    public async Task<ActionResult<ApiEnvelope<BookingResponse>>> CancelBooking(
        string branchId,
        string bookingId,
        CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CancelBookingAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Booking, bookingId),
            request,
            cancellationToken);
        return Ok(new ApiEnvelope<BookingResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsReschedule)]
    [HttpPost("bookings/{bookingId}/reschedule")]
    public async Task<ActionResult<ApiEnvelope<BookingResponse>>> RescheduleBooking(
        string branchId,
        string bookingId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.RescheduleBookingAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Booking, bookingId),
            request,
            cancellationToken);
        var envelope = new ApiEnvelope<BookingResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/scheduling/bookings/{response.Id}", envelope);
    }

    [Authorize(Policy = FoundationPermissions.SchedulingWaitlistManage)]
    [HttpPost("waitlist")]
    public async Task<ActionResult<ApiEnvelope<BookingWaitlistResponse>>> CreateWaitlist(
        string branchId,
        CreateBookingWaitlistRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateWaitlistAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/scheduling/waitlist/{response.Id}",
            new ApiEnvelope<BookingWaitlistResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingWaitlistView)]
    [HttpGet("waitlist/{waitlistId}")]
    public async Task<ActionResult<ApiEnvelope<BookingWaitlistResponse>>> GetWaitlist(
        string branchId,
        string waitlistId,
        CancellationToken cancellationToken)
    {
        var response = await service.GetWaitlistAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.BookingWaitlistEntry, waitlistId),
            cancellationToken);
        return Ok(new ApiEnvelope<BookingWaitlistResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingWaitlistManage)]
    [HttpPost("waitlist/{waitlistId}/withdraw")]
    public async Task<ActionResult<ApiEnvelope<BookingWaitlistResponse>>> WithdrawWaitlist(
        string branchId,
        string waitlistId,
        WithdrawBookingWaitlistRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.WithdrawWaitlistAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.BookingWaitlistEntry, waitlistId),
            request,
            cancellationToken);
        return Ok(new ApiEnvelope<BookingWaitlistResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingWaitlistManage)]
    [HttpPost("waitlist/{waitlistId}/promote")]
    public async Task<ActionResult<ApiEnvelope<BookingResponse>>> PromoteWaitlist(
        string branchId,
        string waitlistId,
        PromoteBookingWaitlistRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.PromoteWaitlistAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.BookingWaitlistEntry, waitlistId),
            request,
            cancellationToken);
        var envelope = new ApiEnvelope<BookingResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/scheduling/bookings/{response.Id}", envelope);
    }
}
