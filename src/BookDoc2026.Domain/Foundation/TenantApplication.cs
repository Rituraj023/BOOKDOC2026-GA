using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Foundation;

public sealed class TenantApplication : Entity
{
    private TenantApplication()
    {
    }

    public string LegalName { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string ContactEmail { get; private set; } = string.Empty;

    public string FirstBranchName { get; private set; } = string.Empty;

    public string FirstBranchCode { get; private set; } = string.Empty;

    public TenantApplicationStatus Status { get; private set; }

    public string SubmissionSource { get; private set; } = string.Empty;

    public long? ReviewedByActorId { get; private set; }

    public DateTimeOffset? ReviewedUtc { get; private set; }

    public long Version { get; private set; } = 1;

    public static TenantApplication Submit(
        string legalName,
        string slug,
        string contactEmail,
        string firstBranchName,
        string firstBranchCode,
        string submissionSource,
        DateTimeOffset now)
    {
        var application = new TenantApplication
        {
            LegalName = Required(legalName, nameof(legalName), 200),
            Slug = NormalizeSlug(slug),
            ContactEmail = Required(contactEmail, nameof(contactEmail), 320).ToLowerInvariant(),
            FirstBranchName = Required(firstBranchName, nameof(firstBranchName), 160),
            FirstBranchCode = Required(firstBranchCode, nameof(firstBranchCode), 30).ToUpperInvariant(),
            SubmissionSource = Required(submissionSource, nameof(submissionSource), 40),
            Status = TenantApplicationStatus.Submitted
        };
        application.StampCreated(now);
        return application;
    }

    public void Approve(long actorId, long expectedVersion, DateTimeOffset now)
    {
        if (Status != TenantApplicationStatus.Submitted)
        {
            throw new DomainRuleException("Only a submitted clinic application can be approved.");
        }

        if (Version != expectedVersion)
        {
            throw new ConcurrencyConflictException("The clinic application changed after it was reviewed.");
        }

        Status = TenantApplicationStatus.Approved;
        ReviewedByActorId = actorId;
        ReviewedUtc = now;
        Version++;
        StampModified(now);
    }

    private static string Required(string value, string field, int maxLength)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new DomainRuleException($"{field} is required and must not exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeSlug(string slug)
    {
        var normalized = Required(slug, nameof(slug), 80).ToLowerInvariant();
        if (normalized.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            throw new DomainRuleException("Slug can contain only lowercase letters, numbers and hyphens.");
        }

        return normalized;
    }
}
