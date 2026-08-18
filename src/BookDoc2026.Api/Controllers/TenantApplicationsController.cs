using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class TenantApplicationsController(FoundationService foundationService, HttpPublicIdDecoder ids) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("tenant-applications")]
    [ProducesResponseType<TenantApplicationResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiEnvelope<TenantApplicationResponse>>> Submit(
        SubmitTenantApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foundationService.SubmitApplicationAsync(request, "ClinicApplicant", cancellationToken);
        return Created(
            $"/api/v1/platform/tenant-applications/{response.Id}",
            new ApiEnvelope<TenantApplicationResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.TenantsRegister)]
    [HttpPost("platform/tenant-applications")]
    public async Task<ActionResult<ApiEnvelope<TenantApplicationResponse>>> DirectRegister(
        SubmitTenantApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foundationService.SubmitApplicationAsync(request, "PlatformOperator", cancellationToken);
        return Created(
            $"/api/v1/platform/tenant-applications/{response.Id}",
            new ApiEnvelope<TenantApplicationResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.TenantsApprove)]
    [HttpPost("platform/tenant-applications/{applicationId}/approve")]
    public async Task<ActionResult<ApiEnvelope<TenantProvisioningResponse>>> Approve(
        string applicationId,
        ApproveTenantApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foundationService.ApproveApplicationAsync(
            ids.Platform(PublicIdKind.TenantApplication, applicationId), request, cancellationToken);
        return Ok(new ApiEnvelope<TenantProvisioningResponse>(response, HttpContext.TraceIdentifier));
    }
}
