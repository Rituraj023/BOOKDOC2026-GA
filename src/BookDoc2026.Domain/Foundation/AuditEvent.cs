namespace BookDoc2026.Domain.Foundation;

public sealed class AuditEvent : Common.Entity
{
    private AuditEvent()
    {
    }

    public long? TenantId { get; private set; }

    public long? BranchId { get; private set; }

    public long ActorId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public long EntityId { get; private set; }

    public string DataJson { get; private set; } = "{}";

    public string CorrelationId { get; private set; } = string.Empty;

    public static AuditEvent Record(
        long? tenantId,
        long? branchId,
        long actorId,
        string action,
        string entityType,
        long entityId,
        string dataJson,
        string correlationId,
        DateTimeOffset now)
    {
        var auditEvent = new AuditEvent
        {
            TenantId = tenantId,
            BranchId = branchId,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DataJson = dataJson,
            CorrelationId = correlationId
        };
        auditEvent.StampCreated(now);
        return auditEvent;
    }
}
