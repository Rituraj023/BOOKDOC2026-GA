using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Stakeholders;

public sealed class StakeholderService(
    IStakeholderRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<StakeholderResponse> CreatePersonAsync(
        long branchId,
        CreatePersonStakeholderRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.StakeholdersManage, branchId, cancellationToken);
        RequireDocumentPermission(request.Documents.Count);
        var aggregate = StakeholderAggregateFactory.CreatePerson(
            branch.TenantId,
            request.Person,
            request.Contacts,
            request.Identifiers,
            request.Addresses,
            request.Documents,
            publicIds,
            clock.UtcNow);
        await SaveNewAsync(branchId, aggregate, cancellationToken);
        return StakeholderAggregateFactory.Map(aggregate, publicIds);
    }

    public async Task<StakeholderResponse> CreateCorporateAsync(
        long branchId,
        StakeholderCorporateRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.StakeholdersManage, branchId, cancellationToken);
        RequireDocumentPermission(request.Documents.Count);
        var aggregate = StakeholderAggregateFactory.CreateCorporate(branch.TenantId, request, publicIds, clock.UtcNow);
        await SaveNewAsync(branchId, aggregate, cancellationToken);
        return StakeholderAggregateFactory.Map(aggregate, publicIds);
    }

    public async Task<StakeholderResponse> GetAsync(
        long branchId,
        long stakeholderId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.StakeholdersView, branchId, cancellationToken);
        var aggregate = await repository.GetAsync(stakeholderId, cancellationToken)
            ?? throw new NotFoundException("Stakeholder was not found in the current tenant scope.");
        return StakeholderAggregateFactory.Map(aggregate, publicIds);
    }

    private async Task SaveNewAsync(
        long branchId,
        StakeholderAggregate aggregate,
        CancellationToken cancellationToken)
    {
        await repository.AddAsync(aggregate, cancellationToken);
        await repository.AddAuditEventAsync(AuditEvent.Record(
            aggregate.Stakeholder.TenantId,
            branchId,
            actor.ActorId,
            "Stakeholder.Created",
            nameof(Domain.Stakeholders.Stakeholder),
            aggregate.Stakeholder.Id,
            JsonSerializer.Serialize(new { aggregate.Stakeholder.Type, aggregate.Stakeholder.DisplayName }),
            correlationContext.CorrelationId,
            clock.UtcNow), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Branch> RequireBranchAsync(
        string permission,
        long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("The actor is not authorized for this stakeholder operation.");
        }

        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private void RequireDocumentPermission(int documentCount)
    {
        if (documentCount > 0 && !actor.HasPermission(FoundationPermissions.StakeholderDocumentsManage))
        {
            throw new ForbiddenException("Managing stakeholder document references requires the document permission.");
        }
    }
}
