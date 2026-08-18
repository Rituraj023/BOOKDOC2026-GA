using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Communications;

namespace BookDoc2026.Application.Abstractions;

public interface IFoundationRepository
{
    Task<bool> TenantSlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task AddTenantApplicationAsync(TenantApplication application, CancellationToken cancellationToken);

    Task<TenantApplication?> GetTenantApplicationAsync(long id, CancellationToken cancellationToken);

    Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken);

    Task AddOrganizationAsync(Organization organization, CancellationToken cancellationToken);

    Task AddBranchAsync(Branch branch, CancellationToken cancellationToken);

    Task AddBranchConfigurationAsync(BranchConfiguration configuration, CancellationToken cancellationToken);

    Task<(Branch Branch, BranchConfiguration Configuration)?> GetBranchConfigurationAsync(
        long branchId,
        CancellationToken cancellationToken);

    Task<bool> NumberPrefixInUseAsync(
        long tenantId,
        long excludedBranchId,
        string invoicePrefix,
        string receiptPrefix,
        CancellationToken cancellationToken);

    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken);

    Task AddMessageTemplateAsync(MessageTemplate template, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
