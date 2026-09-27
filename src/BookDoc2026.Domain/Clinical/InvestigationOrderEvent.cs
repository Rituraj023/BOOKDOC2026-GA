using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class InvestigationOrderEvent : TenantScopedEntity
{
    private InvestigationOrderEvent() { }

    public long BranchId { get; private set; }
    public long OrderId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public long ActorId { get; private set; }
    public long? QueueTicketId { get; private set; }
    public long OrderVersion { get; private set; }

    public static InvestigationOrderEvent Record(
        InvestigationOrder order,
        string action,
        long actorId,
        long? queueTicketId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(order);
        var normalizedAction = action?.Trim() ?? string.Empty;
        if (normalizedAction.Length is < 2 or > 40 || actorId <= 0 || queueTicketId is <= 0)
            throw new DomainRuleException("Investigation event action, actor or queue link is invalid.");

        var item = new InvestigationOrderEvent
        {
            TenantId = order.TenantId,
            BranchId = order.BranchId,
            OrderId = order.Id,
            Action = normalizedAction,
            ActorId = actorId,
            QueueTicketId = queueTicketId,
            OrderVersion = order.Version
        };
        item.StampCreated(now);
        return item;
    }
}
