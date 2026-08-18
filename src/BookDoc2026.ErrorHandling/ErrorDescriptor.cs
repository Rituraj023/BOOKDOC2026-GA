namespace BookDoc2026.ErrorHandling;

public sealed record ErrorDescriptor(
    string Code,
    ErrorCategory Category,
    string SafeMessage,
    bool IsTransient = false)
{
    public static ErrorDescriptor Internal() => new(
        ErrorCodes.Internal,
        ErrorCategory.Internal,
        "The operation could not be completed.");
}

public sealed record ErrorOccurrence(ErrorDescriptor Error, string? TraceId = null);

public interface IExceptionErrorMapper
{
    bool TryMap(Exception exception, out ErrorDescriptor descriptor);
}
