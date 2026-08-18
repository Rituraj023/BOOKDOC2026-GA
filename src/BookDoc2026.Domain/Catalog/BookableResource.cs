using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

public sealed class BookableResource : TenantScopedEntity
{
    private BookableResource()
    {
    }

    public long BranchId { get; private set; }

    public long CategoryId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public CapacityMode CapacityMode { get; private set; }

    public int Capacity { get; private set; }

    public string TimeZoneId { get; private set; } = "Asia/Kolkata";

    public long? ExternalReferenceId { get; private set; }

    public bool IsActive { get; private set; }

    public ResourceOperationalStatus OperationalStatus { get; private set; }

    public long Version { get; private set; } = 1;

    public static BookableResource Create(
        long tenantId,
        long branchId,
        long categoryId,
        string code,
        string name,
        CapacityMode capacityMode,
        int capacity,
        string timeZoneId,
        long? externalReferenceId,
        DateTimeOffset now)
    {
        ValidateCapacity(capacityMode, capacity);
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Trim().Length > 100)
        {
            throw new DomainRuleException("A valid resource time-zone identifier is required.");
        }

        var resource = new BookableResource
        {
            TenantId = tenantId,
            BranchId = branchId,
            CategoryId = categoryId,
            Code = CatalogCode.Normalize(code, "Resource"),
            Name = CatalogCode.RequiredName(name, "Resource"),
            CapacityMode = capacityMode,
            Capacity = capacity,
            TimeZoneId = timeZoneId.Trim(),
            ExternalReferenceId = externalReferenceId,
            IsActive = true,
            OperationalStatus = ResourceOperationalStatus.Available
        };
        resource.StampCreated(now);
        return resource;
    }

    public void Update(
        long expectedVersion,
        string name,
        CapacityMode capacityMode,
        int capacity,
        bool isActive,
        DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        ValidateCapacity(capacityMode, capacity);
        Name = CatalogCode.RequiredName(name, "Resource");
        CapacityMode = capacityMode;
        Capacity = capacity;
        IsActive = isActive;
        if (!isActive)
        {
            OperationalStatus = ResourceOperationalStatus.Unavailable;
        }

        Version++;
        StampModified(now);
    }

    public void ChangeStatus(
        long expectedVersion,
        ResourceOperationalStatus status,
        DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (!Enum.IsDefined(status))
        {
            throw new DomainRuleException("Resource operational status is not supported.");
        }

        if (!IsActive && status == ResourceOperationalStatus.Available)
        {
            throw new DomainRuleException("An inactive resource cannot be marked available.");
        }

        if (OperationalStatus == status)
        {
            throw new DomainRuleException("Resource is already in the requested operational status.");
        }

        OperationalStatus = status;
        Version++;
        StampModified(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException("The resource changed after it was loaded.");
        }
    }

    private static void ValidateCapacity(CapacityMode mode, int capacity)
    {
        if (!Enum.IsDefined(mode) || capacity is < 1 or > 1000)
        {
            throw new DomainRuleException("Resource capacity configuration is invalid.");
        }

        if (mode == CapacityMode.Exclusive && capacity != 1)
        {
            throw new DomainRuleException("An exclusive resource must have capacity one.");
        }
    }
}
