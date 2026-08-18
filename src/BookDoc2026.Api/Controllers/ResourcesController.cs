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
[Route("api/v1/branches/{branchId}/resources")]
public sealed class ResourcesController(CatalogService catalogService, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.ResourcesManage)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<BookableResourceResponse>>> Create(
        string branchId,
        CreateBookableResourceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.CreateResourceAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/resources/{response.Id}",
            new ApiEnvelope<BookableResourceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ResourcesView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<BookableResourceResponse>>>> List(
        string branchId,
        [FromQuery] string? categoryId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.ListResourcesAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.TenantOptional(PublicIdKind.ResourceCategory, categoryId),
            includeInactive,
            cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<BookableResourceResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ResourcesManage)]
    [HttpPut("{resourceId}")]
    public async Task<ActionResult<ApiEnvelope<BookableResourceResponse>>> Update(
        string branchId,
        string resourceId,
        UpdateBookableResourceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.UpdateResourceAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.BookableResource, resourceId), request, cancellationToken);
        return Ok(new ApiEnvelope<BookableResourceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ResourceStatusManage)]
    [HttpPost("{resourceId}/status")]
    public async Task<ActionResult<ApiEnvelope<BookableResourceResponse>>> ChangeStatus(
        string branchId,
        string resourceId,
        ChangeResourceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.ChangeResourceStatusAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.BookableResource, resourceId), request, cancellationToken);
        return Ok(new ApiEnvelope<BookableResourceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ResourcesManage)]
    [HttpPost("{resourceId}/capabilities")]
    public async Task<ActionResult<ApiEnvelope<ResourceCapabilityResponse>>> AddCapability(
        string branchId,
        string resourceId,
        AddResourceCapabilityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.AddCapabilityAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.BookableResource, resourceId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/resources/{resourceId}/capabilities/{response.Id}",
            new ApiEnvelope<ResourceCapabilityResponse>(response, HttpContext.TraceIdentifier));
    }
}
