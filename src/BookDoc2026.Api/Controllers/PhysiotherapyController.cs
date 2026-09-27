using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Clinical;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/physiotherapy/care-plans")]
public sealed class PhysiotherapyController(PhysiotherapyService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<PhysiotherapyCarePlanListItemResponse>>>> List(
        string branchId, [FromQuery] int take = 50, CancellationToken cancellationToken = default) => Ok(
        new ApiEnvelope<IReadOnlyCollection<PhysiotherapyCarePlanListItemResponse>>(
            await service.ListAsync(ids.Tenant(PublicIdKind.Branch, branchId), take, cancellationToken),
            HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansManage)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Create(string branchId,
        CreatePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(ids.Tenant(PublicIdKind.Branch, branchId), request,
            cancellationToken);
        return Created($"/api/v1/branches/{branchId}/physiotherapy/care-plans/{response.Id}",
            new ApiEnvelope<PhysiotherapyCarePlanResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansView)]
    [HttpGet("{carePlanId}")]
    public async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Get(string branchId,
        string carePlanId, CancellationToken cancellationToken) => Ok(
        new ApiEnvelope<PhysiotherapyCarePlanResponse>(await service.GetAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.PhysiotherapyCarePlan, carePlanId),
            cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansManage)]
    [HttpPost("{carePlanId}/revisions")]
    public async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Revise(string branchId,
        string carePlanId, RevisePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken) =>
        Created($"/api/v1/branches/{branchId}/physiotherapy/care-plans/{carePlanId}",
            new ApiEnvelope<PhysiotherapyCarePlanResponse>(await service.ReviseAsync(
                ids.Tenant(PublicIdKind.Branch, branchId),
                ids.Tenant(PublicIdKind.PhysiotherapyCarePlan, carePlanId), request, cancellationToken),
                HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansManage)]
    [HttpPost("{carePlanId}/activate")]
    public async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Activate(string branchId,
        string carePlanId, ActivatePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken) =>
        Ok(new ApiEnvelope<PhysiotherapyCarePlanResponse>(await service.ActivateAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.PhysiotherapyCarePlan, carePlanId),
            request.ExpectedVersion, cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansManage)]
    [HttpPost("{carePlanId}/complete")]
    public Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Complete(string branchId,
        string carePlanId, ChangePhysiotherapyCarePlanStatusRequest request, CancellationToken cancellationToken) =>
        Close(branchId, carePlanId, request, false, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PhysiotherapyCarePlansManage)]
    [HttpPost("{carePlanId}/discontinue")]
    public Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Discontinue(string branchId,
        string carePlanId, ChangePhysiotherapyCarePlanStatusRequest request, CancellationToken cancellationToken) =>
        Close(branchId, carePlanId, request, true, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PhysiotherapySessionsRecord)]
    [HttpPost("{carePlanId}/sessions")]
    public async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> RecordSession(string branchId,
        string carePlanId, RecordPhysiotherapySessionRequest request, CancellationToken cancellationToken) =>
        Created($"/api/v1/branches/{branchId}/physiotherapy/care-plans/{carePlanId}",
            new ApiEnvelope<PhysiotherapyCarePlanResponse>(await service.RecordSessionAsync(
                ids.Tenant(PublicIdKind.Branch, branchId),
                ids.Tenant(PublicIdKind.PhysiotherapyCarePlan, carePlanId), request, cancellationToken),
                HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PhysiotherapyOutcomesRecord)]
    [HttpPost("{carePlanId}/outcomes")]
    public async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> RecordOutcome(string branchId,
        string carePlanId, RecordPhysiotherapyOutcomeRequest request, CancellationToken cancellationToken) =>
        Created($"/api/v1/branches/{branchId}/physiotherapy/care-plans/{carePlanId}",
            new ApiEnvelope<PhysiotherapyCarePlanResponse>(await service.RecordOutcomeAsync(
                ids.Tenant(PublicIdKind.Branch, branchId),
                ids.Tenant(PublicIdKind.PhysiotherapyCarePlan, carePlanId), request, cancellationToken),
                HttpContext.TraceIdentifier));

    private async Task<ActionResult<ApiEnvelope<PhysiotherapyCarePlanResponse>>> Close(string branchId,
        string carePlanId, ChangePhysiotherapyCarePlanStatusRequest request, bool discontinued,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<PhysiotherapyCarePlanResponse>(
        await service.CloseAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.PhysiotherapyCarePlan, carePlanId), request, discontinued, cancellationToken),
        HttpContext.TraceIdentifier));
}
