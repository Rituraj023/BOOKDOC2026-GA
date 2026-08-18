namespace BookDoc2026.Domain.Common;

public class DomainRuleException(string message) : InvalidOperationException(message);

public sealed class ConcurrencyConflictException(string message) : DomainRuleException(message);

public sealed class NotFoundException(string message) : DomainRuleException(message);

public sealed class ForbiddenException(string message) : DomainRuleException(message);

public sealed class InvalidPublicIdException(string message) : DomainRuleException(message);

public sealed class UnauthorizedException(string message) : DomainRuleException(message);
