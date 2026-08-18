using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/platform/identity/users")]
[Authorize(Policy = FoundationPermissions.UsersManage)]
public sealed class IdentityUsersController(IIdentityAdministrationService identityAdministration) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<IdentityUserResponse>>> Create(
        CreateIdentityUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await identityAdministration.CreateUserAsync(request, cancellationToken);
        return Created(
            $"/api/v1/platform/identity/users/{response.SubjectId}",
            new ApiEnvelope<IdentityUserResponse>(response, HttpContext.TraceIdentifier));
    }
}
