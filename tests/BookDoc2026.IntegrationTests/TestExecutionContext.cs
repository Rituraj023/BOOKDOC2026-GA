using BookDoc2026.Application.Abstractions;

namespace BookDoc2026.IntegrationTests;

internal sealed class TestExecutionContext : ICurrentActor, ICorrelationContext
{
    private HashSet<long> _branchIds = [];
    private HashSet<string> _permissions = new(StringComparer.Ordinal);

    public long ActorId { get; private set; } = 1;

    public long? TenantId { get; private set; }

    public bool IsPlatformOperator { get; private set; }

    public IReadOnlySet<long> BranchIds => _branchIds;

    public string CorrelationId { get; private set; } = $"test-{Guid.NewGuid():N}";

    public bool HasPermission(string permission) => _permissions.Contains(permission);

    public void BecomePlatform(params string[] permissions)
    {
        ActorId++;
        TenantId = null;
        IsPlatformOperator = true;
        _branchIds = [];
        _permissions = permissions.ToHashSet(StringComparer.Ordinal);
        CorrelationId = $"test-{Guid.NewGuid():N}";
    }

    public void BecomeTenant(long tenantId, IEnumerable<long> branchIds, params string[] permissions)
    {
        ActorId++;
        TenantId = tenantId;
        IsPlatformOperator = false;
        _branchIds = branchIds.ToHashSet();
        _permissions = permissions.ToHashSet(StringComparer.Ordinal);
        CorrelationId = $"test-{Guid.NewGuid():N}";
    }
}
