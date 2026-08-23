using BookDoc2026.Application.Communications;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.UnitTests;

public sealed class BookingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ActiveHold_CreatesDistinctBookingAndCopiedResourceAllocation()
    {
        var hold = CreateHold();
        var reservation = ResourceReservation.Create(
            hold.TenantId,
            hold.BranchId,
            hold.Id,
            NumericId.Next(),
            hold.StartUtc,
            hold.EndUtc,
            1,
            Now,
            "ImagingMachine");

        var booking = Booking.Confirm(hold, Now);
        var allocation = BookingResourceAllocation.FromReservation(booking, reservation, Now);
        hold.Confirm(1, Now);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(SchedulingHoldStatus.Confirmed, hold.Status);
        Assert.StartsWith("BKG-", booking.BookingNumber, StringComparison.Ordinal);
        Assert.Equal(24, booking.BookingNumber.Length);
        Assert.Equal(reservation.Id, allocation.HoldReservationId);
        Assert.Equal("IMAGINGMACHINE", allocation.RequirementRoleCode);
    }

    [Fact]
    public void BookingConfirmation_RejectsExpiredOrStaleHold()
    {
        var hold = CreateHold(expiresUtc: Now.AddMinutes(1));

        Assert.Throws<DomainRuleException>(() => Booking.Confirm(hold, Now.AddMinutes(2)));

        hold = CreateHold();
        Assert.Throws<ConcurrencyConflictException>(() => hold.Confirm(2, Now));
    }

    [Fact]
    public void BookingLifecycle_CancelsAndLinksAReplacementWithoutMutatingIdentity()
    {
        var originalHold = CreateHold();
        var original = Booking.Confirm(originalHold, Now);
        var replacementHold = SchedulingHold.Create(
            original.TenantId, original.BranchId, original.PatientId, original.ServiceId,
            Guid.NewGuid(), new string('B', 64), original.StartUtc.AddHours(2),
            original.EndUtc.AddHours(2), Now.AddMinutes(10), Now);
        var replacement = Booking.RescheduleFrom(replacementHold, original, Now);

        original.ReplaceWith(replacement.Id, 1, "Patient requested another time", Now.AddMinutes(1));

        Assert.Equal(BookingStatus.Cancelled, original.Status);
        Assert.Equal(replacement.Id, original.ReplacedByBookingId);
        Assert.Equal(original.Id, replacement.PreviousBookingId);
        Assert.Equal(2, original.Version);
        Assert.Throws<ConcurrencyConflictException>(() =>
            original.Cancel(1, "Duplicate request", Now.AddMinutes(2)));
    }

    [Fact]
    public void Waitlist_UsesVersionedWaitingPromotedAndWithdrawnTransitions()
    {
        var hold = CreateHold();
        var waitlist = BookingWaitlistEntry.Create(
            hold.TenantId, hold.BranchId, hold.PatientId, hold.ServiceId,
            hold.StartUtc.AddMinutes(-30), hold.StartUtc.AddMinutes(30), 2,
            "Earlier appointment requested", Now);
        var promoted = Booking.PromoteFromWaitlist(hold, waitlist, Now);
        waitlist.Promote(promoted.Id, 1, Now.AddMinutes(1));

        Assert.Equal(BookingWaitlistStatus.Promoted, waitlist.Status);
        Assert.Equal(promoted.Id, waitlist.PromotedBookingId);
        Assert.Equal(waitlist.Id, promoted.WaitlistEntryId);
        Assert.Throws<ConcurrencyConflictException>(() =>
            waitlist.Withdraw(1, "No longer needed", Now.AddMinutes(2)));

        var withdrawn = BookingWaitlistEntry.Create(
            hold.TenantId, hold.BranchId, hold.PatientId, hold.ServiceId,
            hold.StartUtc, hold.StartUtc.AddHours(1), 3, "Backup appointment", Now);
        withdrawn.Withdraw(1, "Patient declined", Now.AddMinutes(1));
        Assert.Equal(BookingWaitlistStatus.Withdrawn, withdrawn.Status);
    }

    [Fact]
    public void RequirementEvaluator_RequiresEveryMandatoryCategoryRole()
    {
        var hold = CreateHold();
        var category = ResourceCategory.Create(
            hold.TenantId, null, "CT", "CT Scanner", ResourceKind.ImagingModality, Now);
        var resource = BookableResource.Create(
            hold.TenantId,
            hold.BranchId,
            category.Id,
            "CT-01",
            "CT Scanner 1",
            CapacityMode.Exclusive,
            1,
            "Asia/Kolkata",
            null,
            Now);
        var reservation = ResourceReservation.Create(
            hold.TenantId,
            hold.BranchId,
            hold.Id,
            resource.Id,
            hold.StartUtc,
            hold.EndUtc,
            1,
            Now,
            "Scanner");
        var requirement = ServiceResourceRequirement.Create(
            hold.TenantId,
            hold.ServiceId,
            category.Id,
            "Scanner",
            1,
            false,
            Now);

        SchedulingRequirementEvaluator.EnsureComplete(
            [reservation],
            new Dictionary<long, BookableResource> { [resource.Id] = resource },
            [requirement]);

        var missing = ServiceResourceRequirement.Create(
            hold.TenantId,
            hold.ServiceId,
            category.Id,
            "Radiographer",
            1,
            false,
            Now);
        Assert.Throws<DomainRuleException>(() => SchedulingRequirementEvaluator.EnsureComplete(
            [reservation],
            new Dictionary<long, BookableResource> { [resource.Id] = resource },
            [requirement, missing]));
    }

    [Fact]
    public void PreferenceEvaluator_FailsClosedAndHonorsOvernightQuietHours()
    {
        Assert.Equal(
            "preference_not_recorded",
            CommunicationPreferenceEvaluator.Evaluate(null, Now).Code);
        var allowed = Preference(
            CommunicationPreferenceDecision.Allowed,
            new TimeOnly(21, 0),
            new TimeOnly(8, 0));

        Assert.True(CommunicationPreferenceEvaluator.Evaluate(allowed, Now).CanSend);
        var quietTime = new DateTimeOffset(2026, 8, 23, 18, 0, 0, TimeSpan.Zero);
        Assert.Equal(
            "preference_quiet_hours",
            CommunicationPreferenceEvaluator.Evaluate(allowed, quietTime).Code);
        Assert.Equal(
            "preference_denied",
            CommunicationPreferenceEvaluator.Evaluate(
                Preference(CommunicationPreferenceDecision.Denied, null, null),
                Now).Code);
    }

    private static SchedulingHold CreateHold(DateTimeOffset? expiresUtc = null) =>
        SchedulingHold.Create(
            NumericId.Next(),
            NumericId.Next(),
            NumericId.Next(),
            NumericId.Next(),
            Guid.NewGuid(),
            new string('A', 64),
            Now.AddDays(1),
            Now.AddDays(1).AddMinutes(30),
            expiresUtc ?? Now.AddMinutes(10),
            Now);

    private static CommunicationPreferenceEvent Preference(
        CommunicationPreferenceDecision decision,
        TimeOnly? quietStart,
        TimeOnly? quietEnd) =>
        CommunicationPreferenceEvent.Record(
            NumericId.Next(),
            NumericId.Next(),
            NumericId.Next(),
            "Booking.Confirmation",
            CommunicationMessageClass.Transactional,
            CommunicationChannel.Email,
            decision,
            quietStart,
            quietEnd,
            "Asia/Kolkata",
            "UnitTest",
            null,
            NumericId.Next(),
            Now);
}
