using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.UnitTests;

public sealed class EncounterDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DraftHistory_SignsAsNewImmutableRevision()
    {
        var (encounter, first) = ClinicalEncounter.Start(CreateBooking(), Content("Initial plan"), 1001, Now);
        var second = encounter.ReviseDraft(first, Content("Reviewed plan"), 1, 1002, Now.AddMinutes(1));
        var signed = encounter.Sign(second, 2, 1003, Now.AddMinutes(2));

        Assert.Equal(EncounterStatus.Signed, encounter.Status);
        Assert.Equal(3, encounter.LatestRevisionNumber);
        Assert.Equal(EncounterRevisionKind.Draft, first.Kind);
        Assert.Equal(EncounterRevisionKind.Draft, second.Kind);
        Assert.Equal(EncounterRevisionKind.Signed, signed.Kind);
        Assert.Equal(second.Id, signed.ParentRevisionId);
        Assert.Equal(second.ContentHash, signed.ContentHash);
        Assert.Equal(1003, encounter.SignedByActorId);
        Assert.Throws<DomainRuleException>(() =>
            encounter.ReviseDraft(signed, Content("Unsafe overwrite"), 3, 1003, Now.AddMinutes(3)));
    }

    [Fact]
    public void Signing_RequiresAssessmentAndPlan()
    {
        var incomplete = EncounterContent.Create("PHYSIOTHERAPY", "INITIAL-ASSESSMENT", "v1",
            "Knee pain", null, null, null, null, null, "Knee", "Right");
        var (encounter, draft) = ClinicalEncounter.Start(CreateBooking(), incomplete, 1001, Now);

        Assert.Throws<DomainRuleException>(() => encounter.Sign(draft, 1, 1001, Now.AddMinutes(1)));
        Assert.Equal(EncounterStatus.Draft, encounter.Status);
        Assert.Equal(1, encounter.LatestRevisionNumber);
    }

    [Fact]
    public void Amendment_AppendsReasonedSignedContentAndPreservesOriginalHash()
    {
        var (encounter, draft) = ClinicalEncounter.Start(CreateBooking(), Content("Initial plan"), 1001, Now);
        var signed = encounter.Sign(draft, 1, 1001, Now.AddMinutes(1));
        var originalHash = signed.ContentHash;

        Assert.Throws<DomainRuleException>(() => encounter.Amend(
            signed, Content("Corrected plan"), " ", 2, 1002, Now.AddMinutes(2)));

        var amendment = encounter.Amend(signed, Content("Corrected plan"),
            "Corrected the documented follow-up interval", 2, 1002, Now.AddMinutes(3));

        Assert.Equal(EncounterRevisionKind.Amendment, amendment.Kind);
        Assert.Equal(signed.Id, amendment.ParentRevisionId);
        Assert.NotEqual(originalHash, amendment.ContentHash);
        Assert.Equal(originalHash, signed.ContentHash);
        Assert.Equal("Corrected the documented follow-up interval", amendment.AmendmentReason);
    }

    [Fact]
    public void Revision_RejectsStaleEncounterVersion()
    {
        var (encounter, draft) = ClinicalEncounter.Start(CreateBooking(), Content("Plan"), 1001, Now);

        Assert.Throws<ConcurrencyConflictException>(() => encounter.ReviseDraft(
            draft, Content("Changed"), 2, 1001, Now.AddMinutes(1)));
        Assert.Equal(1, encounter.LatestRevisionNumber);
    }

    private static EncounterContent Content(string plan) => EncounterContent.Create(
        "PHYSIOTHERAPY", "INITIAL-ASSESSMENT", "v1", "Right knee pain",
        "Pain for two weeks", "Reduced flexion", "Mechanical knee pain", plan,
        "Return if symptoms worsen", "Knee", "Right");

    private static Booking CreateBooking()
    {
        var hold = SchedulingHold.Create(NumericId.Next(), NumericId.Next(), NumericId.Next(), NumericId.Next(),
            Guid.NewGuid(), new string('A', 64), Now.AddDays(1), Now.AddDays(1).AddMinutes(30),
            Now.AddMinutes(10), Now);
        return Booking.Confirm(hold, Now);
    }
}
