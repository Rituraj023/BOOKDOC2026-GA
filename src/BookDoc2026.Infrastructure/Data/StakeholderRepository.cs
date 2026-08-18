using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class StakeholderRepository(BookDocDbContext dbContext) : IStakeholderRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public async Task<StakeholderAggregate?> GetAsync(long stakeholderId, CancellationToken cancellationToken)
    {
        var stakeholder = await dbContext.Stakeholders.SingleOrDefaultAsync(
            candidate => candidate.Id == stakeholderId,
            cancellationToken);
        if (stakeholder is null)
        {
            return null;
        }

        var person = await dbContext.StakeholderPersons.SingleOrDefaultAsync(
            candidate => candidate.StakeholderId == stakeholderId,
            cancellationToken);
        var corporate = await dbContext.StakeholderCorporates.SingleOrDefaultAsync(
            candidate => candidate.StakeholderId == stakeholderId,
            cancellationToken);
        var contacts = await dbContext.StakeholderContactPoints
            .Where(contact => contact.StakeholderId == stakeholderId)
            .ToListAsync(cancellationToken);
        var identifiers = await dbContext.StakeholderIdentifiers
            .Where(identifier => identifier.StakeholderId == stakeholderId)
            .ToListAsync(cancellationToken);
        var addresses = await dbContext.StakeholderAddresses
            .Where(address => address.StakeholderId == stakeholderId)
            .ToListAsync(cancellationToken);
        var documents = await dbContext.StakeholderDocumentReferences
            .Where(document => document.StakeholderId == stakeholderId)
            .ToListAsync(cancellationToken);
        return new StakeholderAggregate(stakeholder, person, corporate, contacts, identifiers, addresses, documents);
    }

    public async Task AddAsync(StakeholderAggregate aggregate, CancellationToken cancellationToken)
    {
        await dbContext.Stakeholders.AddAsync(aggregate.Stakeholder, cancellationToken);
        if (aggregate.Person is not null)
        {
            await dbContext.StakeholderPersons.AddAsync(aggregate.Person, cancellationToken);
        }

        if (aggregate.Corporate is not null)
        {
            await dbContext.StakeholderCorporates.AddAsync(aggregate.Corporate, cancellationToken);
        }

        await dbContext.StakeholderContactPoints.AddRangeAsync(aggregate.Contacts, cancellationToken);
        await dbContext.StakeholderIdentifiers.AddRangeAsync(aggregate.Identifiers, cancellationToken);
        await dbContext.StakeholderAddresses.AddRangeAsync(aggregate.Addresses, cancellationToken);
        await dbContext.StakeholderDocumentReferences.AddRangeAsync(aggregate.Documents, cancellationToken);
    }

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
            throw new ConcurrencyConflictException("The stakeholder changed while the operation was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Stakeholder data conflicts with an existing identifier, contact or file.");
        }
    }
}
