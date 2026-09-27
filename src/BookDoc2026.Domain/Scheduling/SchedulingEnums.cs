namespace BookDoc2026.Domain.Scheduling;

public enum AvailabilityExceptionKind
{
    Unavailable = 1,
    CapacityOverride = 2
}

public enum SchedulingHoldStatus
{
    Active = 1,
    Confirmed = 2,
    Released = 3,
    Expired = 4
}

public enum BookingStatus
{
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    NoShow = 4
}

public enum BookingWaitlistStatus
{
    Waiting = 1,
    Promoted = 2,
    Withdrawn = 3
}

public enum BookingSlotStatus
{
    Available = 1,
    PartiallyBooked = 2,
    FullyBooked = 3,
    Blocked = 4,
    Cancelled = 5
}

public enum BookingRequestStatus
{
    PendingApproval = 1,
    UnderReview = 2,
    Approved = 3,
    Declined = 4,
    Rescheduled = 5
}
