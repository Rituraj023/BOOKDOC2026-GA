using System.Security.Cryptography;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class Booking : TenantScopedEntity
{
    private Booking() { }

    public long BranchId { get; private set; }
    public long HoldId { get; private set; }
    public long PatientId { get; private set; }
    public long ServiceId { get; private set; }
    public string BookingNumber { get; private set; } = string.Empty;
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTimeOffset ConfirmedUtc { get; private set; }
    public DateTimeOffset? CancelledUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public long? PreviousBookingId { get; private set; }
    public long? ReplacedByBookingId { get; private set; }
    public long? WaitlistEntryId { get; private set; }
    public long Version { get; private set; } = 1;

    public static Booking Confirm(SchedulingHold hold, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hold);
        if (hold.Status != SchedulingHoldStatus.Active || hold.ExpiresUtc <= now)
            throw new DomainRuleException("Only an active, unexpired hold can become a booking.");

        var booking = new Booking
        {
            TenantId = hold.TenantId,
            BranchId = hold.BranchId,
            HoldId = hold.Id,
            PatientId = hold.PatientId,
            ServiceId = hold.ServiceId,
            StartUtc = hold.StartUtc,
            EndUtc = hold.EndUtc,
            Status = BookingStatus.Confirmed,
            ConfirmedUtc = now
        };
        booking.BookingNumber = CreateNumber(booking.Id);
        booking.StampCreated(now);
        return booking;
    }

    public static Booking PromoteFromWaitlist(
        SchedulingHold hold,
        BookingWaitlistEntry waitlist,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(waitlist);
        if (waitlist.TenantId != hold.TenantId
            || waitlist.BranchId != hold.BranchId
            || waitlist.PatientId != hold.PatientId
            || waitlist.ServiceId != hold.ServiceId
            || hold.StartUtc < waitlist.EarliestStartUtc
            || hold.StartUtc > waitlist.LatestStartUtc)
            throw new DomainRuleException("The held schedule does not match the waitlist entry.");

        var booking = Confirm(hold, now);
        booking.WaitlistEntryId = waitlist.Id;
        return booking;
    }

    public static Booking RescheduleFrom(SchedulingHold hold, Booking previous, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (previous.Status != BookingStatus.Confirmed
            || previous.TenantId != hold.TenantId
            || previous.BranchId != hold.BranchId
            || previous.PatientId != hold.PatientId
            || previous.ServiceId != hold.ServiceId)
            throw new DomainRuleException("The replacement hold does not match the confirmed booking.");

        var booking = Confirm(hold, now);
        booking.PreviousBookingId = previous.Id;
        return booking;
    }

    public void Cancel(long expectedVersion, string reason, DateTimeOffset now)
    {
        EnsureCanLeaveConfirmed(expectedVersion, reason, now);
        Status = BookingStatus.Cancelled;
        CancelledUtc = now;
        CancellationReason = NormalizeReason(reason);
        Version++;
        StampModified(now);
    }

    public void ReplaceWith(long replacementBookingId, long expectedVersion, string reason, DateTimeOffset now)
    {
        if (replacementBookingId <= 0 || replacementBookingId == Id)
            throw new DomainRuleException("A valid replacement booking is required.");
        EnsureCanLeaveConfirmed(expectedVersion, reason, now);
        Status = BookingStatus.Cancelled;
        CancelledUtc = now;
        CancellationReason = NormalizeReason(reason);
        ReplacedByBookingId = replacementBookingId;
        Version++;
        StampModified(now);
    }

    private void EnsureCanLeaveConfirmed(long expectedVersion, string reason, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The booking changed after it was loaded.");
        if (Status != BookingStatus.Confirmed)
            throw new DomainRuleException("Only a confirmed booking can change lifecycle state.");
        if (now >= StartUtc)
            throw new DomainRuleException("A booking cannot be cancelled or rescheduled after it starts.");
        _ = NormalizeReason(reason);
    }

    private static string NormalizeReason(string reason)
    {
        var normalized = reason?.Trim() ?? string.Empty;
        if (normalized.Length is < 3 or > 250)
            throw new DomainRuleException("A lifecycle reason between 3 and 250 characters is required.");
        return normalized;
    }

    private static string CreateNumber(long id)
    {
        var digest = SHA256.HashData(BitConverter.GetBytes(id));
        return $"BKG-{Convert.ToHexString(digest)[..20]}";
    }
}
