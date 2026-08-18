using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

public sealed class ServiceResourceRequirement : TenantScopedEntity
{
    private ServiceResourceRequirement()
    {
    }

    public long ServiceId { get; private set; }

    public long CategoryId { get; private set; }

    public string RoleCode { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public bool IsOptional { get; private set; }

    public static ServiceResourceRequirement Create(
        long tenantId,
        long serviceId,
        long categoryId,
        string roleCode,
        int quantity,
        bool isOptional,
        DateTimeOffset now)
    {
        if (quantity is < 1 or > 1000)
        {
            throw new DomainRuleException("Required resource quantity must be between 1 and 1000.");
        }

        var requirement = new ServiceResourceRequirement
        {
            TenantId = tenantId,
            ServiceId = serviceId,
            CategoryId = categoryId,
            RoleCode = CatalogCode.Normalize(roleCode, "Resource role"),
            Quantity = quantity,
            IsOptional = isOptional
        };
        requirement.StampCreated(now);
        return requirement;
    }
}
