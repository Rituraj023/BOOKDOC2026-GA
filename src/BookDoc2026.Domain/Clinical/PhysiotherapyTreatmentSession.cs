using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class PhysiotherapyTreatmentSession : TenantScopedEntity
{
    private PhysiotherapyTreatmentSession() { }

    public long BranchId { get; private set; }
    public long CarePlanId { get; private set; }
    public long EncounterId { get; private set; }
    public long BookingId { get; private set; }
    public int SequenceNumber { get; private set; }
    public long AuthorActorId { get; private set; }
    public string SubjectiveResponse { get; private set; } = string.Empty;
    public string Interventions { get; private set; } = string.Empty;
    public string Tolerance { get; private set; } = string.Empty;
    public string NextPlan { get; private set; } = string.Empty;
    public bool HadAdverseEvent { get; private set; }
    public string? AdverseEventDetails { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public DateTimeOffset PerformedUtc { get; private set; }

    public static PhysiotherapyTreatmentSession Record(PhysiotherapyCarePlan plan, ClinicalEncounter encounter,
        EncounterRevision latestEncounterRevision, int sequenceNumber, PhysiotherapySessionContent content,
        long actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(latestEncounterRevision); ArgumentNullException.ThrowIfNull(content);
        if (plan.Status != PhysiotherapyCarePlanStatus.Active)
            throw new DomainRuleException("Treatment sessions require an active care plan.");
        if (encounter.Status != EncounterStatus.Signed || latestEncounterRevision.Id != encounter.LatestRevisionId
            || !string.Equals(latestEncounterRevision.SpecialtyCode, "PHYSIOTHERAPY", StringComparison.Ordinal))
            throw new DomainRuleException("A signed Physiotherapy encounter is required for a treatment session.");
        if (plan.TenantId != encounter.TenantId || plan.BranchId != encounter.BranchId
            || plan.PatientId != encounter.PatientId || plan.ServiceId != encounter.ServiceId || sequenceNumber <= 0
            || actorId <= 0)
            throw new DomainRuleException("Treatment session scope, patient, service, sequence or author is invalid.");
        var session = new PhysiotherapyTreatmentSession
        {
            TenantId = plan.TenantId, BranchId = plan.BranchId, CarePlanId = plan.Id,
            EncounterId = encounter.Id, BookingId = encounter.BookingId, SequenceNumber = sequenceNumber,
            AuthorActorId = actorId, SubjectiveResponse = content.SubjectiveResponse,
            Interventions = content.Interventions, Tolerance = content.Tolerance, NextPlan = content.NextPlan,
            HadAdverseEvent = content.HadAdverseEvent, AdverseEventDetails = content.AdverseEventDetails,
            ContentHash = content.Hash(), PerformedUtc = encounter.SignedUtc ?? now
        };
        session.StampCreated(now);
        return session;
    }
}
