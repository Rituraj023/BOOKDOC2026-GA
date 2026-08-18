namespace BookDoc2026.ErrorHandling;

public sealed class RemoteErrorException(
    ErrorDescriptor error,
    string? traceId,
    int? transportStatusCode = null,
    Exception? innerException = null)
    : Exception(error.SafeMessage, innerException)
{
    public ErrorDescriptor Error { get; } = error;

    public string? TraceId { get; } = traceId;

    public int? TransportStatusCode { get; } = transportStatusCode;
}
