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

public interface IEncounterRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<Booking?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<bool> EncounterExistsForBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<EncounterAggregate?> GetEncounterAsync(long branchId, long encounterId, bool tracked,
        CancellationToken cancellationToken);
    Task AddEncounterAsync(ClinicalEncounter encounter, EncounterRevision revision, AuditEvent auditEvent,
        CancellationToken cancellationToken);
    Task AddRevisionAsync(EncounterRevision revision, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
