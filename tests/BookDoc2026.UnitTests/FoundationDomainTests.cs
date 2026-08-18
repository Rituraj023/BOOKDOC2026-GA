using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.UnitTests;

public sealed class FoundationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Application_Approval_RequiresCurrentVersionAndSubmittedState()
    {
        var application = TenantApplication.Submit(
            "Clinic A",
            "clinic-a",
            "owner@example.invalid",
            "Main Branch",
            "DEL01",
            "ClinicApplicant",
            Now);

        Assert.Throws<ConcurrencyConflictException>(() => application.Approve(NumericId.Next(), 2, Now));

        application.Approve(NumericId.Next(), 1, Now);

        Assert.Equal(TenantApplicationStatus.Approved, application.Status);
        Assert.Equal(2, application.Version);
        Assert.Throws<DomainRuleException>(() => application.Approve(NumericId.Next(), 2, Now));
    }

    [Fact]
    public void BranchConfiguration_Update_IsVersionedAndProviderGated()
    {
        var configuration = BranchConfiguration.CreateDefault(NumericId.Next(), NumericId.Next(), "DEL01", Now);

        configuration.Update(
            1,
            "https://cdn.example.invalid/logo.png",
            "DEL-INV",
            "DEL-RCT",
            "billing@example.invalid",
            "+919999999999",
            Now.AddMinutes(1));

        Assert.Equal(2, configuration.Version);
        Assert.Equal(CommunicationVerificationStatus.Pending, configuration.CommunicationVerificationStatus);
        Assert.Throws<ConcurrencyConflictException>(() => configuration.Update(
            1,
            null,
            "NEW-INV",
            "NEW-RCT",
            null,
            null,
            Now.AddMinutes(2)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("HAS SPACE")]
    [InlineData("PREFIX_THAT_IS_LONGER_THAN_THIRTY_CHARACTERS")]
    public void BranchConfiguration_RejectsInvalidNumberPrefixes(string prefix)
    {
        var configuration = BranchConfiguration.CreateDefault(NumericId.Next(), NumericId.Next(), "DEL01", Now);

        Assert.Throws<DomainRuleException>(() => configuration.Update(
            1,
            null,
            prefix,
            "VALID-RCT",
            null,
            null,
            Now));
    }

    [Fact]
    public void CompletedOutboxMessage_CannotBeClaimedAgain()
    {
        var message = OutboxMessage.Enqueue(null, null, "test", "{}", Now, Guid.NewGuid(), "test-correlation");
        message.Claim("worker-1", Now, TimeSpan.FromMinutes(1));
        message.Complete("worker-1", Now.AddSeconds(1));

        Assert.Equal(OutboxStatus.Completed, message.Status);
        Assert.Throws<DomainRuleException>(() => message.Claim("worker-2", Now.AddSeconds(2), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void MessageTemplate_PublishAndRetireRequireCurrentRevision()
    {
        var template = MessageTemplate.CreateDraft(
            NumericId.Next(),
            null,
            null,
            "Booking.Confirmed.Patient",
            1,
            CommunicationChannel.Email,
            "en-IN",
            MessageTemplateContentKind.Html,
            "<p>Booking {{BookingNumber}} confirmed.</p>",
            "Booking confirmed",
            Now);

        Assert.Throws<ConcurrencyConflictException>(() => template.Publish(2, Now));
        template.Publish(1, Now.AddMinutes(1));
        Assert.Equal(MessageTemplateStatus.Published, template.Status);
        Assert.Equal(2, template.Revision);

        template.Retire(2, Now.AddMinutes(2));
        Assert.Equal(MessageTemplateStatus.Retired, template.Status);
        Assert.Equal(3, template.Revision);
    }

    [Fact]
    public void Outbox_PermanentFailureDeadLettersWithSafeCode()
    {
        var message = OutboxMessage.Enqueue(
            NumericId.Next(),
            NumericId.Next(),
            "test",
            "{}",
            Now,
            Guid.NewGuid(),
            "test-correlation");
        message.Claim("worker-1", Now, TimeSpan.FromMinutes(1));

        message.Fail("worker-1", "provider_rejected", false, Now.AddSeconds(1), 5);

        Assert.Equal(OutboxStatus.DeadLetter, message.Status);
        Assert.Equal("provider_rejected", message.LastErrorCode);
        Assert.Equal(1, message.AttemptCount);
    }
}
