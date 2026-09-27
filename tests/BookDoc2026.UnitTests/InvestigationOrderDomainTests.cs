using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.UnitTests;

public sealed class InvestigationOrderDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SignedEncounter_CreatesPolicyNeutralOrderAndIndependentQueueHandoff()
    {
        var encounter = SignedEncounter();
        var order = InvestigationOrder.Request(encounter, NumericId.Next(), ImagingModality.XRay,
            "Persistent right-knee pain after assessment", Guid.NewGuid(), 4101, Now);

        Assert.Equal(InvestigationOrderStatus.Requested, order.Status);
        Assert.Equal(InvestigationResultStatus.Pending, order.ResultStatus);
        Assert.Null(order.QueueHandoffUtc);

        order.RecordQueueHandoff(1, 4102, Now.AddMinutes(2));
        var ticket = QueueTicket.CheckInInvestigation(order.TenantId, order.BranchId, NumericId.Next(),
            order.PatientId, order.Id, Guid.NewGuid(), QueuePriority.Normal, Now.AddMinutes(2));

        Assert.Equal(2, order.Version);
        Assert.Equal(InvestigationOrderStatus.Requested, order.Status);
        Assert.Equal(InvestigationResultStatus.Pending, order.ResultStatus);
        Assert.Equal(order.Id, ticket.InvestigationOrderId);
        Assert.Equal(QueueTicketStatus.Waiting, ticket.Status);
        Assert.Null(ticket.BookingId);
    }

    [Fact]
    public void InvestigationRequest_RequiresSignedEncounterAndHandoffIsVersionedOnce()
    {
        var (draft, _) = ClinicalEncounter.Start(CreateBooking(), Content(), 4101, Now);
        Assert.Throws<DomainRuleException>(() => InvestigationOrder.Request(draft, NumericId.Next(),
            ImagingModality.CT, "CT requested for persistent symptoms", Guid.NewGuid(), 4101, Now));

        var order = InvestigationOrder.Request(SignedEncounter(), NumericId.Next(), ImagingModality.CT,
            "CT requested for persistent symptoms", Guid.NewGuid(), 4101, Now);
        Assert.Throws<ConcurrencyConflictException>(() =>
            order.RecordQueueHandoff(2, 4102, Now.AddMinutes(1)));
        order.RecordQueueHandoff(1, 4102, Now.AddMinutes(1));
        Assert.Throws<DomainRuleException>(() =>
            order.RecordQueueHandoff(2, 4102, Now.AddMinutes(2)));
    }

    [Fact]
    public void InvestigationHistory_RecordsQueueLinkWithoutChangingClinicalStates()
    {
        var order = InvestigationOrder.Request(SignedEncounter(), NumericId.Next(), ImagingModality.XRay,
            "Standing knee X-ray requested", Guid.NewGuid(), 4101, Now);
        var requested = InvestigationOrderEvent.Record(order, "Requested", 4101, null, Now);
        order.RecordQueueHandoff(1, 4102, Now.AddMinutes(1));
        var queueTicketId = NumericId.Next();
        var linked = InvestigationOrderEvent.Record(order, "QueueLinked", 4102, queueTicketId,
            Now.AddMinutes(1));

        Assert.Equal(1, requested.OrderVersion);
        Assert.Null(requested.QueueTicketId);
        Assert.Equal(2, linked.OrderVersion);
        Assert.Equal(queueTicketId, linked.QueueTicketId);
        Assert.Equal(InvestigationOrderStatus.Requested, order.Status);
        Assert.Equal(InvestigationResultStatus.Pending, order.ResultStatus);
    }

    [Fact]
    public void InvestigationRequest_UsesIdempotencyIdentityForStableUniqueOrderNumber()
    {
        var firstRequestId = Guid.NewGuid();
        var secondRequestId = Guid.NewGuid();
        var encounter = SignedEncounter();

        var first = InvestigationOrder.Request(encounter, NumericId.Next(), ImagingModality.XRay,
            "Standing knee X-ray requested", firstRequestId, 4101, Now);
        var replayEquivalent = InvestigationOrder.Request(encounter, NumericId.Next(), ImagingModality.XRay,
            "Standing knee X-ray requested", firstRequestId, 4101, Now);
        var second = InvestigationOrder.Request(encounter, NumericId.Next(), ImagingModality.XRay,
            "Standing knee X-ray requested", secondRequestId, 4101, Now);

        Assert.Equal(first.OrderNumber, replayEquivalent.OrderNumber);
        Assert.NotEqual(first.OrderNumber, second.OrderNumber);
        Assert.Matches("^INV-[0-9A-F]{16}$", first.OrderNumber);
    }

    private static ClinicalEncounter SignedEncounter()
    {
        var (encounter, draft) = ClinicalEncounter.Start(CreateBooking(), Content(), 4101, Now.AddMinutes(-2));
        _ = encounter.Sign(draft, 1, 4101, Now.AddMinutes(-1));
        return encounter;
    }

    private static EncounterContent Content() => EncounterContent.Create(
        "ORTHOPAEDICS", "INITIAL-ASSESSMENT", "v1", "Right knee pain", "Persistent symptoms",
        "Joint-line tenderness", "Mechanical knee pain", "Review imaging",
        "Return if symptoms worsen", "Knee", "Right");

    private static Booking CreateBooking()
    {
        var hold = SchedulingHold.Create(NumericId.Next(), NumericId.Next(), NumericId.Next(), NumericId.Next(),
            Guid.NewGuid(), new string('B', 64), Now.AddDays(1), Now.AddDays(1).AddMinutes(30),
            Now.AddMinutes(10), Now.AddMinutes(-3));
        return Booking.Confirm(hold, Now.AddMinutes(-3));
    }
}
