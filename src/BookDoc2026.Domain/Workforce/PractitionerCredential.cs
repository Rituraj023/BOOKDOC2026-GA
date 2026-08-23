using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Workforce;

public sealed class PractitionerCredential : TenantScopedEntity
{
    private PractitionerCredential() { }

    public long PractitionerId { get; private set; }
    public string CredentialTypeCode { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public string IssuingAuthority { get; private set; } = string.Empty;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public CredentialVerificationStatus VerificationStatus { get; private set; }
    public long? VerifiedByActorId { get; private set; }
    public DateTimeOffset? VerifiedUtc { get; private set; }
    public string? DecisionReason { get; private set; }
    public long Version { get; private set; } = 1;

    public static PractitionerCredential Create(long tenantId, long practitionerId, string credentialTypeCode,
        string registrationNumber, string issuingAuthority, DateOnly validFrom, DateOnly? validTo,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || practitionerId <= 0)
            throw new DomainRuleException("Credential practitioner scope is required.");
        if (validTo < validFrom)
            throw new DomainRuleException("Credential validity end cannot precede its start.");
        var credential = new PractitionerCredential
        {
            TenantId = tenantId,
            PractitionerId = practitionerId,
            CredentialTypeCode = PractitionerProfile.Code(credentialTypeCode, "Credential type code"),
            RegistrationNumber = Required(registrationNumber, 100, "Registration number"),
            IssuingAuthority = Required(issuingAuthority, 160, "Issuing authority"),
            ValidFrom = validFrom,
            ValidTo = validTo,
            VerificationStatus = CredentialVerificationStatus.Pending
        };
        credential.StampCreated(now);
        return credential;
    }

    public void Verify(long expectedVersion, long actorId, DateTimeOffset now)
    {
        Decide(expectedVersion, actorId, CredentialVerificationStatus.Verified, null, now);
    }

    public void Reject(long expectedVersion, long actorId, string reason, DateTimeOffset now)
    {
        var normalized = Required(reason, 500, "Rejection reason");
        Decide(expectedVersion, actorId, CredentialVerificationStatus.Rejected, normalized, now);
    }

    public bool IsCurrent(DateOnly date) => VerificationStatus == CredentialVerificationStatus.Verified
        && date >= ValidFrom && (!ValidTo.HasValue || date <= ValidTo.Value);

    private void Decide(long expectedVersion, long actorId, CredentialVerificationStatus status,
        string? reason, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The practitioner credential changed after it was loaded.");
        if (VerificationStatus != CredentialVerificationStatus.Pending)
            throw new DomainRuleException("A credential decision is final; add a replacement credential for new evidence.");
        if (actorId <= 0) throw new DomainRuleException("Credential decision actor is required.");
        VerificationStatus = status;
        VerifiedByActorId = actorId;
        VerifiedUtc = now;
        DecisionReason = reason;
        Version++;
        StampModified(now);
    }

    private static string Required(string value, int max, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 2 || normalized.Length > max)
            throw new DomainRuleException($"{label} must contain 2 to {max} characters.");
        return normalized;
    }
}
