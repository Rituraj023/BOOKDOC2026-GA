using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class CatalogRepository(BookDocDbContext dbContext) : ICatalogRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public Task<bool> ServiceCodeExistsAsync(string code, CancellationToken cancellationToken) =>
        dbContext.ClinicalServices.AnyAsync(service => service.Code == code, cancellationToken);

    public Task AddServiceAsync(ClinicalService service, CancellationToken cancellationToken) =>
        dbContext.ClinicalServices.AddAsync(service, cancellationToken).AsTask();

    public Task<ClinicalService?> GetServiceAsync(long serviceId, CancellationToken cancellationToken) =>
        dbContext.ClinicalServices.SingleOrDefaultAsync(service => service.Id == serviceId, cancellationToken);

    public async Task<IReadOnlyCollection<ClinicalService>> ListServicesAsync(
        bool includeInactive,
        CancellationToken cancellationToken) =>
        await dbContext.ClinicalServices
            .Where(service => includeInactive || service.Status == CatalogItemStatus.Active)
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Code)
            .ToListAsync(cancellationToken);

    public Task<bool> CategoryCodeExistsAsync(string code, CancellationToken cancellationToken) =>
        dbContext.ResourceCategories.AnyAsync(category => category.Code == code, cancellationToken);

    public Task AddCategoryAsync(ResourceCategory category, CancellationToken cancellationToken) =>
        dbContext.ResourceCategories.AddAsync(category, cancellationToken).AsTask();

    public Task<ResourceCategory?> GetCategoryAsync(long categoryId, CancellationToken cancellationToken) =>
        dbContext.ResourceCategories.SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);

    public async Task<IReadOnlyCollection<ResourceCategory>> ListCategoriesAsync(
        bool includeInactive,
        CancellationToken cancellationToken) =>
        await dbContext.ResourceCategories
            .Where(category => includeInactive || category.IsActive)
            .OrderBy(category => category.Kind)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> CategoryInUseAsync(long categoryId, CancellationToken cancellationToken) =>
        await dbContext.BookableResources.AnyAsync(
            resource => resource.CategoryId == categoryId && resource.IsActive,
            cancellationToken)
        || await dbContext.ServiceResourceRequirements.AnyAsync(
            requirement => requirement.CategoryId == categoryId,
            cancellationToken);

    public Task<bool> ResourceCodeExistsAsync(
        long branchId,
        string code,
        CancellationToken cancellationToken) =>
        dbContext.BookableResources.AnyAsync(
            resource => resource.BranchId == branchId && resource.Code == code,
            cancellationToken);

    public Task AddResourceAsync(BookableResource resource, CancellationToken cancellationToken) =>
        dbContext.BookableResources.AddAsync(resource, cancellationToken).AsTask();

    public async Task<BookableResourceAggregate?> GetResourceAsync(
        long resourceId,
        CancellationToken cancellationToken)
    {
        var resource = await dbContext.BookableResources.SingleOrDefaultAsync(
            candidate => candidate.Id == resourceId,
            cancellationToken);
        if (resource is null)
        {
            return null;
        }

        var category = await dbContext.ResourceCategories.SingleAsync(
            candidate => candidate.Id == resource.CategoryId,
            cancellationToken);
        return new BookableResourceAggregate(resource, category);
    }

    public async Task<IReadOnlyCollection<BookableResourceAggregate>> ListResourcesAsync(
        long branchId,
        long? categoryId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var resources = await dbContext.BookableResources
            .Where(resource => resource.BranchId == branchId
                && (!categoryId.HasValue || resource.CategoryId == categoryId.Value)
                && (includeInactive || resource.IsActive))
            .OrderBy(resource => resource.Name)
            .ThenBy(resource => resource.Code)
            .ToListAsync(cancellationToken);
        if (resources.Count == 0)
        {
            return [];
        }

        var categoryIds = resources.Select(resource => resource.CategoryId).Distinct().ToArray();
        var categories = await dbContext.ResourceCategories
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, cancellationToken);
        return resources.Select(resource => new BookableResourceAggregate(resource, categories[resource.CategoryId])).ToArray();
    }

    public async Task<int> GetMaximumCapabilityCapacityAsync(
        long resourceId,
        CancellationToken cancellationToken) =>
        await dbContext.ResourceCapabilities
            .Where(capability => capability.ResourceId == resourceId && capability.IsActive)
            .MaxAsync(capability => (int?)capability.CapacityRequired, cancellationToken)
        ?? 0;

    public Task<bool> CapabilityExistsAsync(
        long resourceId,
        long serviceId,
        CancellationToken cancellationToken) =>
        dbContext.ResourceCapabilities.AnyAsync(
            capability => capability.ResourceId == resourceId && capability.ServiceId == serviceId,
            cancellationToken);

    public Task AddCapabilityAsync(ResourceCapability capability, CancellationToken cancellationToken) =>
        dbContext.ResourceCapabilities.AddAsync(capability, cancellationToken).AsTask();

    public Task<bool> RequirementExistsAsync(
        long serviceId,
        long categoryId,
        string roleCode,
        CancellationToken cancellationToken) =>
        dbContext.ServiceResourceRequirements.AnyAsync(
            requirement => requirement.ServiceId == serviceId
                && requirement.CategoryId == categoryId
                && requirement.RoleCode == roleCode,
            cancellationToken);

    public Task AddRequirementAsync(
        ServiceResourceRequirement requirement,
        CancellationToken cancellationToken) =>
        dbContext.ServiceResourceRequirements.AddAsync(requirement, cancellationToken).AsTask();

    public Task AddStatusEventAsync(ResourceStatusEvent statusEvent, CancellationToken cancellationToken) =>
        dbContext.ResourceStatusEvents.AddAsync(statusEvent, cancellationToken).AsTask();

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
            throw new ConcurrencyConflictException("The catalog record changed while the operation was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Catalog data conflicts with an existing code or relationship.");
        }
    }
}
