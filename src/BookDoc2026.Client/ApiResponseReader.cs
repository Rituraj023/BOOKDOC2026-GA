using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookDoc2026.Contracts.Common;
using BookDoc2026.ErrorHandling;

namespace BookDoc2026.Client;

internal static class ApiResponseReader
{
    public static async Task<T> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            ApiError? apiError = null;
            try
            {
                apiError = await response.Content.ReadFromJsonAsync<ApiError>(
                    cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                // Gateways and proxies may return a non-BOOKDOC error body.
            }
            catch (NotSupportedException)
            {
                // Treat an unsupported content type as a transport-level response.
            }

            var category = apiError?.Category is not null and not ErrorCategory.Unknown
                ? apiError.Category
                : CategoryFromStatus(response.StatusCode);
            var transient = apiError?.IsTransient == true
                || (int)response.StatusCode >= 500
                || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
            var descriptor = new ErrorDescriptor(
                apiError?.Code ?? CodeFromStatus(response.StatusCode),
                category,
                apiError?.Message ?? "The service could not complete the request.",
                transient);

            throw new RemoteErrorException(
                descriptor,
                apiError?.TraceId,
                (int)response.StatusCode);
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(
            cancellationToken: cancellationToken);
        if (envelope is null)
        {
            throw new RemoteErrorException(
                new ErrorDescriptor(
                    ErrorCodes.InvalidResponse,
                    ErrorCategory.Dependency,
                    "The service returned an invalid response."),
                traceId: null,
                (int)response.StatusCode);
        }

        return envelope.Data;
    }

    private static ErrorCategory CategoryFromStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ErrorCategory.Validation,
        HttpStatusCode.Unauthorized => ErrorCategory.Authentication,
        HttpStatusCode.Forbidden => ErrorCategory.Authorization,
        HttpStatusCode.NotFound => ErrorCategory.NotFound,
        HttpStatusCode.Conflict => ErrorCategory.Conflict,
        HttpStatusCode.TooManyRequests => ErrorCategory.RateLimited,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ErrorCategory.Timeout,
        HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway => ErrorCategory.Unavailable,
        _ when (int)status >= 500 => ErrorCategory.Dependency,
        _ => ErrorCategory.Unknown
    };

    private static string CodeFromStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => ErrorCodes.Unauthorized,
        HttpStatusCode.Forbidden => ErrorCodes.Forbidden,
        HttpStatusCode.NotFound => ErrorCodes.NotFound,
        HttpStatusCode.Conflict => ErrorCodes.ConcurrencyConflict,
        HttpStatusCode.TooManyRequests => ErrorCodes.RateLimited,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ErrorCodes.Timeout,
        HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway => ErrorCodes.Unavailable,
        _ when (int)status >= 500 => ErrorCodes.DependencyFailure,
        _ => ErrorCodes.Validation
    };
}
