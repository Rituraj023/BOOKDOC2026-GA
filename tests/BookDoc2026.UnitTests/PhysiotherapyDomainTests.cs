using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.UnitTests;

public sealed class PhysiotherapyDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CarePlan_IsEncounterAnchoredVersionedAndRequiresReasonWhenActive()
    {
        var (encounter, signed) = SignedEncounter();
        var (plan, first) = PhysiotherapyCarePlan.Start(encounter, signed, PlanContent("Initial goals"), 10, Now);
        plan.Activate(1, 10, Now.AddMinutes(1));

        Assert.Throws<DomainRuleException>(() => plan.Revise(first, PlanContent("Changed goals"), null,
            2, 10, Now.AddMinutes(2)));
        var second = plan.Revise(first, PlanContent("Changed goals"), "Progress review changed goals",
            2, 10, Now.AddMinutes(3));

        Assert.Equal(PhysiotherapyCarePlanStatus.Active, plan.Status);
        Assert.Equal(2, plan.LatestRevisionNumber);
        Assert.Equal(first.Id, second.ParentRevisionId);
        Assert.NotEqual(first.ContentHash, second.ContentHash);
        Assert.Equal("Progress review changed goals", second.ChangeReason);
    }

    [Fact]
    public void CarePlan_RejectsUnsignedOrNonPhysiotherapyEncounterAndStaleVersion()
    {
        var booking = BookingFor();
        var (draftEncounter, draftRevision) = ClinicalEncounter.Start(booking, EncounterContentFor("PHYSIOTHERAPY"),
            10, Now);
        Assert.Throws<DomainRuleException>(() => PhysiotherapyCarePlan.Start(draftEncounter, draftRevision,
            PlanContent("Goals"), 10, Now));

        var (otherEncounter, otherDraft) = ClinicalEncounter.Start(BookingFor(), EncounterContentFor("ORTHOPAEDICS"),
            10, Now);
        var otherSigned = otherEncounter.Sign(otherDraft, 1, 10, Now);
        Assert.Throws<DomainRuleException>(() => PhysiotherapyCarePlan.Start(otherEncounter, otherSigned,
            PlanContent("Goals"), 10, Now));

        var (encounter, signed) = SignedEncounter();
        var (plan, _) = PhysiotherapyCarePlan.Start(encounter, signed, PlanContent("Goals"), 10, Now);
        Assert.Throws<ConcurrencyConflictException>(() => plan.Activate(2, 10, Now));
    }

    [Fact]
    public void TreatmentSession_RequiresActiveMatchingPlanAndAdverseEventEvidence()
    {
        var (encounter, signed) = SignedEncounter();
        var (plan, _) = PhysiotherapyCarePlan.Start(encounter, signed, PlanContent("Goals"), 10, Now);
        var content = PhysiotherapySessionContent.Create("Improving", "Exercise progression", "Tolerated",
            "Continue", false, null);
        Assert.Throws<DomainRuleException>(() => PhysiotherapyTreatmentSession.Record(
            plan, encounter, signed, 1, content, 10, Now));
        Assert.Throws<DomainRuleException>(() => PhysiotherapySessionContent.Create(
            "Pain", "Manual therapy", "Stopped", "Review", true, null));

        plan.Activate(1, 10, Now);
        var session = PhysiotherapyTreatmentSession.Record(plan, encounter, signed, 1, content, 10, Now);
        Assert.Equal(1, session.SequenceNumber);
        Assert.Equal(64, session.ContentHash.Length);
        Assert.False(session.HadAdverseEvent);
    }

    [Fact]
    public void Outcome_PreservesToolVersionContextUnitAndOptionalSession()
    {
        var (encounter, signed) = SignedEncounter();
        var (plan, _) = PhysiotherapyCarePlan.Start(encounter, signed, PlanContent("Goals"), 10, Now);
        plan.Activate(1, 10, Now);
        var session = PhysiotherapyTreatmentSession.Record(plan, encounter, signed, 1,
            PhysiotherapySessionContent.Create("Stable", "Exercise", "Good", "Progress", false, null), 10, Now);

        var outcome = PhysiotherapyOutcomeObservation.Record(plan, session, "Reassessment", "NPRS",
            "1.0", 4.12555m, "score", "Knee", "Right", Now, 10, Now);

        Assert.Equal("REASSESSMENT", outcome.ContextCode);
        Assert.Equal("NPRS", outcome.MeasureCode);
        Assert.Equal("1.0", outcome.ToolVersion);
        Assert.Equal(4.1256m, outcome.Value);
        Assert.Equal(session.Id, outcome.TreatmentSessionId);
        Assert.Throws<DomainRuleException>(() => PhysiotherapyOutcomeObservation.Record(plan, session,
            "Review", "NPRS", "1.0", 5, "score", null, null, Now.AddHours(1), 10, Now));
    }

    [Fact]
    public void ClosingPlan_IsReasonedVersionedAndBlocksFurtherClinicalRecords()
    {
        var (encounter, signed) = SignedEncounter();
        var (plan, _) = PhysiotherapyCarePlan.Start(encounter, signed, PlanContent("Goals"), 10, Now);
        plan.Activate(1, 10, Now);
        plan.Close(false, "Goals achieved and discharged", 2, 10, Now.AddDays(1));

        Assert.Equal(PhysiotherapyCarePlanStatus.Completed, plan.Status);
        Assert.Equal("Goals achieved and discharged", plan.ClosureReason);
        Assert.Throws<DomainRuleException>(() => PhysiotherapyTreatmentSession.Record(plan, encounter, signed, 1,
            PhysiotherapySessionContent.Create("Stable", "Exercise", "Good", "Discharge", false, null),
            10, Now.AddDays(1)));
        Assert.Throws<DomainRuleException>(() => PhysiotherapyOutcomeObservation.Record(plan, null,
            "Discharge", "NPRS", "1", 0, "score", null, null, Now, 10, Now.AddDays(1)));
    }

    private static PhysiotherapyCarePlanContent PlanContent(string goals) =>
        PhysiotherapyCarePlanContent.Create(goals, "Twice weekly for four weeks",
            "Graded exercise and education", "Stop if symptoms worsen", new DateOnly(2026, 9, 15));

    private static (ClinicalEncounter Encounter, EncounterRevision Signed) SignedEncounter()
    {
        var (encounter, draft) = ClinicalEncounter.Start(BookingFor(), EncounterContentFor("PHYSIOTHERAPY"),
            10, Now);
        return (encounter, encounter.Sign(draft, 1, 10, Now));
    }

    private static EncounterContent EncounterContentFor(string specialty) => EncounterContent.Create(
        specialty, "INITIAL-ASSESSMENT", "v1", "Knee pain", "Two weeks", "Reduced flexion",
        "Mechanical presentation", "Physiotherapy care plan", "Safety advice", "Knee", "Right");

    private static Booking BookingFor()
    {
        var hold = SchedulingHold.Create(1, 2, 3, 4, Guid.NewGuid(), new string('A', 64),
            Now.AddDays(1), Now.AddDays(1).AddMinutes(30), Now.AddMinutes(10), Now);
        return Booking.Confirm(hold, Now);
    }
}
