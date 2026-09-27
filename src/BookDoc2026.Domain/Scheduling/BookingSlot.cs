using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class BookingSlot : TenantScopedEntity
{
    private BookingSlot() { }

    public long BranchId { get; private set; }
    public long PractitionerId { get; private set; }
    public long? ServiceId { get; private set; }
    public DateOnly SlotDate { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxCapacity { get; private set; }
    public int BookedCount { get; private set; }
    public BookingSlotStatus Status { get; private set; }
    public string? BlockReason { get; private set; }
    public long Version { get; private set; } = 1;

    public static BookingSlot Create(
        long tenantId,
        long branchId,
        long practitionerId,
        long? serviceId,
        DateOnly slotDate,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int durationMinutes,
        int maxCapacity,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || practitionerId <= 0)
            throw new DomainRuleException("Tenant, branch, and practitioner IDs are required.");

        if (startUtc >= endUtc)
            throw new DomainRuleException("Slot start time must be before end time.");

        if (durationMinutes <= 0 || durationMinutes > 1440)
            throw new DomainRuleException("Duration must be between 1 and 1440 minutes.");

        if (maxCapacity <= 0)
            throw new DomainRuleException("Slot max capacity must be at least 1.");

        var slot = new BookingSlot
        {
            TenantId = tenantId,
            BranchId = branchId,
            PractitionerId = practitionerId,
            ServiceId = serviceId,
            SlotDate = slotDate,
            StartUtc = startUtc,
            EndUtc = endUtc,
            DurationMinutes = durationMinutes,
            MaxCapacity = maxCapacity,
            BookedCount = 0,
            Status = BookingSlotStatus.Available
        };

        slot.StampCreated(now);
        return slot;
    }

    public void ReserveCapacity(int count, DateTimeOffset now)
    {
        if (count <= 0)
            throw new DomainRuleException("Reserved capacity count must be at least 1.");

        if (Status != BookingSlotStatus.Available && Status != BookingSlotStatus.PartiallyBooked)
            throw new DomainRuleException($"Cannot reserve slot with status {Status}. Only available or partially booked slots can be reserved.");

        if (BookedCount + count > MaxCapacity)
            throw new DomainRuleException($"Requested capacity ({count}) exceeds remaining slot capacity ({MaxCapacity - BookedCount}).");

        BookedCount += count;
        Status = BookedCount >= MaxCapacity ? BookingSlotStatus.FullyBooked : BookingSlotStatus.PartiallyBooked;
        Version++;
        StampModified(now);
    }

    public void ReleaseCapacity(int count, DateTimeOffset now)
    {
        if (count <= 0)
            throw new DomainRuleException("Released capacity count must be at least 1.");

        BookedCount = Math.Max(0, BookedCount - count);

        if (Status != BookingSlotStatus.Blocked && Status != BookingSlotStatus.Cancelled)
        {
            Status = BookedCount == 0 ? BookingSlotStatus.Available : BookingSlotStatus.PartiallyBooked;
        }

        Version++;
        StampModified(now);
    }

    public void Block(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainRuleException("A reason is required to block a slot.");

        Status = BookingSlotStatus.Blocked;
        BlockReason = reason.Trim();
        Version++;
        StampModified(now);
    }

    public void Unblock(DateTimeOffset now)
    {
        if (Status != BookingSlotStatus.Blocked)
            throw new DomainRuleException("Only a blocked slot can be unblocked.");

        BlockReason = null;
        Status = BookedCount switch
        {
            0 => BookingSlotStatus.Available,
            var b when b >= MaxCapacity => BookingSlotStatus.FullyBooked,
            _ => BookingSlotStatus.PartiallyBooked
        };

        Version++;
        StampModified(now);
    }

    public void Cancel(DateTimeOffset now)
    {
        Status = BookingSlotStatus.Cancelled;
        Version++;
        StampModified(now);
    }
}
