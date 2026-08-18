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
[Route("api/v1/branches")]
public sealed class BranchesController(FoundationService foundationService, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.BranchAdministratorsManage)]
    [HttpPost("{branchId}/administrators")]
    public async Task<ActionResult<ApiEnvelope<BranchAdministratorResponse>>> CreateAdministrator(
        string branchId,
        CreateBranchAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foundationService.CreateBranchAdministratorAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/administrators/{response.UserScopeId}",
            new ApiEnvelope<BranchAdministratorResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.BranchesView)]
    [HttpGet("{branchId}/configuration")]
    public async Task<ActionResult<ApiEnvelope<BranchConfigurationResponse>>> GetConfiguration(
        string branchId,
        CancellationToken cancellationToken)
    {
        var response = await foundationService.GetBranchConfigurationAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), cancellationToken);
        return Ok(new ApiEnvelope<BranchConfigurationResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.BranchesConfigurationManage)]
    [HttpPut("{branchId}/configuration")]
    public async Task<ActionResult<ApiEnvelope<BranchConfigurationResponse>>> UpdateConfiguration(
        string branchId,
        UpdateBranchConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foundationService.UpdateBranchConfigurationAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Ok(new ApiEnvelope<BranchConfigurationResponse>(response, HttpContext.TraceIdentifier));
    }
}
