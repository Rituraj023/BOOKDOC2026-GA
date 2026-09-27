using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;

namespace BookDoc2026.Application.Abstractions;

public sealed record PatientAggregate(Patient Patient, StakeholderAggregate Stakeholder);

public sealed record PatientRelationDetail(
    PatientRelation Relation,
    Patient RelatedPatient,
    StakeholderAggregate RelatedStakeholder);

public interface IPatientRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);

    Task<PatientAggregate?> GetByRegistrationRequestAsync(
        Guid registrationRequestId,
        CancellationToken cancellationToken);

    Task<PatientAggregate?> GetAsync(long patientId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PatientAggregate>> SearchAsync(
        string normalizedQuery,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<long>> FindPotentialDuplicatesAsync(
        string normalizedName,
        DateOnly? dateOfBirth,
        IReadOnlyCollection<string> normalizedContacts,
        IReadOnlyCollection<(string Type, string Issuer, string Value)> normalizedIdentifiers,
        CancellationToken cancellationToken);

    Task AddAsync(PatientAggregate aggregate, CancellationToken cancellationToken);

    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PatientRelationDetail>> GetRelationsAsync(long patientId, CancellationToken cancellationToken);

    Task<PatientRelation?> GetRelationByIdAsync(long relationId, CancellationToken cancellationToken);

    Task<bool> RelationExistsAsync(long patientId, long relatedPatientId, CancellationToken cancellationToken);

    Task AddRelationAsync(PatientRelation relation, CancellationToken cancellationToken);

    Task RemoveRelationAsync(PatientRelation relation, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
