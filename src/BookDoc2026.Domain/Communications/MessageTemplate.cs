using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Communications;

public sealed class MessageTemplate : TenantScopedEntity
{
    private MessageTemplate()
    {
    }

    public long? OrganizationId { get; private set; }

    public long? BranchId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public int TemplateVersion { get; private set; }

    public CommunicationChannel Channel { get; private set; }

    public string Culture { get; private set; } = string.Empty;

    public MessageTemplateContentKind ContentKind { get; private set; }

    public string? SubjectTemplate { get; private set; }

    public string BodyTemplate { get; private set; } = string.Empty;

    public MessageTemplateStatus Status { get; private set; }

    public DateTimeOffset? PublishedUtc { get; private set; }

    public long Revision { get; private set; } = 1;

    public static MessageTemplate CreateDraft(
        long tenantId,
        long? organizationId,
        long? branchId,
        string key,
        int templateVersion,
        CommunicationChannel channel,
        string culture,
        MessageTemplateContentKind contentKind,
        string bodyTemplate,
        string? subjectTemplate,
        DateTimeOffset now)
    {
        if (tenantId <= 0)
        {
            throw new DomainRuleException("Template tenant must be positive.");
        }

        if (branchId.HasValue && !organizationId.HasValue)
        {
            throw new DomainRuleException("A branch-scoped template requires an organization scope.");
        }

        if (templateVersion <= 0)
        {
            throw new DomainRuleException("Template version must be positive.");
        }

        if (!Enum.IsDefined(channel) || !Enum.IsDefined(contentKind))
        {
            throw new DomainRuleException("Template channel or content kind is invalid.");
        }

        ValidateText(key, 160, "Template key");
        ValidateText(culture, 20, "Template culture");
        ValidateText(bodyTemplate, 100_000, "Template body");
        if (subjectTemplate is not null && subjectTemplate.Length > 1_000)
        {
            throw new DomainRuleException("Template subject exceeds the supported length.");
        }

        var template = new MessageTemplate
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            BranchId = branchId,
            Key = key.Trim(),
            TemplateVersion = templateVersion,
            Channel = channel,
            Culture = culture.Trim(),
            ContentKind = contentKind,
            BodyTemplate = bodyTemplate,
            SubjectTemplate = string.IsNullOrWhiteSpace(subjectTemplate) ? null : subjectTemplate,
            Status = MessageTemplateStatus.Draft
        };
        template.StampCreated(now);
        return template;
    }

    public void Publish(long expectedRevision, DateTimeOffset now)
    {
        if (Revision != expectedRevision)
        {
            throw new ConcurrencyConflictException("The message template changed before it could be published.");
        }

        if (Status != MessageTemplateStatus.Draft)
        {
            throw new DomainRuleException("Only a draft message template can be published.");
        }

        Status = MessageTemplateStatus.Published;
        PublishedUtc = now;
        Revision++;
        StampModified(now);
    }

    public void Retire(long expectedRevision, DateTimeOffset now)
    {
        if (Revision != expectedRevision)
        {
            throw new ConcurrencyConflictException("The message template changed before it could be retired.");
        }

        if (Status != MessageTemplateStatus.Published)
        {
            throw new DomainRuleException("Only a published message template can be retired.");
        }

        Status = MessageTemplateStatus.Retired;
        Revision++;
        StampModified(now);
    }

    private static void ValidateText(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
        {
            throw new DomainRuleException($"{field} is required and must not exceed {maxLength} characters.");
        }
    }
}
