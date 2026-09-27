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
[Route("api/v1/branches/{branchId}/investigations/worklist")]
public sealed class InvestigationWorklistController(
    InvestigationService service,
    HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.InvestigationWorklistView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<InvestigationWorklistItemResponse>>>> List(
        string branchId,
        [FromQuery] string servicePointId,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.ListWorklistAsync(
            branch,
            ids.Tenant(PublicIdKind.ImagingServicePoint, servicePointId),
            take,
            cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<InvestigationWorklistItemResponse>>(
            response, HttpContext.TraceIdentifier));
    }
}
