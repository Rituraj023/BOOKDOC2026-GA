using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Stakeholders;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/stakeholders")]
public sealed class StakeholdersController(StakeholderService stakeholderService, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.StakeholdersManage)]
    [HttpPost("persons")]
    public async Task<ActionResult<ApiEnvelope<StakeholderResponse>>> CreatePerson(
        string branchId,
        CreatePersonStakeholderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await stakeholderService.CreatePersonAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/stakeholders/{response.Id}",
            new ApiEnvelope<StakeholderResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.StakeholdersManage)]
    [HttpPost("corporates")]
    public async Task<ActionResult<ApiEnvelope<StakeholderResponse>>> CreateCorporate(
        string branchId,
        StakeholderCorporateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await stakeholderService.CreateCorporateAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/stakeholders/{response.Id}",
            new ApiEnvelope<StakeholderResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.StakeholdersView)]
    [HttpGet("{stakeholderId}")]
    public async Task<ActionResult<ApiEnvelope<StakeholderResponse>>> Get(
        string branchId,
        string stakeholderId,
        CancellationToken cancellationToken)
    {
        var response = await stakeholderService.GetAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Stakeholder, stakeholderId), cancellationToken);
        return Ok(new ApiEnvelope<StakeholderResponse>(response, HttpContext.TraceIdentifier));
    }
}
