using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Abstractions;

public sealed record BookableResourceAggregate(BookableResource Resource, ResourceCategory Category);

public interface ICatalogRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);

    Task<bool> ServiceCodeExistsAsync(string code, CancellationToken cancellationToken);

    Task AddServiceAsync(ClinicalService service, CancellationToken cancellationToken);

    Task<ClinicalService?> GetServiceAsync(long serviceId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ClinicalService>> ListServicesAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<bool> CategoryCodeExistsAsync(string code, CancellationToken cancellationToken);

    Task AddCategoryAsync(ResourceCategory category, CancellationToken cancellationToken);

    Task<ResourceCategory?> GetCategoryAsync(long categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ResourceCategory>> ListCategoriesAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<bool> CategoryInUseAsync(long categoryId, CancellationToken cancellationToken);

    Task<bool> ResourceCodeExistsAsync(long branchId, string code, CancellationToken cancellationToken);

    Task AddResourceAsync(BookableResource resource, CancellationToken cancellationToken);

    Task<BookableResourceAggregate?> GetResourceAsync(long resourceId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BookableResourceAggregate>> ListResourcesAsync(
        long branchId,
        long? categoryId,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<int> GetMaximumCapabilityCapacityAsync(long resourceId, CancellationToken cancellationToken);

    Task<bool> CapabilityExistsAsync(long resourceId, long serviceId, CancellationToken cancellationToken);

    Task AddCapabilityAsync(ResourceCapability capability, CancellationToken cancellationToken);

    Task<bool> RequirementExistsAsync(
        long serviceId,
        long categoryId,
        string roleCode,
        CancellationToken cancellationToken);

    Task AddRequirementAsync(ServiceResourceRequirement requirement, CancellationToken cancellationToken);

    Task AddStatusEventAsync(ResourceStatusEvent statusEvent, CancellationToken cancellationToken);

    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
