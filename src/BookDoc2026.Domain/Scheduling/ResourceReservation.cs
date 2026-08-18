using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class ResourceReservation : TenantScopedEntity
{
    private ResourceReservation() { }

    public long BranchId { get; private set; }
    public long HoldId { get; private set; }
    public long ResourceId { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public int Quantity { get; private set; }

    public static ResourceReservation Create(long tenantId, long branchId, long holdId, long resourceId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, int quantity, DateTimeOffset now)
    {
        if (quantity is < 1 or > 1000 || startUtc >= endUtc) throw new DomainRuleException("Resource reservation is invalid.");
        var item = new ResourceReservation
        {
            TenantId = tenantId,
            BranchId = branchId,
            HoldId = holdId,
            ResourceId = resourceId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Quantity = quantity
        };
        item.StampCreated(now);
        return item;
    }
}
