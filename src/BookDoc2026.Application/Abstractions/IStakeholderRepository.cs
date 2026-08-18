using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Stakeholders;

namespace BookDoc2026.Application.Abstractions;

public sealed record StakeholderAggregate(
    Stakeholder Stakeholder,
    StakeholderPerson? Person,
    StakeholderCorporate? Corporate,
    IReadOnlyCollection<StakeholderContactPoint> Contacts,
    IReadOnlyCollection<StakeholderIdentifier> Identifiers,
    IReadOnlyCollection<StakeholderAddress> Addresses,
    IReadOnlyCollection<StakeholderDocumentReference> Documents);

public interface IStakeholderRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);

    Task<StakeholderAggregate?> GetAsync(long stakeholderId, CancellationToken cancellationToken);

    Task AddAsync(StakeholderAggregate aggregate, CancellationToken cancellationToken);

    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
