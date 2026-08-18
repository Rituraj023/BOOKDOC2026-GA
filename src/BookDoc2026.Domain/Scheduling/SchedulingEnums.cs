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
