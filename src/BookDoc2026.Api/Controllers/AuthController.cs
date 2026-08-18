using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, ICurrentActor actor) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiEnvelope<AuthSessionResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(new ApiEnvelope<AuthSessionResponse>(response, HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiEnvelope<AuthSessionResponse>>> Refresh(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return Ok(new ApiEnvelope<AuthSessionResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        if (actor.ActorId is <= 0 or > uint.MaxValue)
        {
            return Unauthorized();
        }

        await authService.RevokeAsync((uint)actor.ActorId, cancellationToken);
        return NoContent();
    }
}
