using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Identity;

public sealed class ApplicationUserScope : Entity
{
    private ApplicationUserScope()
    {
    }

    public uint UserId { get; private set; }

    public long TenantId { get; private set; }

    public long OrganizationId { get; private set; }

    public long? BranchId { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public static ApplicationUserScope Create(
        uint userId,
        long tenantId,
        long organizationId,
        long? branchId,
        bool isDefault,
        DateTimeOffset now)
    {
        if (userId == 0 || tenantId <= 0 || organizationId <= 0 || branchId is <= 0)
        {
            throw new DomainRuleException("A user scope requires valid user, tenant, organization and optional branch identifiers.");
        }

        var scope = new ApplicationUserScope
        {
            UserId = userId,
            TenantId = tenantId,
            OrganizationId = organizationId,
            BranchId = branchId,
            IsDefault = isDefault,
            IsActive = true
        };
        scope.StampCreated(now);
        return scope;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        IsDefault = isDefault;
        StampModified(now);
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        IsDefault = false;
        StampModified(now);
    }
}
