using BookDoc2026.ErrorHandling;

namespace BookDoc2026.Contracts.Common;

public sealed record ApiEnvelope<T>(T Data, string TraceId);

public sealed record ApiError(
    string Code,
    string Message,
    string TraceId,
    ErrorCategory Category,
    bool IsTransient = false);
