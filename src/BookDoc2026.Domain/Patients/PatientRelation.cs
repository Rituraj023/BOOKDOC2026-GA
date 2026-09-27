using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Patients;

public sealed class PatientRelation : TenantScopedEntity
{
    private PatientRelation() { }

    public long PatientId { get; private set; }
    public long RelatedPatientId { get; private set; }
    public PatientRelationshipType RelationshipType { get; private set; }
    public bool IsEmergencyContact { get; private set; }
    public bool IsGuardian { get; private set; }
    public string? Notes { get; private set; }
    public long Version { get; private set; } = 1;

    public static PatientRelation Create(
        long tenantId,
        long patientId,
        long relatedPatientId,
        PatientRelationshipType relationshipType,
        bool isEmergencyContact,
        bool isGuardian,
        string? notes,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || patientId <= 0 || relatedPatientId <= 0)
            throw new DomainRuleException("Tenant and patient identifiers are required.");
        if (patientId == relatedPatientId)
            throw new DomainRuleException("A patient cannot be related to themselves.");

        var relation = new PatientRelation
        {
            TenantId = tenantId,
            PatientId = patientId,
            RelatedPatientId = relatedPatientId,
            RelationshipType = relationshipType,
            IsEmergencyContact = isEmergencyContact,
            IsGuardian = isGuardian,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
        relation.StampCreated(now);
        return relation;
    }

    public void Update(
        long expectedVersion,
        PatientRelationshipType relationshipType,
        bool isEmergencyContact,
        bool isGuardian,
        string? notes,
        DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The patient relationship was modified by another user.");

        RelationshipType = relationshipType;
        IsEmergencyContact = isEmergencyContact;
        IsGuardian = isGuardian;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Version++;
        StampModified(now);
    }
}
