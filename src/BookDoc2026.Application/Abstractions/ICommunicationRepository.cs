using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Abstractions;

public interface ICommunicationRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageTemplate>> ListTemplatesAsync(
        long tenantId,
        long organizationId,
        long branchId,
        CancellationToken cancellationToken);

    Task<MessageTemplate?> GetTemplateAsync(long templateId, CancellationToken cancellationToken);

    Task<int> GetNextBranchTemplateVersionAsync(
        long tenantId,
        long organizationId,
        long branchId,
        string key,
        CommunicationChannel channel,
        string culture,
        CancellationToken cancellationToken);

    Task AddTemplateAsync(MessageTemplate template, CancellationToken cancellationToken);

    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> StakeholderExistsAsync(long stakeholderId, CancellationToken cancellationToken);

    Task AddPreferenceEventAsync(CommunicationPreferenceEvent preference, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommunicationPreferenceEvent>> ListPreferenceEventsAsync(
        long stakeholderId,
        CancellationToken cancellationToken);

    Task<MessageDeliveryAttempt?> FindDeliveryAttemptForCallbackAsync(
        string providerCode,
        string providerMessageId,
        CancellationToken cancellationToken);

    Task<ProviderCallbackInbox?> FindCallbackAsync(
        string providerCode,
        string externalEventId,
        CancellationToken cancellationToken);

    Task AddCallbackAsync(ProviderCallbackInbox callback, CancellationToken cancellationToken);

    Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProviderCallbackInbox>> ListCallbacksAsync(
        long tenantId,
        long branchId,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageDeliveryAttempt>> ListDeliveryAttemptsAsync(
        long tenantId,
        long branchId,
        int take,
        CancellationToken cancellationToken);
}
