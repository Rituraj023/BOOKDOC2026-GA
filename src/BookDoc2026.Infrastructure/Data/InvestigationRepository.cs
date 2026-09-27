using System.Data;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class InvestigationRepository(BookDocDbContext dbContext) : IInvestigationRepository
{
    private static readonly SemaphoreSlim InMemoryMutationGate = new(1, 1);

    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<ClinicalEncounter?> GetEncounterAsync(long branchId, long encounterId,
        CancellationToken cancellationToken) => dbContext.ClinicalEncounters.AsNoTracking()
        .SingleOrDefaultAsync(item => item.BranchId == branchId && item.Id == encounterId, cancellationToken);

    public async Task<InvestigationServiceCatalogItem?> GetInvestigationServiceAsync(
        long serviceId, ImagingModality modality, CancellationToken cancellationToken) =>
        (await ListInvestigationServicesCoreAsync(serviceId, cancellationToken))
            .SingleOrDefault(item => item.Modality == modality);

    public Task<IReadOnlyCollection<InvestigationServiceCatalogItem>> ListInvestigationServicesAsync(
        CancellationToken cancellationToken) => ListInvestigationServicesCoreAsync(null, cancellationToken);

    public Task<ImagingServicePoint?> GetServicePointAsync(long branchId, long servicePointId,
        CancellationToken cancellationToken) => dbContext.ImagingServicePoints.AsNoTracking()
        .SingleOrDefaultAsync(item => item.BranchId == branchId && item.Id == servicePointId, cancellationToken);

    public Task<InvestigationOrderAggregate?> GetOrderAsync(long branchId, long encounterId, long orderId,
        bool tracked, CancellationToken cancellationToken) =>
        LoadAggregateAsync(branchId, encounterId, orderId, tracked, cancellationToken);

    public async Task<IReadOnlyCollection<InvestigationOrderAggregate>> ListOrdersAsync(
        long branchId, long encounterId, CancellationToken cancellationToken)
    {
        var ids = await dbContext.InvestigationOrders.AsNoTracking()
            .Where(item => item.BranchId == branchId && item.EncounterId == encounterId)
            .OrderByDescending(item => item.OrderedUtc).ThenByDescending(item => item.Id)
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var items = new List<InvestigationOrderAggregate>(ids.Length);
        foreach (var id in ids)
        {
            var aggregate = await LoadAggregateAsync(branchId, encounterId, id, false, cancellationToken);
            if (aggregate is not null) items.Add(aggregate);
        }
        return items;
    }

    public async Task<IReadOnlyCollection<InvestigationWorklistProjection>> ListWorklistAsync(
        long branchId, long servicePointId, int take, CancellationToken cancellationToken)
    {
        var rows = await (
            from ticket in dbContext.QueueTickets.AsNoTracking()
            where ticket.BranchId == branchId
                && ticket.ServicePointId == servicePointId
                && ticket.InvestigationOrderId.HasValue
                && ticket.Status != QueueTicketStatus.Completed
                && ticket.Status != QueueTicketStatus.Cancelled
            join order in dbContext.InvestigationOrders.AsNoTracking()
                on ticket.InvestigationOrderId equals (long?)order.Id
            join service in dbContext.ClinicalServices.AsNoTracking()
                on order.RequestedServiceId equals service.Id
            join patient in dbContext.Patients.AsNoTracking()
                on order.PatientId equals patient.Id
            join stakeholder in dbContext.Stakeholders.AsNoTracking()
                on new { patient.TenantId, Id = patient.StakeholderId }
                equals new { stakeholder.TenantId, stakeholder.Id }
            join encounter in dbContext.ClinicalEncounters.AsNoTracking()
                on order.EncounterId equals encounter.Id
            where order.BranchId == branchId
                && order.PatientId == ticket.PatientId
                && encounter.BranchId == branchId
                && encounter.PatientId == order.PatientId
                && service.TenantId == order.TenantId
            orderby ticket.Priority descending, ticket.ArrivedUtc, ticket.Id
            select new { ticket, order, service, patient, stakeholder, encounter })
            .Take(take)
            .ToArrayAsync(cancellationToken);

        return rows.Select(item => new InvestigationWorklistProjection(
            item.ticket, item.order, item.service, item.patient, item.stakeholder, item.encounter)).ToArray();
    }

    public Task<InvestigationOrderCreationResult> CreateOrderAsync(
        InvestigationOrder order, InvestigationOrderEvent orderEvent, AuditEvent auditEvent,
        CancellationToken cancellationToken) => MutateAsync(
        $"BOOKDOC:INVESTIGATION:REQUEST:{order.TenantId}:{order.RequestId}",
        () => CreateOrderCoreAsync(order, orderEvent, auditEvent, cancellationToken), cancellationToken);

    public Task<InvestigationQueueHandoffResult> CreateQueueHandoffAsync(
        InvestigationOrder order, QueueTicket ticket, QueueTicketEvent queueEvent,
        InvestigationOrderEvent orderEvent, AuditEvent orderAudit, AuditEvent queueAudit,
        CancellationToken cancellationToken) => MutateAsync(
        $"BOOKDOC:INVESTIGATION:QUEUE:{order.TenantId}:{order.Id}",
        () => CreateQueueHandoffCoreAsync(order, ticket, queueEvent, orderEvent, orderAudit, queueAudit,
            cancellationToken), cancellationToken);

    private async Task<InvestigationOrderCreationResult> CreateOrderCoreAsync(
        InvestigationOrder order, InvestigationOrderEvent orderEvent, AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.InvestigationOrders.SingleOrDefaultAsync(
            item => item.RequestId == order.RequestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.BranchId != order.BranchId || existing.EncounterId != order.EncounterId
                || existing.PatientId != order.PatientId || existing.RequestedServiceId != order.RequestedServiceId
                || existing.Modality != order.Modality || existing.ClinicalIndication != order.ClinicalIndication)
                throw new DomainRuleException(
                    "The investigation request identifier was reused with different content.");
            return new((await LoadAggregateAsync(existing.BranchId, existing.EncounterId, existing.Id,
                false, cancellationToken))!, true);
        }

        await dbContext.InvestigationOrders.AddAsync(order, cancellationToken);
        await dbContext.InvestigationOrderEvents.AddAsync(orderEvent, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new((await LoadAggregateAsync(order.BranchId, order.EncounterId, order.Id,
            false, cancellationToken))!, false);
    }

    private async Task<InvestigationQueueHandoffResult> CreateQueueHandoffCoreAsync(
        InvestigationOrder order, QueueTicket ticket, QueueTicketEvent queueEvent,
        InvestigationOrderEvent orderEvent, AuditEvent orderAudit, AuditEvent queueAudit,
        CancellationToken cancellationToken)
    {
        var replay = await dbContext.QueueTickets.AsNoTracking().SingleOrDefaultAsync(
            item => item.RequestId == ticket.RequestId, cancellationToken);
        if (replay is not null)
        {
            ValidateQueueReplay(replay, ticket);
            return new((await LoadAggregateAsync(order.BranchId, order.EncounterId, order.Id,
                false, cancellationToken))!, true);
        }

        if (await dbContext.QueueTickets.AnyAsync(
                item => item.InvestigationOrderId == order.Id, cancellationToken))
            throw new DomainRuleException("This investigation order already has a queue handoff.");

        await dbContext.QueueTickets.AddAsync(ticket, cancellationToken);
        await dbContext.QueueTicketEvents.AddAsync(queueEvent, cancellationToken);
        await dbContext.InvestigationOrderEvents.AddAsync(orderEvent, cancellationToken);
        await dbContext.AuditEvents.AddRangeAsync([orderAudit, queueAudit], cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new((await LoadAggregateAsync(order.BranchId, order.EncounterId, order.Id,
            false, cancellationToken))!, false);
    }

    private static void ValidateQueueReplay(QueueTicket existing, QueueTicket requested)
    {
        if (existing.BranchId != requested.BranchId || existing.ServicePointId != requested.ServicePointId
            || existing.PatientId != requested.PatientId
            || existing.InvestigationOrderId != requested.InvestigationOrderId
            || existing.Priority != requested.Priority)
            throw new DomainRuleException("The queue handoff request identifier was reused with different content.");
    }

    private async Task<InvestigationOrderAggregate?> LoadAggregateAsync(
        long branchId, long encounterId, long orderId, bool tracked, CancellationToken cancellationToken)
    {
        var orders = dbContext.InvestigationOrders.Where(item => item.BranchId == branchId
            && item.EncounterId == encounterId && item.Id == orderId);
        var order = await (tracked ? orders : orders.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (order is null) return null;
        var service = await dbContext.ClinicalServices.AsNoTracking()
            .SingleAsync(item => item.Id == order.RequestedServiceId, cancellationToken);
        var ticket = await dbContext.QueueTickets.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InvestigationOrderId == order.Id, cancellationToken);
        var history = await dbContext.InvestigationOrderEvents.AsNoTracking()
            .Where(item => item.OrderId == order.Id)
            .OrderBy(item => item.OrderVersion).ThenBy(item => item.CreatedUtc)
            .ToArrayAsync(cancellationToken);
        return new(order, service, ticket, history);
    }

    private async Task<IReadOnlyCollection<InvestigationServiceCatalogItem>> ListInvestigationServicesCoreAsync(
        long? serviceId, CancellationToken cancellationToken)
    {
        var configured = await dbContext.ServiceResourceRequirements.AsNoTracking()
            .Where(requirement => !requirement.IsOptional
                && (!serviceId.HasValue || requirement.ServiceId == serviceId.Value))
            .Join(dbContext.ResourceCategories.AsNoTracking()
                    .Where(category => category.IsActive && category.Kind == ResourceKind.ImagingModality),
                requirement => requirement.CategoryId,
                category => category.Id,
                (requirement, category) => new { requirement.ServiceId, category.Code })
            .Join(dbContext.ClinicalServices.AsNoTracking()
                    .Where(service => service.Status == CatalogItemStatus.Active),
                configuredItem => configuredItem.ServiceId,
                service => service.Id,
                (configuredItem, service) => new { Service = service, configuredItem.Code })
            .ToArrayAsync(cancellationToken);

        return configured
            .Select(item => new { item.Service, Modality = ParseModalityCategory(item.Code) })
            .Where(item => item.Modality.HasValue)
            .GroupBy(item => new { item.Service.Id, Modality = item.Modality!.Value })
            .Select(group => new InvestigationServiceCatalogItem(group.First().Service, group.Key.Modality))
            .OrderBy(item => item.Service.Name)
            .ThenBy(item => item.Modality)
            .ToArray();
    }

    private static ImagingModality? ParseModalityCategory(string code) =>
        code.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal) switch
        {
            "XRAY" => ImagingModality.XRay,
            "CT" => ImagingModality.CT,
            _ => null
        };

    private async Task<T> MutateAsync<T>(string lockResource, Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await InMemoryMutationGate.WaitAsync(cancellationToken);
            try { return await operation(); }
            finally { InMemoryMutationGate.Release(); }
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource={lockResource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
                cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Investigation data changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Investigation data conflicts with an existing request or queue handoff.");
        }
    }
}
