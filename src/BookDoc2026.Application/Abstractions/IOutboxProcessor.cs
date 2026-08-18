namespace BookDoc2026.Application.Abstractions;

public interface IOutboxProcessor
{
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken);
}

public interface IOutboxMessageHandler
{
    string MessageType { get; }

    Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken);
}

public sealed record OutboxMessageContext(
    long MessageId,
    long? TenantId,
    long? BranchId,
    string MessageType,
    string PayloadJson,
    Guid OperationId,
    string CorrelationId,
    int AttemptNumber);

public sealed class OutboxDispatchException(
    string errorCode,
    bool isTransient,
    string safeMessage) : InvalidOperationException(safeMessage)
{
    public string ErrorCode { get; } = errorCode;

    public bool IsTransient { get; } = isTransient;
}
