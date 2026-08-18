using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Catalog;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/catalog")]
public sealed class CatalogController(CatalogService catalogService, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.CatalogManage)]
    [HttpPost("services")]
    public async Task<ActionResult<ApiEnvelope<ServiceResponse>>> CreateService(
        string branchId,
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.CreateServiceAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/catalog/services/{response.Id}",
            new ApiEnvelope<ServiceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CatalogView)]
    [HttpGet("services")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<ServiceResponse>>>> ListServices(
        string branchId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.ListServicesAsync(ids.Tenant(PublicIdKind.Branch, branchId), includeInactive, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<ServiceResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CatalogManage)]
    [HttpPut("services/{serviceId}")]
    public async Task<ActionResult<ApiEnvelope<ServiceResponse>>> UpdateService(
        string branchId,
        string serviceId,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.UpdateServiceAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.ClinicalService, serviceId), request, cancellationToken);
        return Ok(new ApiEnvelope<ServiceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CatalogManage)]
    [HttpPost("services/{serviceId}/resource-requirements")]
    public async Task<ActionResult<ApiEnvelope<ServiceResourceRequirementResponse>>> AddRequirement(
        string branchId,
        string serviceId,
        AddServiceResourceRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.AddRequirementAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.ClinicalService, serviceId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/catalog/services/{serviceId}/resource-requirements/{response.Id}",
            new ApiEnvelope<ServiceResourceRequirementResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CatalogManage)]
    [HttpPost("resource-categories")]
    public async Task<ActionResult<ApiEnvelope<ResourceCategoryResponse>>> CreateCategory(
        string branchId,
        CreateResourceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.CreateCategoryAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/catalog/resource-categories/{response.Id}",
            new ApiEnvelope<ResourceCategoryResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CatalogView)]
    [HttpGet("resource-categories")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<ResourceCategoryResponse>>>> ListCategories(
        string branchId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.ListCategoriesAsync(ids.Tenant(PublicIdKind.Branch, branchId), includeInactive, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<ResourceCategoryResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CatalogManage)]
    [HttpPut("resource-categories/{categoryId}")]
    public async Task<ActionResult<ApiEnvelope<ResourceCategoryResponse>>> UpdateCategory(
        string branchId,
        string categoryId,
        UpdateResourceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.UpdateCategoryAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.ResourceCategory, categoryId), request, cancellationToken);
        return Ok(new ApiEnvelope<ResourceCategoryResponse>(response, HttpContext.TraceIdentifier));
    }
}
