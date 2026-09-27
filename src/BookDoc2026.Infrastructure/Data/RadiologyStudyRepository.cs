using System.Data;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Radiology;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class RadiologyStudyRepository(BookDocDbContext db) : IRadiologyStudyRepository
{
    private static readonly SemaphoreSlim InMemoryMutationGate = new(1, 1);

    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        db.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public async Task<RadiologyOrderContext?> GetOrderContextAsync(long branchId, long orderId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from order in db.InvestigationOrders.AsNoTracking()
            where order.BranchId == branchId && order.Id == orderId
            join ticket in db.QueueTickets.AsNoTracking() on order.Id equals ticket.InvestigationOrderId
            join point in db.ImagingServicePoints.AsNoTracking() on ticket.ServicePointId equals point.Id
            where ticket.BranchId == branchId && point.BranchId == branchId
                && ticket.PatientId == order.PatientId && point.Modality == order.Modality
            select new { order, ticket, point }).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new(row.order, row.ticket, row.point);
    }

    public Task<BookableResource?> GetEligibleEquipmentAsync(long branchId, long resourceId, long serviceId,
        ImagingModality modality, CancellationToken cancellationToken)
    {
        var expectedCategoryCode = modality == ImagingModality.XRay ? "XRAY" : "CT";
        return db.BookableResources.AsNoTracking().SingleOrDefaultAsync(resource =>
            resource.Id == resourceId && resource.BranchId == branchId && resource.IsActive
            && resource.OperationalStatus == ResourceOperationalStatus.Available
            && db.ResourceCategories.Any(category => category.Id == resource.CategoryId && category.IsActive
                && category.Kind == ResourceKind.ImagingModality
                && category.Code.Replace("-", "").Replace("_", "") == expectedCategoryCode)
            && db.ResourceCapabilities.Any(capability => capability.ResourceId == resource.Id
                && capability.ServiceId == serviceId && capability.IsActive), cancellationToken);
    }

    public async Task<IReadOnlyCollection<BookableResource>> ListEligibleEquipmentAsync(long branchId,
        long serviceId, ImagingModality modality, CancellationToken cancellationToken)
    {
        var expectedCategoryCode = modality == ImagingModality.XRay ? "XRAY" : "CT";
        return await db.BookableResources.AsNoTracking()
            .Where(resource => resource.BranchId == branchId && resource.IsActive
                && resource.OperationalStatus == ResourceOperationalStatus.Available
                && db.ResourceCategories.Any(category => category.Id == resource.CategoryId && category.IsActive
                    && category.Kind == ResourceKind.ImagingModality
                    && category.Code.Replace("-", "").Replace("_", "") == expectedCategoryCode)
                && db.ResourceCapabilities.Any(capability => capability.ResourceId == resource.Id
                    && capability.ServiceId == serviceId && capability.IsActive))
            .OrderBy(resource => resource.Code)
            .ToArrayAsync(cancellationToken);
    }

    public Task<RadiologyStudyAggregate?> GetByIdAsync(long branchId, long studyId, bool tracked,
        CancellationToken cancellationToken) => LoadAsync(branchId, studyId, null, tracked, cancellationToken);

    public Task<RadiologyStudyAggregate?> GetByOrderAsync(long branchId, long orderId, bool tracked,
        CancellationToken cancellationToken) => LoadAsync(branchId, null, orderId, tracked, cancellationToken);

    public Task<RadiologyStudyEvent?> GetEventByRequestIdAsync(Guid requestId,
        CancellationToken cancellationToken) => db.RadiologyStudyEvents.AsNoTracking()
        .SingleOrDefaultAsync(item => item.RequestId == requestId, cancellationToken);

    public Task<RadiologyStudyMutationResult> RegisterAsync(RadiologyStudy study,
        RadiologyStudyEvent studyEvent, AuditEvent audit, CancellationToken cancellationToken) =>
        MutateAsync(study.TenantId, study.OrderId, () => RegisterCoreAsync(study, studyEvent, audit,
            cancellationToken), cancellationToken);

    public Task<RadiologyStudyMutationResult> StartAsync(RadiologyStudy study,
        RadiologyStudyEvent studyEvent, AuditEvent audit, CancellationToken cancellationToken) =>
        MutateAsync(study.TenantId, study.Id, () => AddMutationCoreAsync(study, studyEvent, audit,
            null, null, cancellationToken), cancellationToken);

    public Task<RadiologyStudyMutationResult> RecordAcquisitionAsync(RadiologyStudy study,
        RadiologyAcquisitionAttempt attempt, RadiologyStudyEvent studyEvent, AuditEvent audit,
        CancellationToken cancellationToken) => MutateAsync(study.TenantId, study.Id,
        () => AddMutationCoreAsync(study, studyEvent, audit, attempt, null, cancellationToken),
        cancellationToken);

    public Task<RadiologyStudyMutationResult> ReviewQualityAsync(RadiologyStudy study,
        RadiologyQualityReview review, RadiologyStudyEvent studyEvent, AuditEvent audit,
        CancellationToken cancellationToken) => MutateAsync(study.TenantId, study.Id,
        () => AddMutationCoreAsync(study, studyEvent, audit, null, review, cancellationToken),
        cancellationToken);

    private async Task<RadiologyStudyMutationResult> RegisterCoreAsync(RadiologyStudy study,
        RadiologyStudyEvent studyEvent, AuditEvent audit, CancellationToken cancellationToken)
    {
        var replay = await FindReplayCoreAsync(studyEvent, study.OrderId, cancellationToken);
        if (replay is not null) return replay;
        if (await db.RadiologyStudies.AnyAsync(item => item.OrderId == study.OrderId, cancellationToken))
            throw new DomainRuleException("This investigation order already has a radiology study.");
        await db.RadiologyStudies.AddAsync(study, cancellationToken);
        await db.RadiologyStudyEvents.AddAsync(studyEvent, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new((await LoadAsync(study.BranchId, study.Id, null, false, cancellationToken))!, false);
    }

    private async Task<RadiologyStudyMutationResult> AddMutationCoreAsync(RadiologyStudy study,
        RadiologyStudyEvent studyEvent, AuditEvent audit, RadiologyAcquisitionAttempt? attempt,
        RadiologyQualityReview? review, CancellationToken cancellationToken)
    {
        var replay = await FindReplayCoreAsync(studyEvent, null, cancellationToken);
        if (replay is not null) return replay;
        if (attempt is not null) await db.RadiologyAcquisitionAttempts.AddAsync(attempt, cancellationToken);
        if (review is not null) await db.RadiologyQualityReviews.AddAsync(review, cancellationToken);
        await db.RadiologyStudyEvents.AddAsync(studyEvent, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new((await LoadAsync(study.BranchId, study.Id, null, false, cancellationToken))!, false);
    }

    private async Task<RadiologyStudyMutationResult?> FindReplayCoreAsync(RadiologyStudyEvent requested,
        long? expectedOrderId, CancellationToken cancellationToken)
    {
        var prior = await db.RadiologyStudyEvents.AsNoTracking()
            .SingleOrDefaultAsync(item => item.RequestId == requested.RequestId, cancellationToken);
        if (prior is null) return null;
        if (prior.StudyId != requested.StudyId || prior.Action != requested.Action
            || prior.RequestFingerprint != requested.RequestFingerprint)
            throw new DomainRuleException("The radiology request identifier was reused with different content.");
        var aggregate = await LoadAsync(requested.BranchId, prior.StudyId, null, false, cancellationToken)
            ?? throw new NotFoundException("Radiology study was not found.");
        if (expectedOrderId.HasValue && aggregate.Study.OrderId != expectedOrderId.Value)
            throw new DomainRuleException("The radiology request identifier belongs to another order.");
        return new(aggregate, true);
    }

    private async Task<RadiologyStudyAggregate?> LoadAsync(long branchId, long? studyId, long? orderId,
        bool tracked, CancellationToken cancellationToken)
    {
        var studies = db.RadiologyStudies.Where(item => item.BranchId == branchId
            && (!studyId.HasValue || item.Id == studyId.Value)
            && (!orderId.HasValue || item.OrderId == orderId.Value));
        var study = await (tracked ? studies : studies.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (study is null) return null;
        var attempts = await db.RadiologyAcquisitionAttempts.AsNoTracking()
            .Where(item => item.StudyId == study.Id).OrderBy(item => item.Sequence)
            .ToArrayAsync(cancellationToken);
        var reviews = await db.RadiologyQualityReviews.AsNoTracking()
            .Where(item => item.StudyId == study.Id).OrderBy(item => item.CreatedUtc)
            .ToArrayAsync(cancellationToken);
        var history = await db.RadiologyStudyEvents.AsNoTracking()
            .Where(item => item.StudyId == study.Id).OrderBy(item => item.StudyVersion)
            .ToArrayAsync(cancellationToken);
        return new(study, attempts, reviews, history);
    }

    private async Task<T> MutateAsync<T>(long tenantId, long studyId, Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            await InMemoryMutationGate.WaitAsync(cancellationToken);
            try { return await operation(); }
            finally { InMemoryMutationGate.Release(); }
        }

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            var resource = $"BOOKDOC:RADIOLOGY:STUDY:{tenantId}:{studyId}";
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
                cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Radiology study data changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Radiology study data conflicts with an existing request or record.");
        }
    }
}
