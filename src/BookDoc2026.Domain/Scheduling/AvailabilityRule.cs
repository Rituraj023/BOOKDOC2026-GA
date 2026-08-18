using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class AvailabilityRule : TenantScopedEntity
{
    private AvailabilityRule() { }

    public long BranchId { get; private set; }
    public long ResourceId { get; private set; }
    public long? ServiceId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly LocalStart { get; private set; }
    public TimeOnly LocalEnd { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public int SlotIntervalMinutes { get; private set; }
    public int Capacity { get; private set; }
    public bool IsActive { get; private set; }
    public long Version { get; private set; } = 1;

    public static AvailabilityRule Create(long tenantId, long branchId, long resourceId, long? serviceId,
        DayOfWeek dayOfWeek, TimeOnly localStart, TimeOnly localEnd, DateOnly effectiveFrom,
        DateOnly? effectiveTo, int slotIntervalMinutes, int capacity, DateTimeOffset now)
    {
        if (localStart >= localEnd || effectiveTo < effectiveFrom || slotIntervalMinutes is < 5 or > 1440 || capacity is < 1 or > 1000)
            throw new DomainRuleException("Availability rule timing, dates, interval, or capacity is invalid.");

        var rule = new AvailabilityRule
        {
            TenantId = tenantId,
            BranchId = branchId,
            ResourceId = resourceId,
            ServiceId = serviceId,
            DayOfWeek = dayOfWeek,
            LocalStart = localStart,
            LocalEnd = localEnd,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            SlotIntervalMinutes = slotIntervalMinutes,
            Capacity = capacity,
            IsActive = true
        };
        rule.StampCreated(now);
        return rule;
    }
}
