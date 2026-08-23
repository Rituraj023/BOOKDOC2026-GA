using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Queues;

public sealed class ImagingServicePoint : TenantScopedEntity
{
    private ImagingServicePoint() { }

    public long BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ImagingModality Modality { get; private set; }
    public long? ResourceId { get; private set; }
    public bool IsActive { get; private set; }
    public long Version { get; private set; } = 1;

    public static ImagingServicePoint Create(
        long tenantId,
        long branchId,
        string code,
        string name,
        ImagingModality modality,
        long? resourceId,
        DateTimeOffset now)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedName = name?.Trim() ?? string.Empty;
        if (tenantId <= 0 || branchId <= 0 || normalizedCode.Length is < 2 or > 20
            || normalizedName.Length is < 2 or > 120 || !Enum.IsDefined(modality))
            throw new DomainRuleException("Imaging service-point scope, code, name or modality is invalid.");

        var servicePoint = new ImagingServicePoint
        {
            TenantId = tenantId,
            BranchId = branchId,
            Code = normalizedCode,
            Name = normalizedName,
            Modality = modality,
            ResourceId = resourceId,
            IsActive = true
        };
        servicePoint.StampCreated(now);
        return servicePoint;
    }
}
