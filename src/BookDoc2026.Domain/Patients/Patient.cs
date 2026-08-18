using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Patients;

public sealed class Patient : TenantScopedEntity
{
    private Patient()
    {
    }

    public long RegistrationBranchId { get; private set; }

    public Guid RegistrationRequestId { get; private set; }

    public string RegistrationPayloadHash { get; private set; } = string.Empty;

    public long StakeholderId { get; private set; }

    public string PatientNumber { get; private set; } = string.Empty;

    public PatientStatus Status { get; private set; }

    public string? BloodGroup { get; private set; }

    public long Version { get; private set; } = 1;

    public static Patient Register(
        long tenantId,
        long registrationBranchId,
        Guid registrationRequestId,
        string registrationPayloadHash,
        long stakeholderId,
        string? bloodGroup,
        DateTimeOffset now)
    {
        if (tenantId == 0 || registrationBranchId == 0 || registrationRequestId == Guid.Empty
            || string.IsNullOrWhiteSpace(registrationPayloadHash) || stakeholderId == 0)
        {
            throw new DomainRuleException("Tenant, branch and registration request identifiers are required.");
        }

        _ = NormalizeBloodGroup(bloodGroup);
        var patient = new Patient
        {
            TenantId = tenantId,
            RegistrationBranchId = registrationBranchId,
            RegistrationRequestId = registrationRequestId,
            RegistrationPayloadHash = registrationPayloadHash,
            StakeholderId = stakeholderId,
            Status = PatientStatus.Active,
            BloodGroup = NormalizeBloodGroup(bloodGroup)
        };
        patient.PatientNumber = $"PAT-{patient.Id:N}"[..24].ToUpperInvariant();
        patient.StampCreated(now);
        return patient;
    }

    public void UpdateClinicalProfile(
        long expectedVersion,
        string? bloodGroup,
        DateTimeOffset now)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException("The patient changed after it was loaded.");
        }

        if (Status is PatientStatus.Merged or PatientStatus.Deceased)
        {
            throw new DomainRuleException("Merged or deceased patient demographics cannot be edited through this operation.");
        }

        BloodGroup = NormalizeBloodGroup(bloodGroup);
        Version++;
        StampModified(now);
    }

    private static string? NormalizeBloodGroup(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        string[] allowed = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];
        if (normalized is not null && !allowed.Contains(normalized, StringComparer.Ordinal))
        {
            throw new DomainRuleException("Blood group is not recognized.");
        }

        return normalized;
    }
}
