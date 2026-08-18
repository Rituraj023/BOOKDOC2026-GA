using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class AvailabilityException : TenantScopedEntity
{
    private AvailabilityException() { }

    public long BranchId { get; private set; }
    public long ResourceId { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public AvailabilityExceptionKind Kind { get; private set; }
    public int? CapacityOverride { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public static AvailabilityException Create(long tenantId, long branchId, long resourceId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, AvailabilityExceptionKind kind,
        int? capacityOverride, string reason, DateTimeOffset now)
    {
        if (startUtc >= endUtc || !Enum.IsDefined(kind) || reason.Trim().Length is < 1 or > 500
            || (kind == AvailabilityExceptionKind.CapacityOverride && capacityOverride is not (>= 1 and <= 1000))
            || (kind == AvailabilityExceptionKind.Unavailable && capacityOverride is not null))
            throw new DomainRuleException("Availability exception is invalid.");

        var item = new AvailabilityException
        {
            TenantId = tenantId,
            BranchId = branchId,
            ResourceId = resourceId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Kind = kind,
            CapacityOverride = capacityOverride,
            Reason = reason.Trim()
        };
        item.StampCreated(now);
        return item;
    }
}
