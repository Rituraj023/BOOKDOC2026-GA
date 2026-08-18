using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Client;
using BookDoc2026.Contracts.Common;
using BookDoc2026.ErrorHandling;

namespace BookDoc2026.UnitTests;

public sealed class ErrorHandlingTests
{
    [Fact]
    public void Resolver_UsesExtensionMapperAndSafeFallback()
    {
        var resolver = new ExceptionErrorResolver([new StubExceptionMapper()]);

        var known = resolver.Resolve(new InvalidOperationException("unsafe internal text"));
        var unknown = resolver.Resolve(new Exception("database connection text"));

        Assert.Equal(ErrorCodes.BusinessRule, known.Code);
        Assert.Equal(ErrorCategory.Validation, known.Category);
        Assert.Equal(ErrorCodes.Internal, unknown.Code);
        Assert.DoesNotContain("database", unknown.SafeMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecoveryPolicy_IsPresentationNeutralAndCategoryDriven()
    {
        Assert.Equal(
            ErrorRecoveryAction.Reauthenticate,
            ErrorRecoveryPolicy.Recommend(new ErrorDescriptor(
                ErrorCodes.Unauthorized,
                ErrorCategory.Authentication,
                "Sign in again.")));
        Assert.Equal(
            ErrorRecoveryAction.Retry,
            ErrorRecoveryPolicy.Recommend(new ErrorDescriptor(
                ErrorCodes.DependencyFailure,
                ErrorCategory.Dependency,
                "Temporarily unavailable.",
                IsTransient: true)));
    }

    [Fact]
    public async Task TypedClient_PreservesRemoteCodeCategoryTraceAndStatus()
    {
        var apiError = new ApiError(
            ErrorCodes.ConcurrencyConflict,
            "The record changed.",
            "trace-123",
            ErrorCategory.Conflict);
        using var http = new HttpClient(new StubHttpHandler(
            new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonContent.Create(apiError)
            }))
        {
            BaseAddress = new Uri("https://bookdoc.test")
        };
        var client = new BookDocApiClient(http);

        var exception = await Assert.ThrowsAsync<RemoteErrorException>(() =>
            client.GetAsync<object>("api/v1/test"));

        Assert.Equal(ErrorCodes.ConcurrencyConflict, exception.Error.Code);
        Assert.Equal(ErrorCategory.Conflict, exception.Error.Category);
        Assert.Equal("trace-123", exception.TraceId);
        Assert.Equal(409, exception.TransportStatusCode);
        Assert.Equal(ErrorRecoveryAction.Refresh, ErrorRecoveryPolicy.Recommend(exception.Error));
    }

    private sealed class StubExceptionMapper : IExceptionErrorMapper
    {
        public bool TryMap(Exception exception, out ErrorDescriptor descriptor)
        {
            if (exception is InvalidOperationException)
            {
                descriptor = new ErrorDescriptor(
                    ErrorCodes.BusinessRule,
                    ErrorCategory.Validation,
                    "The operation is not valid.");
                return true;
            }

            descriptor = null!;
            return false;
        }
    }

    private sealed class StubHttpHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }
}
