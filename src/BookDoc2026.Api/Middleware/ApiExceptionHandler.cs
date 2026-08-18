using BookDoc2026.Contracts.Common;
using BookDoc2026.ErrorHandling;
using Microsoft.AspNetCore.Diagnostics;

namespace BookDoc2026.Api.Middleware;

public sealed class ApiExceptionHandler(
    ExceptionErrorResolver errors,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
        {
            return false;
        }

        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var error = errors.Resolve(exception);
        var status = ToStatusCode(error.Category);

        if (error.Category == ErrorCategory.Internal)
        {
            logger.LogError(
                exception,
                "Unhandled API exception. ErrorCode={ErrorCode} TraceId={TraceId}",
                error.Code,
                context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                "Handled API exception. ErrorCode={ErrorCode} Category={ErrorCategory} TraceId={TraceId}",
                error.Code,
                error.Category,
                context.TraceIdentifier);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new ApiError(
                error.Code,
                error.SafeMessage,
                context.TraceIdentifier,
                error.Category,
                error.IsTransient),
            cancellationToken);
        return true;
    }

    private static int ToStatusCode(ErrorCategory category) => category switch
    {
        ErrorCategory.Authentication => StatusCodes.Status401Unauthorized,
        ErrorCategory.Authorization => StatusCodes.Status403Forbidden,
        ErrorCategory.NotFound => StatusCodes.Status404NotFound,
        ErrorCategory.Conflict => StatusCodes.Status409Conflict,
        ErrorCategory.RateLimited => StatusCodes.Status429TooManyRequests,
        ErrorCategory.Dependency or ErrorCategory.Unavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorCategory.Timeout => StatusCodes.Status504GatewayTimeout,
        ErrorCategory.Cancelled => StatusCodes.Status408RequestTimeout,
        ErrorCategory.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}
