using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Catalog;

public sealed class CatalogService(
    ICatalogRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<ServiceResponse> CreateServiceAsync(
        long branchId,
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.CatalogManage, branchId, cancellationToken);
        var code = NormalizeCode(request.Code);
        if (await repository.ServiceCodeExistsAsync(code, cancellationToken))
        {
            throw new DomainRuleException("A service with this code already exists in the tenant.");
        }

        var service = ClinicalService.Create(
            branch.TenantId,
            code,
            request.Name,
            request.Description,
            request.DefaultDurationMinutes,
            clock.UtcNow);
        await repository.AddServiceAsync(service, cancellationToken);
        await AuditAsync(branchId, "CatalogService.Created", nameof(ClinicalService), service.Id,
            new { service.Code, service.DefaultDurationMinutes }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(service);
    }

    public async Task<IReadOnlyCollection<ServiceResponse>> ListServicesAsync(
        long branchId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.CatalogView, branchId, cancellationToken);
        var services = await repository.ListServicesAsync(includeInactive, cancellationToken);
        return services.Select(Map).ToArray();
    }

    public async Task<ServiceResponse> UpdateServiceAsync(
        long branchId,
        long serviceId,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.CatalogManage, branchId, cancellationToken);
        var service = await repository.GetServiceAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException("Service was not found in the current tenant scope.");
        var oldVersion = service.Version;
        service.Update(
            request.ExpectedVersion,
            request.Name,
            request.Description,
            request.DefaultDurationMinutes,
            request.IsActive,
            clock.UtcNow);
        await AuditAsync(branchId, "CatalogService.Updated", nameof(ClinicalService), service.Id,
            new { OldVersion = oldVersion, NewVersion = service.Version, service.Status }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(service);
    }

    public async Task<ResourceCategoryResponse> CreateCategoryAsync(
        long branchId,
        CreateResourceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.CatalogManage, branchId, cancellationToken);
        var code = NormalizeCode(request.Code);
        if (await repository.CategoryCodeExistsAsync(code, cancellationToken))
        {
            throw new DomainRuleException("A resource category with this code already exists in the tenant.");
        }

        var parentCategoryId = publicIds.DecodeOptional(PublicIdKind.ResourceCategory, request.ParentCategoryId, branch.TenantId);
        var parent = parentCategoryId.HasValue
            ? await repository.GetCategoryAsync(parentCategoryId.Value, cancellationToken)
            : null;
        if (parentCategoryId.HasValue && parent is null)
        {
            throw new NotFoundException("Parent resource category was not found in the current tenant scope.");
        }

        if (parent is not null && !parent.IsActive)
        {
            throw new DomainRuleException("An inactive category cannot be used as a parent.");
        }

        var category = ResourceCategory.Create(
            branch.TenantId,
            parentCategoryId,
            code,
            request.Name,
            ParseEnum<ResourceKind>(request.Kind, "Resource kind"),
            clock.UtcNow);
        await repository.AddCategoryAsync(category, cancellationToken);
        await AuditAsync(branchId, "ResourceCategory.Created", nameof(ResourceCategory), category.Id,
            new { category.Code, category.Kind, category.ParentCategoryId }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task<IReadOnlyCollection<ResourceCategoryResponse>> ListCategoriesAsync(
        long branchId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.CatalogView, branchId, cancellationToken);
        var categories = await repository.ListCategoriesAsync(includeInactive, cancellationToken);
        return categories.Select(Map).ToArray();
    }

    public async Task<ResourceCategoryResponse> UpdateCategoryAsync(
        long branchId,
        long categoryId,
        UpdateResourceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.CatalogManage, branchId, cancellationToken);
        var category = await repository.GetCategoryAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("Resource category was not found in the current tenant scope.");
        if (!request.IsActive && await repository.CategoryInUseAsync(categoryId, cancellationToken))
        {
            throw new DomainRuleException("A resource category cannot be deactivated while active resources or service requirements use it.");
        }

        var oldVersion = category.Version;
        category.Update(request.ExpectedVersion, request.Name, request.IsActive, clock.UtcNow);
        await AuditAsync(branchId, "ResourceCategory.Updated", nameof(ResourceCategory), category.Id,
            new { OldVersion = oldVersion, NewVersion = category.Version, category.IsActive }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task<BookableResourceResponse> CreateResourceAsync(
        long branchId,
        CreateBookableResourceRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.ResourcesManage, branchId, cancellationToken);
        var categoryId = publicIds.Decode(PublicIdKind.ResourceCategory, request.CategoryId, branch.TenantId);
        var category = await repository.GetCategoryAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("Resource category was not found in the current tenant scope.");
        if (!category.IsActive)
        {
            throw new DomainRuleException("An inactive resource category cannot receive a new resource.");
        }

        var code = NormalizeCode(request.Code);
        if (await repository.ResourceCodeExistsAsync(branchId, code, cancellationToken))
        {
            throw new DomainRuleException("A resource with this code already exists in the branch.");
        }

        var resource = BookableResource.Create(
            branch.TenantId,
            branchId,
            category.Id,
            code,
            request.Name,
            ParseEnum<CapacityMode>(request.CapacityMode, "Capacity mode"),
            request.Capacity,
            string.IsNullOrWhiteSpace(request.TimeZoneId) ? branch.TimeZoneId : request.TimeZoneId,
            publicIds.DecodeOptional(PublicIdKind.Stakeholder, request.ExternalReferenceId, branch.TenantId),
            clock.UtcNow);
        await repository.AddResourceAsync(resource, cancellationToken);
        await AuditAsync(branchId, "BookableResource.Created", nameof(BookableResource), resource.Id,
            new { resource.Code, resource.CategoryId, resource.CapacityMode, resource.Capacity }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new BookableResourceAggregate(resource, category));
    }

    public async Task<IReadOnlyCollection<BookableResourceResponse>> ListResourcesAsync(
        long branchId,
        long? categoryId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.ResourcesView, branchId, cancellationToken);
        var resources = await repository.ListResourcesAsync(branchId, categoryId, includeInactive, cancellationToken);
        return resources.Select(Map).ToArray();
    }

    public async Task<BookableResourceResponse> UpdateResourceAsync(
        long branchId,
        long resourceId,
        UpdateBookableResourceRequest request,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.ResourcesManage, branchId, cancellationToken);
        var aggregate = await GetBranchResourceAsync(branchId, resourceId, cancellationToken);
        var maximumCapabilityCapacity = await repository.GetMaximumCapabilityCapacityAsync(resourceId, cancellationToken);
        if (request.Capacity < maximumCapabilityCapacity)
        {
            throw new DomainRuleException("Resource capacity cannot be lower than an active service capability requirement.");
        }

        var oldVersion = aggregate.Resource.Version;
        aggregate.Resource.Update(
            request.ExpectedVersion,
            request.Name,
            ParseEnum<CapacityMode>(request.CapacityMode, "Capacity mode"),
            request.Capacity,
            request.IsActive,
            clock.UtcNow);
        await AuditAsync(branchId, "BookableResource.Updated", nameof(BookableResource), resourceId,
            new { OldVersion = oldVersion, NewVersion = aggregate.Resource.Version, aggregate.Resource.IsActive },
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<BookableResourceResponse> ChangeResourceStatusAsync(
        long branchId,
        long resourceId,
        ChangeResourceStatusRequest request,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.ResourceStatusManage, branchId, cancellationToken);
        var aggregate = await GetBranchResourceAsync(branchId, resourceId, cancellationToken);
        var oldStatus = aggregate.Resource.OperationalStatus;
        var newStatus = ParseEnum<ResourceOperationalStatus>(request.Status, "Resource status");
        var now = clock.UtcNow;
        aggregate.Resource.ChangeStatus(request.ExpectedVersion, newStatus, now);
        await repository.AddStatusEventAsync(ResourceStatusEvent.Record(
            aggregate.Resource.TenantId,
            branchId,
            resourceId,
            oldStatus,
            newStatus,
            request.Reason,
            actor.ActorId,
            now), cancellationToken);
        await AuditAsync(branchId, "BookableResource.StatusChanged", nameof(BookableResource), resourceId,
            new { From = oldStatus, To = newStatus, aggregate.Resource.Version }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<ResourceCapabilityResponse> AddCapabilityAsync(
        long branchId,
        long resourceId,
        AddResourceCapabilityRequest request,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.ResourcesManage, branchId, cancellationToken);
        var aggregate = await GetBranchResourceAsync(branchId, resourceId, cancellationToken);
        var serviceId = publicIds.Decode(PublicIdKind.ClinicalService, request.ServiceId, aggregate.Resource.TenantId);
        var service = await repository.GetServiceAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException("Service was not found in the current tenant scope.");
        if (!aggregate.Resource.IsActive || service.Status != CatalogItemStatus.Active)
        {
            throw new DomainRuleException("Capabilities require an active resource and active service.");
        }
        if (await repository.CapabilityExistsAsync(resourceId, serviceId, cancellationToken))
        {
            throw new DomainRuleException("The resource already has this service capability.");
        }

        if (request.CapacityRequired > aggregate.Resource.Capacity)
        {
            throw new DomainRuleException("Capability capacity cannot exceed the resource capacity.");
        }

        var capability = ResourceCapability.Create(
            aggregate.Resource.TenantId,
            resourceId,
            serviceId,
            request.DurationOverrideMinutes,
            request.CapacityRequired,
            clock.UtcNow);
        await repository.AddCapabilityAsync(capability, cancellationToken);
        await AuditAsync(branchId, "ResourceCapability.Added", nameof(ResourceCapability), capability.Id,
            new { capability.ResourceId, capability.ServiceId }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new ResourceCapabilityResponse(
            publicIds.Encode(PublicIdKind.ResourceCapability, capability.Id, capability.TenantId),
            publicIds.Encode(PublicIdKind.BookableResource, capability.ResourceId, capability.TenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, capability.ServiceId, capability.TenantId),
            capability.DurationOverrideMinutes,
            capability.CapacityRequired,
            capability.IsActive);
    }

    public async Task<ServiceResourceRequirementResponse> AddRequirementAsync(
        long branchId,
        long serviceId,
        AddServiceResourceRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.CatalogManage, branchId, cancellationToken);
        var service = await repository.GetServiceAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException("Service was not found in the current tenant scope.");
        var categoryId = publicIds.Decode(PublicIdKind.ResourceCategory, request.CategoryId, branch.TenantId);
        var category = await repository.GetCategoryAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("Resource category was not found in the current tenant scope.");
        if (service.Status != CatalogItemStatus.Active || !category.IsActive)
        {
            throw new DomainRuleException("Resource requirements require an active service and active category.");
        }
        var roleCode = NormalizeCode(request.RoleCode);
        if (await repository.RequirementExistsAsync(serviceId, categoryId, roleCode, cancellationToken))
        {
            throw new DomainRuleException("This service resource requirement already exists.");
        }

        var requirement = ServiceResourceRequirement.Create(
            branch.TenantId,
            serviceId,
            categoryId,
            roleCode,
            request.Quantity,
            request.IsOptional,
            clock.UtcNow);
        await repository.AddRequirementAsync(requirement, cancellationToken);
        await AuditAsync(branchId, "ServiceResourceRequirement.Added", nameof(ServiceResourceRequirement), requirement.Id,
            new { requirement.ServiceId, requirement.CategoryId, requirement.RoleCode }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new ServiceResourceRequirementResponse(
            publicIds.Encode(PublicIdKind.ServiceResourceRequirement, requirement.Id, requirement.TenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, requirement.ServiceId, requirement.TenantId),
            publicIds.Encode(PublicIdKind.ResourceCategory, requirement.CategoryId, requirement.TenantId),
            requirement.RoleCode,
            requirement.Quantity,
            requirement.IsOptional);
    }

    private async Task<Branch> RequireBranchAsync(
        string permission,
        long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("The actor is not authorized for this catalog operation.");
        }

        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private async Task<BookableResourceAggregate> GetBranchResourceAsync(
        long branchId,
        long resourceId,
        CancellationToken cancellationToken)
    {
        var aggregate = await repository.GetResourceAsync(resourceId, cancellationToken)
            ?? throw new NotFoundException("Resource was not found in the current tenant scope.");
        if (aggregate.Resource.BranchId != branchId)
        {
            throw new NotFoundException("Resource was not found in the requested branch.");
        }

        return aggregate;
    }

    private async Task AuditAsync(
        long branchId,
        string action,
        string entityType,
        long entityId,
        object data,
        CancellationToken cancellationToken)
    {
        await repository.AddAuditEventAsync(AuditEvent.Record(
            actor.TenantId,
            branchId,
            actor.ActorId,
            action,
            entityType,
            entityId,
            JsonSerializer.Serialize(data),
            correlationContext.CorrelationId,
            clock.UtcNow), cancellationToken);
    }

    private static string NormalizeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException("Code is required.");
        }

        return value.Trim().ToUpperInvariant();
    }

    private static TEnum ParseEnum<TEnum>(string value, string label) where TEnum : struct, Enum
    {
        if (!Enum.TryParse(value, true, out TEnum parsed) || !Enum.IsDefined(parsed))
        {
            throw new DomainRuleException($"{label} is not supported.");
        }

        return parsed;
    }

    private ServiceResponse Map(ClinicalService service) =>
        new(
            publicIds.Encode(PublicIdKind.ClinicalService, service.Id, service.TenantId),
            service.Code,
            service.Name,
            service.Description,
            service.DefaultDurationMinutes,
            service.Status.ToString(),
            service.Version);

    private ResourceCategoryResponse Map(ResourceCategory category) =>
        new(
            publicIds.Encode(PublicIdKind.ResourceCategory, category.Id, category.TenantId),
            publicIds.EncodeOptional(PublicIdKind.ResourceCategory, category.ParentCategoryId, category.TenantId),
            category.Code,
            category.Name,
            category.Kind.ToString(),
            category.IsActive,
            category.Version);

    private BookableResourceResponse Map(BookableResourceAggregate aggregate) =>
        new(
            publicIds.Encode(PublicIdKind.BookableResource, aggregate.Resource.Id, aggregate.Resource.TenantId),
            publicIds.Encode(PublicIdKind.Branch, aggregate.Resource.BranchId, aggregate.Resource.TenantId),
            publicIds.Encode(PublicIdKind.ResourceCategory, aggregate.Resource.CategoryId, aggregate.Resource.TenantId),
            aggregate.Category.Code,
            aggregate.Category.Kind.ToString(),
            aggregate.Resource.Code,
            aggregate.Resource.Name,
            aggregate.Resource.CapacityMode.ToString(),
            aggregate.Resource.Capacity,
            aggregate.Resource.TimeZoneId,
            publicIds.EncodeOptional(PublicIdKind.Stakeholder, aggregate.Resource.ExternalReferenceId, aggregate.Resource.TenantId),
            aggregate.Resource.IsActive,
            aggregate.Resource.OperationalStatus.ToString(),
            aggregate.Resource.Version);
}
