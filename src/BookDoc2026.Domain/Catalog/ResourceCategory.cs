using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

public sealed class ResourceCategory : TenantScopedEntity
{
    private ResourceCategory()
    {
    }

    public long? ParentCategoryId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ResourceKind Kind { get; private set; }

    public bool IsActive { get; private set; }

    public long Version { get; private set; } = 1;

    public static ResourceCategory Create(
        long tenantId,
        long? parentCategoryId,
        string code,
        string name,
        ResourceKind kind,
        DateTimeOffset now)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainRuleException("Resource kind is not supported.");
        }

        var category = new ResourceCategory
        {
            TenantId = tenantId,
            ParentCategoryId = parentCategoryId,
            Code = CatalogCode.Normalize(code, "Resource category"),
            Name = CatalogCode.RequiredName(name, "Resource category"),
            Kind = kind,
            IsActive = true
        };
        category.StampCreated(now);
        return category;
    }

    public void Update(long expectedVersion, string name, bool isActive, DateTimeOffset now)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException("The resource category changed after it was loaded.");
        }

        Name = CatalogCode.RequiredName(name, "Resource category");
        IsActive = isActive;
        Version++;
        StampModified(now);
    }
}
