using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

public sealed class ResourceCapability : TenantScopedEntity
{
    private ResourceCapability()
    {
    }

    public long ResourceId { get; private set; }

    public long ServiceId { get; private set; }

    public int? DurationOverrideMinutes { get; private set; }

    public int CapacityRequired { get; private set; }

    public bool IsActive { get; private set; }

    public static ResourceCapability Create(
        long tenantId,
        long resourceId,
        long serviceId,
        int? durationOverrideMinutes,
        int capacityRequired,
        DateTimeOffset now)
    {
        if (durationOverrideMinutes is < 5 or > 1440 || capacityRequired is < 1 or > 1000)
        {
            throw new DomainRuleException("Resource capability duration or capacity is invalid.");
        }

        var capability = new ResourceCapability
        {
            TenantId = tenantId,
            ResourceId = resourceId,
            ServiceId = serviceId,
            DurationOverrideMinutes = durationOverrideMinutes,
            CapacityRequired = capacityRequired,
            IsActive = true
        };
        capability.StampCreated(now);
        return capability;
    }
}
