using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Stakeholders;

namespace BookDoc2026.Application.Abstractions;

public sealed record InvestigationOrderAggregate(
    InvestigationOrder Order,
    ClinicalService Service,
    QueueTicket? QueueTicket,
    IReadOnlyCollection<InvestigationOrderEvent> History);

public sealed record InvestigationServiceCatalogItem(
    ClinicalService Service,
    ImagingModality Modality);

public sealed record InvestigationOrderCreationResult(InvestigationOrderAggregate Aggregate, bool IsReplay);

public sealed record InvestigationQueueHandoffResult(InvestigationOrderAggregate Aggregate, bool IsReplay);

public sealed record InvestigationWorklistProjection(
    QueueTicket QueueTicket,
    InvestigationOrder Order,
    ClinicalService Service,
    Patient Patient,
    Stakeholder PatientStakeholder,
    ClinicalEncounter Encounter);

public interface IInvestigationRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<ClinicalEncounter?> GetEncounterAsync(long branchId, long encounterId, CancellationToken cancellationToken);
    Task<InvestigationServiceCatalogItem?> GetInvestigationServiceAsync(long serviceId,
        ImagingModality modality, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvestigationServiceCatalogItem>> ListInvestigationServicesAsync(
        CancellationToken cancellationToken);
    Task<ImagingServicePoint?> GetServicePointAsync(long branchId, long servicePointId,
        CancellationToken cancellationToken);
    Task<InvestigationOrderAggregate?> GetOrderAsync(long branchId, long encounterId, long orderId,
        bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvestigationOrderAggregate>> ListOrdersAsync(long branchId, long encounterId,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvestigationWorklistProjection>> ListWorklistAsync(long branchId,
        long servicePointId, int take, CancellationToken cancellationToken);
    Task<InvestigationOrderCreationResult> CreateOrderAsync(InvestigationOrder order,
        InvestigationOrderEvent orderEvent, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<InvestigationQueueHandoffResult> CreateQueueHandoffAsync(InvestigationOrder order,
        QueueTicket ticket, QueueTicketEvent queueEvent, InvestigationOrderEvent orderEvent,
        AuditEvent orderAudit, AuditEvent queueAudit, CancellationToken cancellationToken);
}
