using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Workforce;

public sealed class PractitionerProfile : TenantScopedEntity
{
    private PractitionerProfile() { }

    public long StakeholderId { get; private set; }
    public uint IdentitySubjectId { get; private set; }
    public string PractitionerCode { get; private set; } = string.Empty;
    public string PractitionerTypeCode { get; private set; } = string.Empty;
    public PractitionerStatus Status { get; private set; }
    public long Version { get; private set; } = 1;

    public static PractitionerProfile Create(long tenantId, long stakeholderId, long identitySubjectId,
        string practitionerCode, string practitionerTypeCode, DateTimeOffset now)
    {
        if (tenantId <= 0 || stakeholderId <= 0 || identitySubjectId is <= 0 or > uint.MaxValue)
            throw new DomainRuleException("Practitioner tenant, person stakeholder and identity subject are required.");
        var profile = new PractitionerProfile
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            IdentitySubjectId = (uint)identitySubjectId,
            PractitionerCode = Code(practitionerCode, "Practitioner code"),
            PractitionerTypeCode = Code(practitionerTypeCode, "Practitioner type code"),
            Status = PractitionerStatus.Pending
        };
        profile.StampCreated(now);
        return profile;
    }

    public void Activate(long expectedVersion, bool hasCurrentVerifiedCredential, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (!hasCurrentVerifiedCredential)
            throw new DomainRuleException("A current verified credential is required before practitioner activation.");
        if (Status == PractitionerStatus.Active)
            throw new DomainRuleException("Practitioner is already active.");
        Status = PractitionerStatus.Active;
        Version++;
        StampModified(now);
    }

    public void Suspend(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != PractitionerStatus.Active)
            throw new DomainRuleException("Only an active practitioner can be suspended.");
        Status = PractitionerStatus.Suspended;
        Version++;
        StampModified(now);
    }

    public void Deactivate(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status == PractitionerStatus.Inactive)
            throw new DomainRuleException("Practitioner is already inactive.");
        Status = PractitionerStatus.Inactive;
        Version++;
        StampModified(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The practitioner changed after it was loaded.");
    }

    internal static string Code(string value, string label)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < 2 or > 40 || normalized.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new DomainRuleException($"{label} must contain 2 to 40 letters, numbers, hyphens or underscores.");
        return normalized;
    }
}
