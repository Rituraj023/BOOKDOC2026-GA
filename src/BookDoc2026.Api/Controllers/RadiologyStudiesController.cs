using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Radiology;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Radiology;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}")]
public sealed class RadiologyStudiesController(
    RadiologyStudyService service,
    HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.RadiologyStudiesView)]
    [HttpGet("investigation-orders/{orderId}/radiology-study")]
    public async Task<ActionResult<ApiEnvelope<RadiologyStudyResponse>>> GetByOrder(
        string branchId, string orderId, CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.GetByOrderAsync(branch,
            ids.Tenant(PublicIdKind.InvestigationOrder, orderId), cancellationToken);
        return Ok(new ApiEnvelope<RadiologyStudyResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.RadiologyStudiesStart)]
    [HttpPost("investigation-orders/{orderId}/radiology-study")]
    public async Task<ActionResult<ApiEnvelope<RadiologyStudyResponse>>> Register(
        string branchId, string orderId, RegisterRadiologyStudyRequest request,
        CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.RegisterAsync(branch,
            ids.Tenant(PublicIdKind.InvestigationOrder, orderId), request, cancellationToken);
        var envelope = new ApiEnvelope<RadiologyStudyResponse>(response, HttpContext.TraceIdentifier);
        return response.IsReplay
            ? Ok(envelope)
            : Created($"/api/v1/branches/{branchId}/radiology/studies/{response.Id}", envelope);
    }

    [Authorize(Policy = FoundationPermissions.RadiologyStudiesView)]
    [HttpGet("radiology/studies/{studyId}")]
    public async Task<ActionResult<ApiEnvelope<RadiologyStudyResponse>>> Get(
        string branchId, string studyId, CancellationToken cancellationToken)
    {
        var branch = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await service.GetAsync(branch,
            ids.Tenant(PublicIdKind.RadiologyStudy, studyId), cancellationToken);
        return Ok(new ApiEnvelope<RadiologyStudyResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.RadiologyAcquisitionsRecord)]
    [HttpGet("radiology/studies/{studyId}/eligible-equipment")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<RadiologyEquipmentOptionResponse>>>>
        ListEligibleEquipment(string branchId, string studyId, CancellationToken cancellationToken)
    {
        var response = await service.ListEligibleEquipmentAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.RadiologyStudy, studyId), cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<RadiologyEquipmentOptionResponse>>(
            response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.RadiologyStudiesStart)]
    [HttpPost("radiology/studies/{studyId}/start")]
    public async Task<ActionResult<ApiEnvelope<RadiologyStudyResponse>>> Start(
        string branchId, string studyId, StartRadiologyStudyRequest request,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<RadiologyStudyResponse>(
        await service.StartAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.RadiologyStudy, studyId), request, cancellationToken),
        HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.RadiologyAcquisitionsRecord)]
    [HttpPost("radiology/studies/{studyId}/acquisitions")]
    public async Task<ActionResult<ApiEnvelope<RadiologyStudyResponse>>> RecordAcquisition(
        string branchId, string studyId, RecordRadiologyAcquisitionRequest request,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<RadiologyStudyResponse>(
        await service.RecordAcquisitionAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.RadiologyStudy, studyId), request, cancellationToken),
        HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.RadiologyStudiesQualityReview)]
    [HttpPost("radiology/studies/{studyId}/quality-reviews")]
    public async Task<ActionResult<ApiEnvelope<RadiologyStudyResponse>>> ReviewQuality(
        string branchId, string studyId, ReviewRadiologyQualityRequest request,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<RadiologyStudyResponse>(
        await service.ReviewQualityAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.RadiologyStudy, studyId), request, cancellationToken),
        HttpContext.TraceIdentifier));
}
