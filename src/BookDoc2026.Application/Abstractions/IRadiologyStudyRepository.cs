using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Radiology;

namespace BookDoc2026.Application.Abstractions;

public sealed record RadiologyOrderContext(
    InvestigationOrder Order,
    QueueTicket QueueTicket,
    ImagingServicePoint ServicePoint);

public sealed record RadiologyStudyAggregate(
    RadiologyStudy Study,
    IReadOnlyCollection<RadiologyAcquisitionAttempt> AcquisitionAttempts,
    IReadOnlyCollection<RadiologyQualityReview> QualityReviews,
    IReadOnlyCollection<RadiologyStudyEvent> History);

public sealed record RadiologyStudyMutationResult(RadiologyStudyAggregate Aggregate, bool IsReplay);

public interface IRadiologyStudyRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<RadiologyOrderContext?> GetOrderContextAsync(long branchId, long orderId,
        CancellationToken cancellationToken);
    Task<BookableResource?> GetEligibleEquipmentAsync(long branchId, long resourceId, long serviceId,
        ImagingModality modality, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BookableResource>> ListEligibleEquipmentAsync(long branchId, long serviceId,
        ImagingModality modality, CancellationToken cancellationToken);
    Task<RadiologyStudyAggregate?> GetByIdAsync(long branchId, long studyId, bool tracked,
        CancellationToken cancellationToken);
    Task<RadiologyStudyAggregate?> GetByOrderAsync(long branchId, long orderId, bool tracked,
        CancellationToken cancellationToken);
    Task<RadiologyStudyEvent?> GetEventByRequestIdAsync(Guid requestId, CancellationToken cancellationToken);
    Task<RadiologyStudyMutationResult> RegisterAsync(RadiologyStudy study, RadiologyStudyEvent studyEvent,
        AuditEvent audit, CancellationToken cancellationToken);
    Task<RadiologyStudyMutationResult> StartAsync(RadiologyStudy study, RadiologyStudyEvent studyEvent,
        AuditEvent audit, CancellationToken cancellationToken);
    Task<RadiologyStudyMutationResult> RecordAcquisitionAsync(RadiologyStudy study,
        RadiologyAcquisitionAttempt attempt, RadiologyStudyEvent studyEvent, AuditEvent audit,
        CancellationToken cancellationToken);
    Task<RadiologyStudyMutationResult> ReviewQualityAsync(RadiologyStudy study,
        RadiologyQualityReview review, RadiologyStudyEvent studyEvent, AuditEvent audit,
        CancellationToken cancellationToken);
}
