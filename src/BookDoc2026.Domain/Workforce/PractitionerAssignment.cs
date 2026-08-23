using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Workforce;

public sealed class PractitionerAssignment : TenantScopedEntity
{
    private PractitionerAssignment() { }

    public long PractitionerId { get; private set; }
    public long BranchId { get; private set; }
    public long ServiceId { get; private set; }
    public long? BookableResourceId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public PractitionerAssignmentStatus Status { get; private set; }
    public long Version { get; private set; } = 1;

    public static PractitionerAssignment Create(long tenantId, long practitionerId, long branchId, long serviceId,
        long? bookableResourceId, string roleCode, DateOnly effectiveFrom, DateOnly? effectiveTo,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || practitionerId <= 0 || branchId <= 0 || serviceId <= 0
            || bookableResourceId is <= 0)
            throw new DomainRuleException("Practitioner assignment scope is invalid.");
        if (effectiveTo < effectiveFrom)
            throw new DomainRuleException("Assignment end cannot precede its start.");
        var assignment = new PractitionerAssignment
        {
            TenantId = tenantId,
            PractitionerId = practitionerId,
            BranchId = branchId,
            ServiceId = serviceId,
            BookableResourceId = bookableResourceId,
            RoleCode = PractitionerProfile.Code(roleCode, "Assignment role code"),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Status = PractitionerAssignmentStatus.Active
        };
        assignment.StampCreated(now);
        return assignment;
    }

    public bool IsEffective(DateOnly date) => Status == PractitionerAssignmentStatus.Active
        && date >= EffectiveFrom && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);

    public void Suspend(long expectedVersion, DateTimeOffset now) => Transition(expectedVersion,
        PractitionerAssignmentStatus.Active, PractitionerAssignmentStatus.Suspended, now);

    public void End(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status == PractitionerAssignmentStatus.Ended)
            throw new DomainRuleException("Practitioner assignment is already ended.");
        Status = PractitionerAssignmentStatus.Ended;
        Version++;
        StampModified(now);
    }

    private void Transition(long expectedVersion, PractitionerAssignmentStatus required,
        PractitionerAssignmentStatus target, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != required) throw new DomainRuleException("Practitioner assignment is not active.");
        Status = target;
        Version++;
        StampModified(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The practitioner assignment changed after it was loaded.");
    }
}
