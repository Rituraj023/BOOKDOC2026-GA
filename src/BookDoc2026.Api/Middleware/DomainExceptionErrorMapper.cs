using BookDoc2026.Domain.Common;
using BookDoc2026.ErrorHandling;

namespace BookDoc2026.Api.Middleware;

public sealed class DomainExceptionErrorMapper : IExceptionErrorMapper
{
    public bool TryMap(Exception exception, out ErrorDescriptor descriptor)
    {
        descriptor = exception switch
        {
            UnauthorizedException => Known(
                ErrorCodes.Unauthorized,
                ErrorCategory.Authentication,
                exception.Message),
            ForbiddenException => Known(
                ErrorCodes.Forbidden,
                ErrorCategory.Authorization,
                exception.Message),
            NotFoundException => Known(
                ErrorCodes.NotFound,
                ErrorCategory.NotFound,
                exception.Message),
            ConcurrencyConflictException => Known(
                ErrorCodes.ConcurrencyConflict,
                ErrorCategory.Conflict,
                exception.Message),
            InvalidPublicIdException => Known(
                ErrorCodes.InvalidIdentifier,
                ErrorCategory.Validation,
                exception.Message),
            DomainRuleException => Known(
                ErrorCodes.BusinessRule,
                ErrorCategory.Validation,
                exception.Message),
            _ => null!
        };

        return descriptor is not null;
    }

    private static ErrorDescriptor Known(string code, ErrorCategory category, string safeMessage) =>
        new(code, category, safeMessage);
}
