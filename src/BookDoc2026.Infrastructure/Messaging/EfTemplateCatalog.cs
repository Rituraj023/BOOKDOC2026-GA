using BookDoc2026.Domain.Communications;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Templates;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Messaging;

public sealed class EfTemplateCatalog(BookDocDbContext dbContext) : ITemplateCatalog
{
    public async ValueTask<TemplateDefinition?> ResolvePublishedAsync(
        TemplateSelection selection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var channel = ToDomainChannel(selection.Channel);
        var candidates = await dbContext.MessageTemplates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(template =>
                template.TenantId == selection.Scope.TenantId
                && template.Key == selection.Key
                && template.Channel == channel
                && template.Culture == selection.Culture
                && template.Status == MessageTemplateStatus.Published
                && (!selection.Version.HasValue || template.TemplateVersion == selection.Version.Value)
                && ((template.OrganizationId == null && template.BranchId == null)
                    || (selection.Scope.OrganizationId.HasValue
                        && template.OrganizationId == selection.Scope.OrganizationId
                        && template.BranchId == null)
                    || (selection.Scope.OrganizationId.HasValue
                        && selection.Scope.BranchId.HasValue
                        && template.OrganizationId == selection.Scope.OrganizationId
                        && template.BranchId == selection.Scope.BranchId)))
            .ToListAsync(cancellationToken);

        var selected = candidates
            .OrderByDescending(template => Specificity(template, selection.Scope))
            .ThenByDescending(template => template.TemplateVersion)
            .FirstOrDefault();

        return selected is null
            ? null
            : new TemplateDefinition(
                selected.Key,
                selected.TemplateVersion,
                selection.Channel,
                selected.Culture,
                selected.ContentKind == MessageTemplateContentKind.Html
                    ? TemplateContentKind.Html
                    : TemplateContentKind.PlainText,
                selected.BodyTemplate,
                selected.SubjectTemplate);
    }

    private static int Specificity(MessageTemplate template, TemplateScope scope) =>
        template.BranchId.HasValue && template.BranchId == scope.BranchId
            ? 3
            : template.OrganizationId.HasValue && template.OrganizationId == scope.OrganizationId
                ? 2
                : 1;

    private static CommunicationChannel ToDomainChannel(TemplateChannel channel) => channel switch
    {
        TemplateChannel.Email => CommunicationChannel.Email,
        TemplateChannel.Sms => CommunicationChannel.Sms,
        TemplateChannel.WhatsApp => CommunicationChannel.WhatsApp,
        TemplateChannel.Push => CommunicationChannel.Push,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "The template channel is not a message channel.")
    };
}
