namespace BookDoc2026.Domain.Foundation;

public enum TenantApplicationStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 3
}

public enum CommunicationVerificationStatus
{
    NotRequired = 1,
    Pending = 2,
    Verified = 3,
    Failed = 4
}

public enum OutboxStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    RetryScheduled = 4,
    DeadLetter = 5
}
