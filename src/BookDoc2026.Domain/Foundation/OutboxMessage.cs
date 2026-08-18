using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Foundation;

public sealed class OutboxMessage : Entity
{
    private OutboxMessage()
    {
    }

    public long? TenantId { get; private set; }

    public long? BranchId { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = "{}";

    public Guid OperationId { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public OutboxStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset NextAttemptUtc { get; private set; }

    public string? LeaseOwner { get; private set; }

    public DateTimeOffset? LeaseUntilUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public long Version { get; private set; } = 1;

    public static OutboxMessage Enqueue(
        long? tenantId,
        long? branchId,
        string messageType,
        string payloadJson,
        DateTimeOffset now,
        Guid operationId,
        string correlationId)
    {
        if (operationId == Guid.Empty)
        {
            throw new DomainRuleException("Outbox operation identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100)
        {
            throw new DomainRuleException("Outbox correlation identifier is required and must not exceed 100 characters.");
        }

        var message = new OutboxMessage
        {
            TenantId = tenantId,
            BranchId = branchId,
            MessageType = messageType,
            PayloadJson = payloadJson,
            OperationId = operationId,
            CorrelationId = correlationId,
            Status = OutboxStatus.Pending,
            NextAttemptUtc = now
        };
        message.StampCreated(now);
        return message;
    }

    public void Claim(string workerId, DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (Status is OutboxStatus.Completed or OutboxStatus.DeadLetter
            || NextAttemptUtc > now
            || LeaseUntilUtc > now)
        {
            throw new DomainRuleException("Outbox message is not available for processing.");
        }

        Status = OutboxStatus.Processing;
        LeaseOwner = workerId;
        LeaseUntilUtc = now.Add(leaseDuration);
        AttemptCount++;
        Version++;
        StampModified(now);
    }

    public void Complete(string workerId, DateTimeOffset now)
    {
        EnsureLease(workerId);
        Status = OutboxStatus.Completed;
        LeaseOwner = null;
        LeaseUntilUtc = null;
        LastErrorCode = null;
        Version++;
        StampModified(now);
    }

    public void Fail(string workerId, string errorCode, bool retryable, DateTimeOffset now, int maxAttempts)
    {
        EnsureLease(workerId);
        if (string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 120)
        {
            throw new DomainRuleException("A safe outbox error code is required.");
        }

        LastErrorCode = errorCode;
        LeaseOwner = null;
        LeaseUntilUtc = null;
        Status = !retryable || AttemptCount >= maxAttempts
            ? OutboxStatus.DeadLetter
            : OutboxStatus.RetryScheduled;
        NextAttemptUtc = now.AddSeconds(Math.Min(300, Math.Pow(2, AttemptCount)));
        Version++;
        StampModified(now);
    }

    private void EnsureLease(string workerId)
    {
        if (Status != OutboxStatus.Processing || !string.Equals(LeaseOwner, workerId, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Outbox lease is not owned by this worker.");
        }
    }
}
