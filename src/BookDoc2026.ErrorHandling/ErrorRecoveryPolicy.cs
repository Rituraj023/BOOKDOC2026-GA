namespace BookDoc2026.ErrorHandling;

public static class ErrorRecoveryPolicy
{
    public static ErrorRecoveryAction Recommend(ErrorDescriptor error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.IsTransient)
        {
            return ErrorRecoveryAction.Retry;
        }

        return error.Category switch
        {
            ErrorCategory.Validation => ErrorRecoveryAction.CorrectInput,
            ErrorCategory.Authentication => ErrorRecoveryAction.Reauthenticate,
            ErrorCategory.Authorization => ErrorRecoveryAction.RequestAccess,
            ErrorCategory.Conflict => ErrorRecoveryAction.Refresh,
            ErrorCategory.RateLimited or ErrorCategory.Dependency or ErrorCategory.Unavailable
                or ErrorCategory.Timeout => ErrorRecoveryAction.Retry,
            ErrorCategory.Internal => ErrorRecoveryAction.ContactSupport,
            _ => ErrorRecoveryAction.None
        };
    }
}
