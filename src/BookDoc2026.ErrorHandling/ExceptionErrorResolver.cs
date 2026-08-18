namespace BookDoc2026.ErrorHandling;

public sealed class ExceptionErrorResolver(IEnumerable<IExceptionErrorMapper> mappers)
{
    private readonly IReadOnlyList<IExceptionErrorMapper> _mappers = mappers.ToArray();

    public ErrorDescriptor Resolve(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        foreach (var mapper in _mappers)
        {
            if (mapper.TryMap(exception, out var descriptor))
            {
                return descriptor;
            }
        }

        return exception switch
        {
            OperationCanceledException => new ErrorDescriptor(
                ErrorCodes.Cancelled,
                ErrorCategory.Cancelled,
                "The operation was cancelled."),
            TimeoutException => new ErrorDescriptor(
                ErrorCodes.Timeout,
                ErrorCategory.Timeout,
                "The operation timed out.",
                IsTransient: true),
            _ => ErrorDescriptor.Internal()
        };
    }
}
