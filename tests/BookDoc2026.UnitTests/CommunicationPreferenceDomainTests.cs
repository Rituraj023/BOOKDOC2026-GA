using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.UnitTests;

public sealed class CommunicationPreferenceDomainTests
{
    [Fact]
    public void PreferenceEvidence_NormalizesPurposeAndPreservesQuietHours()
    {
        var recorded = CommunicationPreferenceEvent.Record(
            10, 20, 30, " appointment.reminder ",
            CommunicationMessageClass.Transactional,
            CommunicationChannel.WhatsApp,
            CommunicationPreferenceDecision.Allowed,
            new TimeOnly(21, 0), new TimeOnly(8, 0),
            "Asia/Kolkata", "StaffRecorded", "CONSENT-1", 40,
            DateTimeOffset.Parse("2026-08-18T04:00:00+00:00"));

        Assert.Equal("APPOINTMENT.REMINDER", recorded.PurposeCode);
        Assert.Equal(new TimeOnly(21, 0), recorded.QuietHoursStart);
        Assert.Equal(CommunicationPreferenceDecision.Allowed, recorded.Decision);
    }

    [Fact]
    public void PreferenceEvidence_RequiresCompleteQuietHours()
    {
        Assert.Throws<DomainRuleException>(() => CommunicationPreferenceEvent.Record(
            10, 20, 30, "Appointment.Reminder",
            CommunicationMessageClass.Transactional,
            CommunicationChannel.Email,
            CommunicationPreferenceDecision.Allowed,
            new TimeOnly(21, 0), null,
            "Asia/Kolkata", "StaffRecorded", null, 40,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CallbackInbox_TransitionsOnceAndBuildsAppendOnlyStatusEvidence()
    {
        var callback = ProviderCallbackInbox.Receive(
            10, 20, 30, "provider", "event-1", "message-1",
            ProviderDeliveryStatus.Delivered,
            DateTimeOffset.Parse("2026-08-18T03:59:00+00:00"),
            "key-1", new string('A', 64),
            DateTimeOffset.Parse("2026-08-18T04:00:00+00:00"));
        var operationId = Guid.NewGuid();
        var status = MessageDeliveryStatusEvent.Record(callback, operationId, DateTimeOffset.UtcNow);
        callback.MarkProcessed(DateTimeOffset.UtcNow);

        Assert.Equal(ProviderCallbackInboxStatus.Processed, callback.Status);
        Assert.Equal(callback.Id, status.CallbackInboxId);
        Assert.Equal(operationId, status.OperationId);
        Assert.Throws<DomainRuleException>(() => callback.MarkFailed("late_failure", DateTimeOffset.UtcNow));
    }
}
