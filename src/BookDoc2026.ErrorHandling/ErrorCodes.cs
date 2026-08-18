namespace BookDoc2026.ErrorHandling;

public static class ErrorCodes
{
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string InvalidIdentifier = "invalid_identifier";
    public const string BusinessRule = "business_rule";
    public const string Validation = "validation_error";
    public const string RateLimited = "rate_limited";
    public const string DependencyFailure = "dependency_failure";
    public const string Unavailable = "service_unavailable";
    public const string Timeout = "timeout";
    public const string Cancelled = "cancelled";
    public const string InvalidResponse = "invalid_response";
    public const string Internal = "internal_error";
}
