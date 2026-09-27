using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Queues;

namespace BookDoc2026.Domain.Radiology;

public sealed class RadiologyStudy : TenantScopedEntity
{
    private RadiologyStudy() { }

    public long BranchId { get; private set; }
    public long OrderId { get; private set; }
    public long PatientId { get; private set; }
    public long ServiceId { get; private set; }
    public ImagingModality Modality { get; private set; }
    public Guid RegistrationRequestId { get; private set; }
    public RadiologyStudyStatus Status { get; private set; }
    public long RegisteredByActorId { get; private set; }
    public DateTimeOffset RegisteredUtc { get; private set; }
    public long? LastStartedByActorId { get; private set; }
    public DateTimeOffset? LastStartedUtc { get; private set; }
    public int AcquisitionAttemptCount { get; private set; }
    public long Version { get; private set; } = 1;

    public static RadiologyStudy Register(
        InvestigationOrder order,
        Guid requestId,
        long actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (requestId == Guid.Empty || actorId <= 0)
            throw new DomainRuleException("Radiology study request identity and actor are required.");
        if (order.QueueHandoffUtc is null)
            throw new DomainRuleException("A queued investigation order is required before registering a radiology study.");

        var study = new RadiologyStudy
        {
            TenantId = order.TenantId,
            BranchId = order.BranchId,
            OrderId = order.Id,
            PatientId = order.PatientId,
            ServiceId = order.RequestedServiceId,
            Modality = order.Modality,
            RegistrationRequestId = requestId,
            Status = RadiologyStudyStatus.Registered,
            RegisteredByActorId = actorId,
            RegisteredUtc = now
        };
        study.StampCreated(now);
        return study;
    }

    public void Start(long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (actorId <= 0) throw new DomainRuleException("A radiology operator is required.");
        if (Status is not RadiologyStudyStatus.Registered and not RadiologyStudyStatus.RepeatRequired)
            throw new DomainRuleException("Only a registered or repeat-required study can start acquisition.");

        Status = RadiologyStudyStatus.InProgress;
        LastStartedByActorId = actorId;
        LastStartedUtc = now;
        Version++;
        StampModified(now);
    }

    public RadiologyAcquisitionAttempt RecordAcquisition(
        long expectedVersion,
        Guid requestId,
        long equipmentResourceId,
        string protocolCode,
        string protocolVersion,
        bool hasProtocolDeviation,
        string? deviationCode,
        string? deviationNote,
        RadiologyAcquisitionOutcome outcome,
        string? outcomeReasonCode,
        string? outcomeNote,
        string? externalStudyReference,
        long actorId,
        DateTimeOffset startedUtc,
        DateTimeOffset completedUtc,
        DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != RadiologyStudyStatus.InProgress)
            throw new DomainRuleException("The radiology study must be in progress before recording acquisition.");

        var attempt = RadiologyAcquisitionAttempt.Record(this, AcquisitionAttemptCount + 1, requestId,
            equipmentResourceId, protocolCode, protocolVersion, hasProtocolDeviation, deviationCode,
            deviationNote, outcome, outcomeReasonCode, outcomeNote, externalStudyReference, actorId,
            startedUtc, completedUtc, now);
        AcquisitionAttemptCount++;
        Status = outcome == RadiologyAcquisitionOutcome.Acquired
            ? RadiologyStudyStatus.Acquired
            : RadiologyStudyStatus.Aborted;
        Version++;
        StampModified(now);
        return attempt;
    }

    public RadiologyQualityReview ReviewQuality(
        long expectedVersion,
        RadiologyAcquisitionAttempt attempt,
        Guid requestId,
        RadiologyQualityDecision decision,
        string reasonCode,
        string? note,
        long reviewerActorId,
        DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        ArgumentNullException.ThrowIfNull(attempt);
        if (Status != RadiologyStudyStatus.Acquired)
            throw new DomainRuleException("Only an acquired study can receive a technical-quality review.");
        if (attempt.StudyId != Id || attempt.Sequence != AcquisitionAttemptCount
            || attempt.Outcome != RadiologyAcquisitionOutcome.Acquired)
            throw new DomainRuleException("Technical-quality review must target the latest acquired attempt.");
        if (attempt.PerformedByActorId == reviewerActorId)
            throw new DomainRuleException("The performing operator cannot quality-review the same acquisition.");

        var review = RadiologyQualityReview.Record(this, attempt, requestId, decision, reasonCode, note,
            reviewerActorId, now);
        Status = decision == RadiologyQualityDecision.Accepted
            ? RadiologyStudyStatus.QualityAccepted
            : RadiologyStudyStatus.RepeatRequired;
        Version++;
        StampModified(now);
        return review;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The radiology study changed after it was loaded.");
    }
}
