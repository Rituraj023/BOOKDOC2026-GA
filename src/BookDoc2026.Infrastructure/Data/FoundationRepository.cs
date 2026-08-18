using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class FoundationRepository(BookDocDbContext dbContext) : IFoundationRepository
{
    public Task<bool> TenantSlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Slug == slug, cancellationToken);

    public Task AddTenantApplicationAsync(TenantApplication application, CancellationToken cancellationToken) =>
        dbContext.TenantApplications.AddAsync(application, cancellationToken).AsTask();

    public Task<TenantApplication?> GetTenantApplicationAsync(long id, CancellationToken cancellationToken) =>
        dbContext.TenantApplications.SingleOrDefaultAsync(application => application.Id == id, cancellationToken);

    public Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken) =>
        dbContext.Tenants.AddAsync(tenant, cancellationToken).AsTask();

    public Task AddOrganizationAsync(Organization organization, CancellationToken cancellationToken) =>
        dbContext.Organizations.AddAsync(organization, cancellationToken).AsTask();

    public Task AddBranchAsync(Branch branch, CancellationToken cancellationToken) =>
        dbContext.Branches.AddAsync(branch, cancellationToken).AsTask();

    public Task AddBranchConfigurationAsync(BranchConfiguration configuration, CancellationToken cancellationToken) =>
        dbContext.BranchConfigurations.AddAsync(configuration, cancellationToken).AsTask();

    public async Task<(Branch Branch, BranchConfiguration Configuration)?> GetBranchConfigurationAsync(
        long branchId,
        CancellationToken cancellationToken)
    {
        var branch = await dbContext.Branches.SingleOrDefaultAsync(candidate => candidate.Id == branchId, cancellationToken);
        if (branch is null)
        {
            return null;
        }

        var configuration = await dbContext.BranchConfigurations
            .SingleAsync(candidate => candidate.BranchId == branchId, cancellationToken);
        return (branch, configuration);
    }

    public Task<bool> NumberPrefixInUseAsync(
        long tenantId,
        long excludedBranchId,
        string invoicePrefix,
        string receiptPrefix,
        CancellationToken cancellationToken) =>
        dbContext.BranchConfigurations.AnyAsync(
            configuration => configuration.TenantId == tenantId
                && configuration.BranchId != excludedBranchId
                && (configuration.InvoicePrefix == invoicePrefix || configuration.ReceiptPrefix == receiptPrefix),
            cancellationToken);

    public Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();

    public Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        dbContext.OutboxMessages.AddAsync(message, cancellationToken).AsTask();

    public Task AddMessageTemplateAsync(MessageTemplate template, CancellationToken cancellationToken) =>
        dbContext.MessageTemplates.AddAsync(template, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The record changed while the operation was being saved.");
        }
    }
}
