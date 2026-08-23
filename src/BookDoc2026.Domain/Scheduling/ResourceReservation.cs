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
    public string? RequirementRoleCode { get; private set; }

    public static ResourceReservation Create(long tenantId, long branchId, long holdId, long resourceId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, int quantity, DateTimeOffset now,
        string? requirementRoleCode = null)
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
            Quantity = quantity,
            RequirementRoleCode = NormalizeRole(requirementRoleCode)
        };
        item.StampCreated(now);
        return item;
    }

    private static string? NormalizeRole(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 40)
            throw new DomainRuleException("Resource requirement role cannot exceed 40 characters.");
        return normalized;
    }
}
