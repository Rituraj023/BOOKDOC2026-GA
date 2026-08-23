namespace BookDoc2026.Domain.Workforce;

public enum PractitionerStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Inactive = 4
}

public enum CredentialVerificationStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

public enum PractitionerAssignmentStatus
{
    Active = 1,
    Suspended = 2,
    Ended = 3
}
