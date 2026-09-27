using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Contracts;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Contracts;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/contracts")]
public sealed class ContractController(ContractService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.ContractsManage)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<ContractResponse>>> Create(
        string branchId, CreateContractRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/contracts/{response.Id}",
            new ApiEnvelope<ContractResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ContractsView)]
    [HttpGet("{contractId}")]
    public async Task<ActionResult<ApiEnvelope<ContractResponse>>> Get(
        string branchId, string contractId, CancellationToken cancellationToken)
    {
        var response = await service.GetAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Contract, contractId), cancellationToken);
        return Ok(new ApiEnvelope<ContractResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ContractEntitlementsReserve)]
    [HttpPost("{contractId}/entitlements/{entitlementId}/reservations")]
    public async Task<ActionResult<ApiEnvelope<EntitlementReservationResponse>>> Reserve(
        string branchId, string contractId, string entitlementId, ReserveEntitlementRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.ReserveAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Contract, contractId),
            ids.Tenant(PublicIdKind.ContractEntitlement, entitlementId), request, cancellationToken);
        var envelope = new ApiEnvelope<EntitlementReservationResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/contracts/entitlement-reservations/{response.Id}", envelope);
    }

    [Authorize(Policy = FoundationPermissions.ContractEntitlementsConsume)]
    [HttpPost("entitlement-reservations/{reservationId}/consume")]
    public Task<ActionResult<ApiEnvelope<EntitlementReservationResponse>>> Consume(
        string branchId, string reservationId, ConsumeEntitlementReservationRequest request,
        CancellationToken cancellationToken) =>
        Transition(branchId, reservationId, request,
            (branch, reservation, token) => service.ConsumeAsync(branch, reservation, request, token),
            cancellationToken);

    [Authorize(Policy = FoundationPermissions.ContractEntitlementsRelease)]
    [HttpPost("entitlement-reservations/{reservationId}/release")]
    public async Task<ActionResult<ApiEnvelope<EntitlementReservationResponse>>> Release(
        string branchId, string reservationId, ReleaseEntitlementReservationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.ReleaseAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.EntitlementReservation, reservationId), request, cancellationToken);
        return Ok(new ApiEnvelope<EntitlementReservationResponse>(response, HttpContext.TraceIdentifier));
    }

    private async Task<ActionResult<ApiEnvelope<EntitlementReservationResponse>>> Transition<TRequest>(
        string branchId, string reservationId, TRequest request,
        Func<long, long, CancellationToken, Task<EntitlementReservationResponse>> action,
        CancellationToken cancellationToken)
    {
        _ = request;
        var response = await action(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.EntitlementReservation, reservationId), cancellationToken);
        return Ok(new ApiEnvelope<EntitlementReservationResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ContractsView)]
    [HttpGet("patients/{patientId}/active-packages")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<PatientPackageSummaryResponse>>>> GetPatientActivePackages(
        string branchId, string patientId, [FromQuery] string? serviceId, CancellationToken cancellationToken)
    {
        var response = await service.GetPatientActivePackagesAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), patientId, serviceId, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<PatientPackageSummaryResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ContractsView)]
    [HttpGet("bookings/{bookingId}/package-status")]
    public async Task<ActionResult<ApiEnvelope<BookingPackageStatusResponse>>> GetBookingPackageStatus(
        string branchId, string bookingId, CancellationToken cancellationToken)
    {
        var response = await service.GetBookingPackageStatusAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), bookingId, cancellationToken);
        return Ok(new ApiEnvelope<BookingPackageStatusResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ContractEntitlementsReserve)]
    [HttpPost("bookings/{bookingId}/link-package")]
    public async Task<ActionResult<ApiEnvelope<EntitlementReservationResponse>>> LinkBookingPackage(
        string branchId, string bookingId, LinkBookingPackageRequest request, CancellationToken cancellationToken)
    {
        var response = await service.LinkBookingToPackageAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), bookingId, request, cancellationToken);
        return Ok(new ApiEnvelope<EntitlementReservationResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ContractEntitlementsRelease)]
    [HttpPost("bookings/{bookingId}/unlink-package")]
    public async Task<ActionResult<ApiEnvelope<EntitlementReservationResponse>>> UnlinkBookingPackage(
        string branchId, string bookingId, UnlinkBookingPackageRequest request, CancellationToken cancellationToken)
    {
        var response = await service.UnlinkBookingFromPackageAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), bookingId, request, cancellationToken);
        return Ok(new ApiEnvelope<EntitlementReservationResponse>(response, HttpContext.TraceIdentifier));
    }
}