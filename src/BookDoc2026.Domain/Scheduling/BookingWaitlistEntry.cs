using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class BookingWaitlistEntry : TenantScopedEntity
{
    private BookingWaitlistEntry() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public long ServiceId { get; private set; }
    public DateTimeOffset EarliestStartUtc { get; private set; }
    public DateTimeOffset LatestStartUtc { get; private set; }
    public int Priority { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public BookingWaitlistStatus Status { get; private set; }
    public long? PromotedBookingId { get; private set; }
    public DateTimeOffset? PromotedUtc { get; private set; }
    public DateTimeOffset? WithdrawnUtc { get; private set; }
    public string? WithdrawalReason { get; private set; }
    public long Version { get; private set; } = 1;

    public static BookingWaitlistEntry Create(
        long tenantId,
        long branchId,
        long patientId,
        long serviceId,
        DateTimeOffset earliestStartUtc,
        DateTimeOffset latestStartUtc,
        int priority,
        string reason,
        DateTimeOffset now)
    {
        var normalizedReason = NormalizeReason(reason);
        if (tenantId <= 0 || branchId <= 0 || patientId <= 0 || serviceId <= 0
            || earliestStartUtc <= now || latestStartUtc < earliestStartUtc || priority is < 1 or > 5)
            throw new DomainRuleException("Waitlist scope, window or priority is invalid.");

        var entry = new BookingWaitlistEntry
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            ServiceId = serviceId,
            EarliestStartUtc = earliestStartUtc,
            LatestStartUtc = latestStartUtc,
            Priority = priority,
            Reason = normalizedReason,
            Status = BookingWaitlistStatus.Waiting
        };
        entry.StampCreated(now);
        return entry;
    }

    public void Promote(long bookingId, long expectedVersion, DateTimeOffset now)
    {
        EnsureWaiting(expectedVersion);
        if (bookingId <= 0) throw new DomainRuleException("A promoted booking is required.");
        Status = BookingWaitlistStatus.Promoted;
        PromotedBookingId = bookingId;
        PromotedUtc = now;
        Version++;
        StampModified(now);
    }

    public void Withdraw(long expectedVersion, string reason, DateTimeOffset now)
    {
        EnsureWaiting(expectedVersion);
        Status = BookingWaitlistStatus.Withdrawn;
        WithdrawalReason = NormalizeReason(reason);
        WithdrawnUtc = now;
        Version++;
        StampModified(now);
    }

    private void EnsureWaiting(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The waitlist entry changed after it was loaded.");
        if (Status != BookingWaitlistStatus.Waiting)
            throw new DomainRuleException("Only a waiting entry can change state.");
    }

    private static string NormalizeReason(string reason)
    {
        var normalized = reason?.Trim() ?? string.Empty;
        if (normalized.Length is < 3 or > 250)
            throw new DomainRuleException("A reason between 3 and 250 characters is required.");
        return normalized;
    }
}
