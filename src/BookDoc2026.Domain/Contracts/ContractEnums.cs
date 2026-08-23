namespace BookDoc2026.Domain.Contracts;

public enum ContractStatus : byte
{
    Active = 1,
    Suspended = 2,
    Completed = 3,
    Cancelled = 4
}

public enum EntitlementReservationStatus : byte
{
    Reserved = 1,
    Consumed = 2,
    Released = 3
}
