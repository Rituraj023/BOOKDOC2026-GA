using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Communications;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Templates;

namespace BookDoc2026.Application.Communications;

public sealed class CommunicationService(
    ICommunicationRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext,
    ITemplateRenderer renderer)
{
    public async Task<IReadOnlyCollection<MessageTemplateResponse>> ListTemplatesAsync(
        long branchId,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.MessageTemplatesView, branchId, cancellationToken);
        var templates = await repository.ListTemplatesAsync(
            branch.TenantId,
            branch.OrganizationId,
            branch.Id,
            cancellationToken);
        return templates.Select(Map).ToArray();
    }

    public async Task<CommunicationPreferenceResponse> RecordPreferenceAsync(
        long branchId,
        long stakeholderId,
        RecordCommunicationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.CommunicationPreferencesManage, branchId, cancellationToken);
        if (!await repository.StakeholderExistsAsync(stakeholderId, cancellationToken))
            throw new NotFoundException("Stakeholder was not found in the current tenant scope.");
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId); }
        catch (TimeZoneNotFoundException) { throw new DomainRuleException("Communication preference time zone is not supported."); }
        catch (InvalidTimeZoneException) { throw new DomainRuleException("Communication preference time zone is invalid."); }

        var preference = CommunicationPreferenceEvent.Record(
            branch.TenantId,
            branch.Id,
            stakeholderId,
            request.PurposeCode,
            ParseEnum<CommunicationMessageClass>(request.MessageClass, "Message class"),
            ParseEnum<CommunicationChannel>(request.Channel, "Communication channel"),
            ParseEnum<CommunicationPreferenceDecision>(request.Decision, "Preference decision"),
            request.QuietHoursStart,
            request.QuietHoursEnd,
            request.TimeZoneId,
            request.EvidenceSource,
            request.EvidenceReference,
            actor.ActorId,
            clock.UtcNow);
        await repository.AddPreferenceEventAsync(preference, cancellationToken);
        await AuditAsync(branch.Id, "CommunicationPreference.Recorded", preference.Id,
            new { preference.StakeholderId, preference.PurposeCode, preference.MessageClass, preference.Channel, preference.Decision },
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(preference);
    }

    public async Task<IReadOnlyCollection<CommunicationPreferenceResponse>> ListPreferencesAsync(
        long branchId,
        long stakeholderId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.CommunicationPreferencesView, branchId, cancellationToken);
        if (!await repository.StakeholderExistsAsync(stakeholderId, cancellationToken))
            throw new NotFoundException("Stakeholder was not found in the current tenant scope.");
        return (await repository.ListPreferenceEventsAsync(stakeholderId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyCollection<ProviderCallbackInboxResponse>> ListCallbacksAsync(
        long branchId,
        int take,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.ProviderCallbacksView, branchId, cancellationToken);
        if (take is < 1 or > 100) throw new DomainRuleException("Callback page size must be between 1 and 100.");
        return (await repository.ListCallbacksAsync(branch.TenantId, branch.Id, take, cancellationToken))
            .Select(Map).ToArray();
    }

    public async Task<MessageTemplateResponse> CreateBranchVersionAsync(
        long branchId,
        CreateMessageTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.MessageTemplatesManage, branchId, cancellationToken);
        var channel = ParseEnum<CommunicationChannel>(request.Channel, "Template channel");
        var contentKind = ParseEnum<MessageTemplateContentKind>(request.ContentKind, "Template content kind");
        var key = request.Key?.Trim() ?? string.Empty;
        var culture = request.Culture?.Trim() ?? string.Empty;
        var version = await repository.GetNextBranchTemplateVersionAsync(
            branch.TenantId,
            branch.OrganizationId,
            branch.Id,
            key,
            channel,
            culture,
            cancellationToken);
        var template = MessageTemplate.CreateDraft(
            branch.TenantId,
            branch.OrganizationId,
            branch.Id,
            key,
            version,
            channel,
            culture,
            contentKind,
            request.BodyTemplate,
            request.SubjectTemplate,
            clock.UtcNow);
        await repository.AddTemplateAsync(template, cancellationToken);
        await AuditAsync(branch.Id, "MessageTemplate.DraftCreated", template,
            new { template.Key, template.TemplateVersion, template.Channel, template.Culture, Scope = "Branch" },
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task<MessageTemplatePreviewResponse> PreviewAsync(
        long branchId,
        long templateId,
        PreviewMessageTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.MessageTemplatesView, branchId, cancellationToken);
        var template = await RequireEffectiveTemplateAsync(branch, templateId, cancellationToken);
        var values = request.Values ?? new Dictionary<string, string?>();
        if (values.Count > 100
            || values.Any(value => string.IsNullOrWhiteSpace(value.Key)
                || value.Key.Length > 100
                || (value.Value?.Length ?? 0) > 4_000))
        {
            throw new DomainRuleException("Template preview supports at most 100 values with bounded keys and values.");
        }

        try
        {
            var rendered = renderer.Render(new TemplateRenderRequest(
                ToDefinition(template),
                values));
            return new MessageTemplatePreviewResponse(rendered.Subject, rendered.Body, rendered.ContentKind.ToString());
        }
        catch (TemplateRenderException exception)
        {
            throw new DomainRuleException(exception.Message);
        }
    }

    public Task<MessageTemplateResponse> PublishAsync(
        long branchId,
        long templateId,
        long expectedRevision,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(branchId, templateId, expectedRevision, publish: true, cancellationToken);

    public Task<MessageTemplateResponse> RetireAsync(
        long branchId,
        long templateId,
        long expectedRevision,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(branchId, templateId, expectedRevision, publish: false, cancellationToken);

    public async Task<IReadOnlyCollection<MessageDeliveryAttemptResponse>> ListDeliveryAttemptsAsync(
        long branchId,
        int take,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null
            || !actor.BranchIds.Contains(branchId)
            || !actor.HasPermission(FoundationPermissions.MessageDeliveriesView))
        {
            throw new ForbiddenException("The actor is not authorized to view message delivery status for this branch.");
        }

        if (take is < 1 or > 100)
        {
            throw new DomainRuleException("Delivery status page size must be between 1 and 100.");
        }

        var attempts = await repository.ListDeliveryAttemptsAsync(
            actor.TenantId.Value,
            branchId,
            take,
            cancellationToken);
        return attempts.Select(attempt => new MessageDeliveryAttemptResponse(
            publicIds.Encode(PublicIdKind.MessageDeliveryAttempt, attempt.Id, attempt.TenantId),
            attempt.Channel.ToString(),
            attempt.TemplateKey,
            attempt.TemplateVersion,
            attempt.AttemptNumber,
            attempt.ProviderCode,
            attempt.RecipientHint,
            attempt.Status.ToString(),
            attempt.ProviderMessageId,
            attempt.ErrorCode,
            attempt.CreatedUtc)).ToArray();
    }

    private async Task<MessageTemplateResponse> ChangeStatusAsync(
        long branchId,
        long templateId,
        long expectedRevision,
        bool publish,
        CancellationToken cancellationToken)
    {
        var permission = publish
            ? FoundationPermissions.MessageTemplatesPublish
            : FoundationPermissions.MessageTemplatesManage;
        var branch = await RequireBranchAsync(permission, branchId, cancellationToken);
        var template = await RequireEffectiveTemplateAsync(branch, templateId, cancellationToken);
        if (template.BranchId != branch.Id)
        {
            throw new ForbiddenException("Inherited message templates cannot be changed through a branch override route.");
        }

        if (publish)
        {
            template.Publish(expectedRevision, clock.UtcNow);
        }
        else
        {
            template.Retire(expectedRevision, clock.UtcNow);
        }

        await AuditAsync(
            branch.Id,
            publish ? "MessageTemplate.Published" : "MessageTemplate.Retired",
            template,
            new { template.Key, template.TemplateVersion, template.Channel, template.Culture, template.Revision },
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    private async Task<Branch> RequireBranchAsync(
        string permission,
        long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("The actor is not authorized for this communication operation.");
        }

        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private async Task<MessageTemplate> RequireEffectiveTemplateAsync(
        Branch branch,
        long templateId,
        CancellationToken cancellationToken)
    {
        var template = await repository.GetTemplateAsync(templateId, cancellationToken)
            ?? throw new NotFoundException("Message template was not found in the current tenant scope.");
        var isEffective = template.OrganizationId is null
            || (template.OrganizationId == branch.OrganizationId && template.BranchId is null)
            || template.BranchId == branch.Id;
        if (!isEffective)
        {
            throw new NotFoundException("Message template was not found in the requested branch scope.");
        }

        return template;
    }

    private async Task AuditAsync(
        long branchId,
        string action,
        MessageTemplate template,
        object data,
        CancellationToken cancellationToken) =>
        await repository.AddAuditEventAsync(AuditEvent.Record(
            actor.TenantId,
            branchId,
            actor.ActorId,
            action,
            nameof(MessageTemplate),
            template.Id,
            JsonSerializer.Serialize(data),
            correlationContext.CorrelationId,
            clock.UtcNow), cancellationToken);

    private async Task AuditAsync(
        long branchId,
        string action,
        long entityId,
        object data,
        CancellationToken cancellationToken) =>
        await repository.AddAuditEventAsync(AuditEvent.Record(
            actor.TenantId,
            branchId,
            actor.ActorId,
            action,
            nameof(CommunicationPreferenceEvent),
            entityId,
            JsonSerializer.Serialize(data),
            correlationContext.CorrelationId,
            clock.UtcNow), cancellationToken);

    private CommunicationPreferenceResponse Map(CommunicationPreferenceEvent preference) => new(
        publicIds.Encode(PublicIdKind.CommunicationPreferenceEvent, preference.Id, preference.TenantId),
        publicIds.Encode(PublicIdKind.Stakeholder, preference.StakeholderId, preference.TenantId),
        preference.PurposeCode,
        preference.MessageClass.ToString(),
        preference.Channel.ToString(),
        preference.Decision.ToString(),
        preference.QuietHoursStart,
        preference.QuietHoursEnd,
        preference.TimeZoneId,
        preference.EvidenceSource,
        preference.EvidenceReference,
        preference.CreatedUtc);

    private ProviderCallbackInboxResponse Map(ProviderCallbackInbox callback) => new(
        publicIds.Encode(PublicIdKind.ProviderCallbackInbox, callback.Id, callback.TenantId),
        callback.ProviderCode,
        callback.ExternalEventId,
        callback.ProviderMessageId,
        callback.DeliveryStatus.ToString(),
        callback.Status.ToString(),
        callback.ErrorCode,
        callback.OccurredUtc,
        callback.CreatedUtc,
        callback.ProcessedUtc);

    private MessageTemplateResponse Map(MessageTemplate template) => new(
        publicIds.Encode(PublicIdKind.MessageTemplate, template.Id, template.TenantId),
        template.BranchId.HasValue ? "Branch" : template.OrganizationId.HasValue ? "Organization" : "Tenant",
        template.Key,
        template.TemplateVersion,
        template.Channel.ToString(),
        template.Culture,
        template.ContentKind.ToString(),
        template.SubjectTemplate,
        template.BodyTemplate,
        template.Status.ToString(),
        template.PublishedUtc,
        template.Revision);

    private static TemplateDefinition ToDefinition(MessageTemplate template) => new(
        template.Key,
        template.TemplateVersion,
        (TemplateChannel)(int)template.Channel,
        template.Culture,
        (TemplateContentKind)(int)template.ContentKind,
        template.BodyTemplate,
        template.SubjectTemplate);

    private static TEnum ParseEnum<TEnum>(string value, string label) where TEnum : struct, Enum
    {
        if (!Enum.TryParse(value, true, out TEnum parsed) || !Enum.IsDefined(parsed))
        {
            throw new DomainRuleException($"{label} is not supported.");
        }

        return parsed;
    }
}
