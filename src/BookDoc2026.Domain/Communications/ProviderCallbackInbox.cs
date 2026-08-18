using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Communications;

public sealed class ProviderCallbackInbox : Entity
{
    private ProviderCallbackInbox() { }

    public long TenantId { get; private set; }
    public long? BranchId { get; private set; }
    public long DeliveryAttemptId { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public string ProviderMessageId { get; private set; } = string.Empty;
    public ProviderDeliveryStatus DeliveryStatus { get; private set; }
    public DateTimeOffset OccurredUtc { get; private set; }
    public string SignatureKeyId { get; private set; } = string.Empty;
    public string PayloadSha256 { get; private set; } = string.Empty;
    public ProviderCallbackInboxStatus Status { get; private set; }
    public DateTimeOffset? ProcessedUtc { get; private set; }
    public string? ErrorCode { get; private set; }

    public static ProviderCallbackInbox Receive(
        long tenantId,
        long? branchId,
        long deliveryAttemptId,
        string providerCode,
        string externalEventId,
        string providerMessageId,
        ProviderDeliveryStatus deliveryStatus,
        DateTimeOffset occurredUtc,
        string signatureKeyId,
        string payloadSha256,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || deliveryAttemptId <= 0 || !Enum.IsDefined(deliveryStatus))
            throw new DomainRuleException("Provider callback identity is invalid.");
        Validate(providerCode, 80, "Provider code");
        Validate(externalEventId, 200, "External event identifier");
        Validate(providerMessageId, 200, "Provider message identifier");
        Validate(signatureKeyId, 100, "Signature key identifier");
        if (payloadSha256.Length != 64 || payloadSha256.Any(character => !Uri.IsHexDigit(character)))
            throw new DomainRuleException("Provider callback payload hash is invalid.");

        var callback = new ProviderCallbackInbox
        {
            TenantId = tenantId,
            BranchId = branchId,
            DeliveryAttemptId = deliveryAttemptId,
            ProviderCode = providerCode.Trim(),
            ExternalEventId = externalEventId.Trim(),
            ProviderMessageId = providerMessageId.Trim(),
            DeliveryStatus = deliveryStatus,
            OccurredUtc = occurredUtc,
            SignatureKeyId = signatureKeyId.Trim(),
            PayloadSha256 = payloadSha256.ToUpperInvariant(),
            Status = ProviderCallbackInboxStatus.Pending
        };
        callback.StampCreated(now);
        return callback;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        if (Status == ProviderCallbackInboxStatus.Processed) return;
        if (Status != ProviderCallbackInboxStatus.Pending)
            throw new DomainRuleException("Only a pending provider callback can be processed.");
        Status = ProviderCallbackInboxStatus.Processed;
        ProcessedUtc = now;
        ErrorCode = null;
        StampModified(now);
    }

    public void MarkFailed(string errorCode, DateTimeOffset now)
    {
        if (Status != ProviderCallbackInboxStatus.Pending)
            throw new DomainRuleException("Only a pending provider callback can fail.");
        Validate(errorCode, 120, "Callback error code");
        Status = ProviderCallbackInboxStatus.Failed;
        ErrorCode = errorCode.Trim();
        ProcessedUtc = now;
        StampModified(now);
    }

    private static void Validate(string value, int maximum, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximum)
            throw new DomainRuleException($"{label} is required and must not exceed {maximum} characters.");
    }
}
