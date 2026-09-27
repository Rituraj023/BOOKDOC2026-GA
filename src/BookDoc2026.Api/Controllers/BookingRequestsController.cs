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
[Route("api/v1/branches/{branchId}/booking-requests")]
public sealed class BookingRequestsController(IBookingRequestService bookingRequestService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<BookingRequestResponse>>> SubmitRequest(
        string branchId,
        SubmitBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingRequestService.SubmitRequestAsync(request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/booking-requests/{result.Id}",
            new ApiEnvelope<BookingRequestResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<BookingRequestResponse>>>> GetRequests(
        string branchId,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await bookingRequestService.GetRequestsAsync(branchId, status, fromDate, toDate, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<BookingRequestResponse>>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsView)]
    [HttpGet("{requestId}")]
    public async Task<ActionResult<ApiEnvelope<BookingRequestResponse>>> GetRequestById(
        string branchId,
        string requestId,
        CancellationToken cancellationToken)
    {
        var result = await bookingRequestService.GetRequestByIdAsync(branchId, requestId, cancellationToken);
        return Ok(new ApiEnvelope<BookingRequestResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsConfirm)]
    [HttpPost("{requestId}/approve")]
    public async Task<ActionResult<ApiEnvelope<BookingRequestResponse>>> ApproveRequest(
        string branchId,
        string requestId,
        ApproveBookingRequest command,
        CancellationToken cancellationToken)
    {
        var result = await bookingRequestService.ApproveRequestAsync(branchId, requestId, command, cancellationToken);
        return Ok(new ApiEnvelope<BookingRequestResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsConfirm)]
    [HttpPost("{requestId}/decline")]
    public async Task<ActionResult<ApiEnvelope<BookingRequestResponse>>> DeclineRequest(
        string branchId,
        string requestId,
        DeclineBookingRequest command,
        CancellationToken cancellationToken)
    {
        var result = await bookingRequestService.DeclineRequestAsync(branchId, requestId, command, cancellationToken);
        return Ok(new ApiEnvelope<BookingRequestResponse>(result, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.SchedulingBookingsConfirm)]
    [HttpPost("{requestId}/reschedule")]
    public async Task<ActionResult<ApiEnvelope<BookingRequestResponse>>> RescheduleRequest(
        string branchId,
        string requestId,
        RescheduleBookingRequestNotice command,
        CancellationToken cancellationToken)
    {
        var result = await bookingRequestService.RescheduleRequestAsync(branchId, requestId, command, cancellationToken);
        return Ok(new ApiEnvelope<BookingRequestResponse>(result, HttpContext.TraceIdentifier));
    }
}
