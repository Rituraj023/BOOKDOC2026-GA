using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

public sealed class ResourceStatusEvent : TenantScopedEntity
{
    private ResourceStatusEvent()
    {
    }

    public long BranchId { get; private set; }

    public long ResourceId { get; private set; }

    public ResourceOperationalStatus FromStatus { get; private set; }

    public ResourceOperationalStatus ToStatus { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public long ActorId { get; private set; }

    public static ResourceStatusEvent Record(
        long tenantId,
        long branchId,
        long resourceId,
        ResourceOperationalStatus fromStatus,
        ResourceOperationalStatus toStatus,
        string reason,
        long actorId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
        {
            throw new DomainRuleException("A resource status reason is required and cannot exceed 500 characters.");
        }

        var statusEvent = new ResourceStatusEvent
        {
            TenantId = tenantId,
            BranchId = branchId,
            ResourceId = resourceId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason.Trim(),
            ActorId = actorId
        };
        statusEvent.StampCreated(now);
        return statusEvent;
    }
}
