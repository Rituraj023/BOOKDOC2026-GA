using System.Text.Json.Serialization;

namespace BookDoc2026.ErrorHandling;

[JsonConverter(typeof(JsonStringEnumConverter<ErrorCategory>))]
public enum ErrorCategory
{
    Unknown = 0,
    Validation = 1,
    Authentication = 2,
    Authorization = 3,
    NotFound = 4,
    Conflict = 5,
    RateLimited = 6,
    Dependency = 7,
    Unavailable = 8,
    Timeout = 9,
    Cancelled = 10,
    Internal = 11
}

public enum ErrorRecoveryAction
{
    None = 0,
    CorrectInput = 1,
    Reauthenticate = 2,
    RequestAccess = 3,
    Refresh = 4,
    Retry = 5,
    ContactSupport = 6
}
