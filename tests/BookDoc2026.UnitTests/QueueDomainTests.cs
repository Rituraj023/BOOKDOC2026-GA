using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Queues;

namespace BookDoc2026.UnitTests;

public sealed class QueueDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ImagingTicket_FollowsTheFirstXRayCtWorkflow()
    {
        var ticket = CreateTicket();

        ticket.Call(1, Now.AddMinutes(1));
        ticket.Recall(2, "Patient did not hear the first call", Now.AddMinutes(2));
        ticket.BeginPreparation(3, Now.AddMinutes(3));
        ticket.StartService(4, Now.AddMinutes(4));
        ticket.Complete(5, Now.AddMinutes(10));

        Assert.Equal(QueueTicketStatus.Completed, ticket.Status);
        Assert.Equal(2, ticket.CallCount);
        Assert.Equal(6, ticket.Version);
        Assert.StartsWith("Q-", ticket.DisplayToken, StringComparison.Ordinal);
        Assert.Equal(10, ticket.DisplayToken.Length);
    }

    [Fact]
    public void ImagingTicket_RejectsStaleAndInvalidTransitions()
    {
        var ticket = CreateTicket();

        Assert.Throws<DomainRuleException>(() => ticket.StartService(1, Now.AddMinutes(1)));
        ticket.Call(1, Now.AddMinutes(1));
        Assert.Throws<ConcurrencyConflictException>(() => ticket.BeginPreparation(1, Now.AddMinutes(2)));
        Assert.Throws<DomainRuleException>(() => ticket.Cancel(2, "x", Now.AddMinutes(2)));
    }

    [Fact]
    public void QueueEvent_IsAppendOnlyTransitionEvidenceWithoutPatientData()
    {
        var ticket = CreateTicket();
        ticket.Call(1, Now.AddMinutes(1));

        var item = QueueTicketEvent.Record(
            ticket, QueueTicketStatus.Waiting, "Called", NumericId.Next(), null, Now.AddMinutes(1));

        Assert.Equal(ticket.Id, item.TicketId);
        Assert.Equal(QueueTicketStatus.Waiting, item.FromStatus);
        Assert.Equal(QueueTicketStatus.Called, item.ToStatus);
        Assert.Equal(2, item.TicketVersion);
        Assert.Null(item.Reason);
    }

    private static QueueTicket CreateTicket() => QueueTicket.CheckIn(
        NumericId.Next(), NumericId.Next(), NumericId.Next(), NumericId.Next(), null,
        Guid.NewGuid(), QueuePriority.Normal, Now);
}
