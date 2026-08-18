using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Communications;

public sealed class MessageDeliveryAttempt : Entity
{
    private MessageDeliveryAttempt()
    {
    }

    public long TenantId { get; private set; }

    public long? BranchId { get; private set; }

    public long OutboxMessageId { get; private set; }

    public Guid OperationId { get; private set; }

    public int AttemptNumber { get; private set; }

    public CommunicationChannel Channel { get; private set; }

    public string TemplateKey { get; private set; } = string.Empty;

    public int TemplateVersion { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public string RecipientHint { get; private set; } = string.Empty;

    public MessageDeliveryStatus Status { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public string? ErrorCode { get; private set; }

    public static MessageDeliveryAttempt Record(
        long tenantId,
        long? branchId,
        long outboxMessageId,
        Guid operationId,
        int attemptNumber,
        CommunicationChannel channel,
        string templateKey,
        int templateVersion,
        string providerCode,
        string recipientHint,
        MessageDeliveryStatus status,
        string? providerMessageId,
        string? errorCode,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || outboxMessageId <= 0 || operationId == Guid.Empty || attemptNumber <= 0)
        {
            throw new DomainRuleException("Delivery attempt identity is invalid.");
        }

        ValidateText(templateKey, 160, "Template key");
        ValidateText(providerCode, 80, "Provider code");
        ValidateText(recipientHint, 160, "Recipient hint");
        if (templateVersion <= 0)
        {
            throw new DomainRuleException("Delivery template version must be positive.");
        }

        if (providerMessageId?.Length > 200 || errorCode?.Length > 120)
        {
            throw new DomainRuleException("Delivery result metadata exceeds the supported length.");
        }

        var attempt = new MessageDeliveryAttempt
        {
            TenantId = tenantId,
            BranchId = branchId,
            OutboxMessageId = outboxMessageId,
            OperationId = operationId,
            AttemptNumber = attemptNumber,
            Channel = channel,
            TemplateKey = templateKey,
            TemplateVersion = templateVersion,
            ProviderCode = providerCode,
            RecipientHint = recipientHint,
            Status = status,
            ProviderMessageId = providerMessageId,
            ErrorCode = errorCode
        };
        attempt.StampCreated(now);
        return attempt;
    }

    private static void ValidateText(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
        {
            throw new DomainRuleException($"{field} is required and must not exceed {maxLength} characters.");
        }
    }
}
