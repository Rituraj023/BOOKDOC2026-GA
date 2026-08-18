using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Communications;

public sealed class MessageDeliveryStatusEvent : Entity
{
    private MessageDeliveryStatusEvent() { }

    public long TenantId { get; private set; }
    public long? BranchId { get; private set; }
    public long DeliveryAttemptId { get; private set; }
    public long CallbackInboxId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public ProviderDeliveryStatus Status { get; private set; }
    public DateTimeOffset OccurredUtc { get; private set; }

    public static MessageDeliveryStatusEvent Record(
        ProviderCallbackInbox callback,
        Guid operationId,
        DateTimeOffset now)
    {
        if (operationId == Guid.Empty)
            throw new DomainRuleException("Delivery status operation identifier is required.");
        var statusEvent = new MessageDeliveryStatusEvent
        {
            TenantId = callback.TenantId,
            BranchId = callback.BranchId,
            DeliveryAttemptId = callback.DeliveryAttemptId,
            CallbackInboxId = callback.Id,
            OperationId = operationId,
            ProviderCode = callback.ProviderCode,
            ExternalEventId = callback.ExternalEventId,
            Status = callback.DeliveryStatus,
            OccurredUtc = callback.OccurredUtc
        };
        statusEvent.StampCreated(now);
        return statusEvent;
    }
}
