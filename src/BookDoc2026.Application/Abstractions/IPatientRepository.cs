using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;

namespace BookDoc2026.Application.Abstractions;

public sealed record PatientAggregate(Patient Patient, StakeholderAggregate Stakeholder);

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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
