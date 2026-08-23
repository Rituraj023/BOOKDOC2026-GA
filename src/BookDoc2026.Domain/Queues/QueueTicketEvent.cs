using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Queues;

public sealed class QueueTicketEvent : TenantScopedEntity
{
    private QueueTicketEvent() { }

    public long BranchId { get; private set; }
    public long TicketId { get; private set; }
    public QueueTicketStatus? FromStatus { get; private set; }
    public QueueTicketStatus ToStatus { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public long ActorId { get; private set; }
    public string? Reason { get; private set; }
    public long TicketVersion { get; private set; }

    public static QueueTicketEvent Record(
        QueueTicket ticket,
        QueueTicketStatus? fromStatus,
        string action,
        long actorId,
        string? reason,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var normalizedAction = action?.Trim() ?? string.Empty;
        if (normalizedAction.Length is < 2 or > 40 || actorId <= 0)
            throw new DomainRuleException("Queue event action or actor is invalid.");
        var item = new QueueTicketEvent
        {
            TenantId = ticket.TenantId,
            BranchId = ticket.BranchId,
            TicketId = ticket.Id,
            FromStatus = fromStatus,
            ToStatus = ticket.Status,
            Action = normalizedAction,
            ActorId = actorId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : QueueTicket.NormalizeReason(reason),
            TicketVersion = ticket.Version
        };
        item.StampCreated(now);
        return item;
    }
}
