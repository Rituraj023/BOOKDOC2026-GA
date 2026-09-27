using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Radiology;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.UnitTests;

public sealed class RadiologyStudyDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AcquiredStudy_RequiresIndependentReviewerBeforeQualityAcceptance()
    {
        var study = RegisteredStudy();
        study.Start(1, 5101, Now.AddMinutes(1));
        var attempt = study.RecordAcquisition(2, Guid.NewGuid(), NumericId.Next(), "XR-KNEE-AP",
            "V1", false, null, null, RadiologyAcquisitionOutcome.Acquired, null, null,
            "1.2.840.10008.20260828.1", 5101, Now.AddMinutes(1), Now.AddMinutes(3),
            Now.AddMinutes(3));

        Assert.Equal(RadiologyStudyStatus.Acquired, study.Status);
        Assert.Equal(1, study.AcquisitionAttemptCount);
        Assert.Equal(1, attempt.Sequence);
        Assert.True(attempt.ExternalStudyReference is not null);
        Assert.Throws<DomainRuleException>(() => study.ReviewQuality(3, attempt, Guid.NewGuid(),
            RadiologyQualityDecision.Accepted, "ACCEPTED", null, 5101, Now.AddMinutes(4)));

        var review = study.ReviewQuality(3, attempt, Guid.NewGuid(), RadiologyQualityDecision.Accepted,
            "ACCEPTED", "Position and exposure accepted", 5102, Now.AddMinutes(4));

        Assert.Equal(RadiologyStudyStatus.QualityAccepted, study.Status);
        Assert.Equal(4, study.Version);
        Assert.Equal(5102, review.ReviewedByActorId);
        Assert.Equal(attempt.Id, review.AcquisitionAttemptId);
    }

    [Fact]
    public void RepeatRequired_PreservesFirstAttemptAndAllowsASeparateSecondAttempt()
    {
        var study = RegisteredStudy();
        study.Start(1, 5101, Now.AddMinutes(1));
        var first = study.RecordAcquisition(2, Guid.NewGuid(), NumericId.Next(), "CT-HEAD",
            "V2", true, "MOTION", "Patient movement recorded",
            RadiologyAcquisitionOutcome.Acquired, null, null, null, 5101,
            Now.AddMinutes(1), Now.AddMinutes(4), Now.AddMinutes(4));
        _ = study.ReviewQuality(3, first, Guid.NewGuid(), RadiologyQualityDecision.RepeatRequired,
            "MOTION-ARTEFACT", "Repeat requested", 5102, Now.AddMinutes(5));

        Assert.Equal(RadiologyStudyStatus.RepeatRequired, study.Status);
        study.Start(4, 5101, Now.AddMinutes(6));
        var second = study.RecordAcquisition(5, Guid.NewGuid(), NumericId.Next(), "CT-HEAD",
            "V2", false, null, null, RadiologyAcquisitionOutcome.Acquired, null, null, null, 5101,
            Now.AddMinutes(6), Now.AddMinutes(9), Now.AddMinutes(9));
        _ = study.ReviewQuality(6, second, Guid.NewGuid(), RadiologyQualityDecision.Accepted,
            "ACCEPTED", null, 5102, Now.AddMinutes(10));

        Assert.Equal(1, first.Sequence);
        Assert.Equal(2, second.Sequence);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, study.AcquisitionAttemptCount);
        Assert.Equal(RadiologyStudyStatus.QualityAccepted, study.Status);
        Assert.Equal(7, study.Version);
    }

    [Fact]
    public void Acquisition_RejectsStaleVersionAndUncodedDeviationOrAbort()
    {
        var study = RegisteredStudy();
        study.Start(1, 5101, Now.AddMinutes(1));

        Assert.Throws<ConcurrencyConflictException>(() => study.RecordAcquisition(1, Guid.NewGuid(),
            NumericId.Next(), "XR-CHEST", "V1", false, null, null,
            RadiologyAcquisitionOutcome.Acquired, null, null, null, 5101,
            Now.AddMinutes(1), Now.AddMinutes(2), Now.AddMinutes(2)));
        Assert.Throws<DomainRuleException>(() => study.RecordAcquisition(2, Guid.NewGuid(), NumericId.Next(),
            "XR-CHEST", "V1", true, null, "Uncoded deviation",
            RadiologyAcquisitionOutcome.Acquired, null, null, null, 5101,
            Now.AddMinutes(1), Now.AddMinutes(2), Now.AddMinutes(2)));
        Assert.Throws<DomainRuleException>(() => study.RecordAcquisition(2, Guid.NewGuid(), NumericId.Next(),
            "XR-CHEST", "V1", false, null, null, RadiologyAcquisitionOutcome.Aborted,
            null, "No code", null, 5101, Now.AddMinutes(1), Now.AddMinutes(2), Now.AddMinutes(2)));
    }

    private static RadiologyStudy RegisteredStudy()
    {
        var order = QueuedOrder();
        return RadiologyStudy.Register(order, Guid.NewGuid(), 5101, Now);
    }

    private static InvestigationOrder QueuedOrder()
    {
        var order = InvestigationOrder.Request(SignedEncounter(), NumericId.Next(), ImagingModality.XRay,
            "Persistent symptoms requiring imaging", Guid.NewGuid(), 4101, Now.AddMinutes(-2));
        order.RecordQueueHandoff(1, 4102, Now.AddMinutes(-1));
        return order;
    }

    private static ClinicalEncounter SignedEncounter()
    {
        var hold = SchedulingHold.Create(NumericId.Next(), NumericId.Next(), NumericId.Next(), NumericId.Next(),
            Guid.NewGuid(), new string('A', 64), Now.AddDays(1), Now.AddDays(1).AddMinutes(30),
            Now.AddMinutes(10), Now.AddMinutes(-5));
        var booking = Booking.Confirm(hold, Now.AddMinutes(-5));
        var content = EncounterContent.Create("ORTHOPAEDICS", "INITIAL-ASSESSMENT", "v1",
            "Knee pain", "Persistent symptoms", "Joint-line tenderness", "Mechanical knee pain",
            "Review imaging", "Return if symptoms worsen", "Knee", "Right");
        var (encounter, draft) = ClinicalEncounter.Start(booking, content, 4101, Now.AddMinutes(-4));
        _ = encounter.Sign(draft, 1, 4101, Now.AddMinutes(-3));
        return encounter;
    }
}
