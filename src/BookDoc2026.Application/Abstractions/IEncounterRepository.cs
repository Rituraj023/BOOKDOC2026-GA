using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Abstractions;

public sealed record EncounterAggregate(
    ClinicalEncounter Encounter,
    IReadOnlyCollection<EncounterRevision> Revisions)
{
    public EncounterRevision Latest => Revisions.Single(item => item.Id == Encounter.LatestRevisionId);
}

public sealed record ClinicalAgendaWorkItem(
    Booking Booking,
    string PatientNumber,
    string PatientDisplayName,
    string ServiceCode,
    string ServiceName,
    ClinicalEncounter? Encounter,
    PhysiotherapyCarePlan? CarePlan);

public interface IEncounterRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<Booking?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<bool> EncounterExistsForBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<EncounterAggregate?> GetEncounterAsync(long branchId, long encounterId, bool tracked,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClinicalAgendaWorkItem>> ListAgendaAsync(long branchId, long actorId,
        DateOnly localDate, DateTimeOffset startUtc, DateTimeOffset endUtc, int take,
        CancellationToken cancellationToken);
    Task AddEncounterAsync(ClinicalEncounter encounter, EncounterRevision revision, AuditEvent auditEvent,
        CancellationToken cancellationToken);
    Task AddRevisionAsync(EncounterRevision revision, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task AddVitalSignsAsync(PatientVitalSigns vitals, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<PatientVitalSigns?> GetLatestVitalSignsAsync(long branchId, long patientId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PatientVitalSigns>> ListVitalSignsAsync(long branchId, long patientId, int take, CancellationToken cancellationToken);
    Task<PatientVitalSigns?> GetVitalSignsByBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<PatientVitalSigns?> GetVitalSignsByEncounterAsync(long branchId, long encounterId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
