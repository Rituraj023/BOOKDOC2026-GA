using BookDoc2026.Application.Communications;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Communications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/communications/callbacks")]
public sealed class ProviderCallbacksController(ProviderCallbackIngressService service) : ControllerBase
{
    [AllowAnonymous]
    [RequestSizeLimit(65_536)]
    [HttpPost("{providerCode}")]
    public async Task<ActionResult<ApiEnvelope<ProviderCallbackAcceptedResponse>>> Receive(
        string providerCode,
        CancellationToken cancellationToken)
    {
        if (Request.ContentLength is > 65_536)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        using var payload = new MemoryStream();
        var buffer = new byte[8_192];
        int read;
        while ((read = await Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (payload.Length + read > 65_536)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            await payload.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        var signature = Request.Headers["X-BookDoc-Signature"].ToString();
        var response = await service.ReceiveAsync(
            providerCode,
            payload.ToArray(),
            signature,
            cancellationToken);
        return Accepted(new ApiEnvelope<ProviderCallbackAcceptedResponse>(
            response,
            HttpContext.TraceIdentifier));
    }
}
