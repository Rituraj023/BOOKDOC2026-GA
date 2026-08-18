using Microsoft.Extensions.Options;

namespace BookDoc2026.Admin.Security;

public sealed class AdminNetworkRestrictionMiddleware(
    RequestDelegate next,
    IOptions<AdminNetworkOptions> options,
    IHostEnvironment environment,
    ILogger<AdminNetworkRestrictionMiddleware> logger)
{
    private readonly AdminNetworkPolicy _policy = new(options.Value);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/health" || context.Request.Path == "/alive")
        {
            await next(context);
            return;
        }

        if (!options.Value.Enforce && !environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("AdminNetwork:Enforce cannot be disabled outside Development or Testing.");
        var decision = _policy.Evaluate(
            context.Connection.RemoteIpAddress,
            context.Request.Headers["X-Forwarded-For"].ToString());
        if (!decision.IsAllowed)
        {
            logger.LogWarning("Admin request denied by network policy. Reason={Reason}", decision.Reason);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Admin access is restricted.", context.RequestAborted);
            return;
        }

        await next(context);
    }
}
