using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class CommunicationRepository(BookDocDbContext dbContext) : ICommunicationRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public async Task<IReadOnlyList<MessageTemplate>> ListTemplatesAsync(
        long tenantId,
        long organizationId,
        long branchId,
        CancellationToken cancellationToken) =>
        await dbContext.MessageTemplates
            .AsNoTracking()
            .Where(template => template.TenantId == tenantId
                && (template.OrganizationId == null
                    || (template.OrganizationId == organizationId && template.BranchId == null)
                    || template.BranchId == branchId))
            .OrderBy(template => template.Key)
            .ThenBy(template => template.Channel)
            .ThenBy(template => template.Culture)
            .ThenByDescending(template => template.TemplateVersion)
            .ToListAsync(cancellationToken);

    public Task<MessageTemplate?> GetTemplateAsync(long templateId, CancellationToken cancellationToken) =>
        dbContext.MessageTemplates.SingleOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public async Task<int> GetNextBranchTemplateVersionAsync(
        long tenantId,
        long organizationId,
        long branchId,
        string key,
        CommunicationChannel channel,
        string culture,
        CancellationToken cancellationToken)
    {
        var maximum = await dbContext.MessageTemplates
            .Where(template => template.TenantId == tenantId
                && template.OrganizationId == organizationId
                && template.BranchId == branchId
                && template.Key == key
                && template.Channel == channel
                && template.Culture == culture)
            .Select(template => (int?)template.TemplateVersion)
            .MaxAsync(cancellationToken);
        return checked((maximum ?? 0) + 1);
    }

    public Task AddTemplateAsync(MessageTemplate template, CancellationToken cancellationToken) =>
        dbContext.MessageTemplates.AddAsync(template, cancellationToken).AsTask();

    public Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The message template changed while the operation was being saved.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConcurrencyConflictException("A message template version was created concurrently. Refresh and try again.");
        }
    }

    public Task<bool> StakeholderExistsAsync(long stakeholderId, CancellationToken cancellationToken) =>
        dbContext.Stakeholders.AnyAsync(stakeholder => stakeholder.Id == stakeholderId, cancellationToken);

    public Task AddPreferenceEventAsync(CommunicationPreferenceEvent preference, CancellationToken cancellationToken) =>
        dbContext.CommunicationPreferenceEvents.AddAsync(preference, cancellationToken).AsTask();

    public async Task<IReadOnlyList<CommunicationPreferenceEvent>> ListPreferenceEventsAsync(
        long stakeholderId,
        CancellationToken cancellationToken) =>
        await dbContext.CommunicationPreferenceEvents
            .AsNoTracking()
            .Where(preference => preference.StakeholderId == stakeholderId)
            .OrderByDescending(preference => preference.CreatedUtc)
            .ThenByDescending(preference => preference.Id)
            .ToListAsync(cancellationToken);

    public Task<MessageDeliveryAttempt?> FindDeliveryAttemptForCallbackAsync(
        string providerCode,
        string providerMessageId,
        CancellationToken cancellationToken) =>
        dbContext.MessageDeliveryAttempts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(attempt => attempt.ProviderCode == providerCode
                && attempt.ProviderMessageId == providerMessageId
                && attempt.Status == MessageDeliveryStatus.Accepted)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ProviderCallbackInbox?> FindCallbackAsync(
        string providerCode,
        string externalEventId,
        CancellationToken cancellationToken) =>
        dbContext.ProviderCallbackInboxes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(callback => callback.ProviderCode == providerCode
                && callback.ExternalEventId == externalEventId, cancellationToken);

    public Task AddCallbackAsync(ProviderCallbackInbox callback, CancellationToken cancellationToken) =>
        dbContext.ProviderCallbackInboxes.AddAsync(callback, cancellationToken).AsTask();

    public Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        dbContext.OutboxMessages.AddAsync(message, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ProviderCallbackInbox>> ListCallbacksAsync(
        long tenantId,
        long branchId,
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.ProviderCallbackInboxes
            .AsNoTracking()
            .Where(callback => callback.TenantId == tenantId && callback.BranchId == branchId)
            .OrderByDescending(callback => callback.CreatedUtc)
            .ThenByDescending(callback => callback.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MessageDeliveryAttempt>> ListDeliveryAttemptsAsync(
        long tenantId,
        long branchId,
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.MessageDeliveryAttempts
            .AsNoTracking()
            .Where(attempt => attempt.TenantId == tenantId && attempt.BranchId == branchId)
            .OrderByDescending(attempt => attempt.CreatedUtc)
            .ThenByDescending(attempt => attempt.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
}
