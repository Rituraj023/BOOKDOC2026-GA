using System.Security.Cryptography;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Domain.Clinical;

public sealed class ClinicalEncounter : TenantScopedEntity
{
    private ClinicalEncounter() { }

    public long BranchId { get; private set; }
    public long BookingId { get; private set; }
    public long PatientId { get; private set; }
    public long ServiceId { get; private set; }
    public string EncounterNumber { get; private set; } = string.Empty;
    public EncounterStatus Status { get; private set; }
    public int LatestRevisionNumber { get; private set; }
    public long LatestRevisionId { get; private set; }
    public DateTimeOffset? SignedUtc { get; private set; }
    public long? SignedByActorId { get; private set; }
    public long? SupervisingPractitionerId { get; private set; }
    public long Version { get; private set; } = 1;

    public static (ClinicalEncounter Encounter, EncounterRevision Revision) Start(
        Booking booking, EncounterContent content, long actorId, DateTimeOffset now) =>
        Start(booking, content, actorId, null, now);

    public static (ClinicalEncounter Encounter, EncounterRevision Revision) Start(
        Booking booking, EncounterContent content, long actorId, long? supervisingPractitionerId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(content);
        if (booking.Status != BookingStatus.Confirmed)
            throw new DomainRuleException("Only a confirmed booking can start a clinical encounter.");
        if (actorId <= 0) throw new DomainRuleException("An encounter author is required.");
        var encounter = new ClinicalEncounter
        {
            TenantId = booking.TenantId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            PatientId = booking.PatientId,
            ServiceId = booking.ServiceId,
            EncounterNumber = CreateNumber(booking.Id),
            Status = EncounterStatus.Draft,
            SupervisingPractitionerId = supervisingPractitionerId
        };
        encounter.StampCreated(now);
        var revision = EncounterRevision.Create(encounter, 1, EncounterRevisionKind.Draft, null,
            content, actorId, null, now);
        encounter.LatestRevisionNumber = 1;
        encounter.LatestRevisionId = revision.Id;
        return (encounter, revision);
    }

    public void AssignSupervisingPractitioner(long? supervisingPractitionerId, DateTimeOffset now)
    {
        if (Status != EncounterStatus.Draft)
            throw new DomainRuleException("Only a draft encounter can update the supervising practitioner.");
        SupervisingPractitionerId = supervisingPractitionerId;
        StampModified(now);
    }

    public EncounterRevision ReviseDraft(EncounterRevision latest, EncounterContent content,
        long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureLatest(latest);
        if (Status != EncounterStatus.Draft || latest.Kind != EncounterRevisionKind.Draft)
            throw new DomainRuleException("Only a draft encounter can receive a draft revision.");
        var revision = EncounterRevision.Create(this, LatestRevisionNumber + 1, EncounterRevisionKind.Draft,
            latest.Id, content, actorId, null, now);
        Advance(revision, now);
        return revision;
    }

    public EncounterRevision Sign(EncounterRevision latest, long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureLatest(latest);
        if (Status != EncounterStatus.Draft || latest.Kind != EncounterRevisionKind.Draft)
            throw new DomainRuleException("Only the latest draft encounter revision can be signed.");
        latest.Content.ValidateForSigning();
        var revision = EncounterRevision.Create(this, LatestRevisionNumber + 1, EncounterRevisionKind.Signed,
            latest.Id, latest.Content, actorId, null, now);
        Status = EncounterStatus.Signed;
        SignedUtc = now;
        SignedByActorId = actorId;
        Advance(revision, now);
        return revision;
    }

    public EncounterRevision Amend(EncounterRevision latest, EncounterContent content, string reason,
        long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureLatest(latest);
        if (Status != EncounterStatus.Signed
            || latest.Kind is not (EncounterRevisionKind.Signed or EncounterRevisionKind.Amendment))
            throw new DomainRuleException("Only a signed encounter can receive an amendment.");
        content.ValidateForSigning();
        var normalizedReason = NormalizeReason(reason);
        var revision = EncounterRevision.Create(this, LatestRevisionNumber + 1, EncounterRevisionKind.Amendment,
            latest.Id, content, actorId, normalizedReason, now);
        SignedUtc = now;
        SignedByActorId = actorId;
        Advance(revision, now);
        return revision;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The encounter changed after it was loaded.");
    }

    private void EnsureLatest(EncounterRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        if (revision.EncounterId != Id || revision.Id != LatestRevisionId
            || revision.RevisionNumber != LatestRevisionNumber || revision.TenantId != TenantId)
            throw new DomainRuleException("The supplied revision is not the current encounter revision.");
    }

    private void Advance(EncounterRevision revision, DateTimeOffset now)
    {
        LatestRevisionNumber = revision.RevisionNumber;
        LatestRevisionId = revision.Id;
        Version++;
        StampModified(now);
    }

    private static string NormalizeReason(string reason)
    {
        var normalized = reason?.Trim() ?? string.Empty;
        if (normalized.Length is < 5 or > 500)
            throw new DomainRuleException("An amendment reason between 5 and 500 characters is required.");
        return normalized;
    }

    private static string CreateNumber(long bookingId)
    {
        var digest = SHA256.HashData(BitConverter.GetBytes(bookingId));
        return $"ENC-{Convert.ToHexString(digest)[..20]}";
    }
}
