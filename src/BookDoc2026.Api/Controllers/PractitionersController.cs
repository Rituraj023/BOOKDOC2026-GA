using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Workforce;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Workforce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/practitioners")]
public sealed class PractitionersController(PractitionerService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.PractitionersManage)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> Create(string branchId,
        CreatePractitionerRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(ids.Tenant(PublicIdKind.Branch, branchId), request,
            cancellationToken);
        return Created($"/api/v1/branches/{branchId}/practitioners/{response.Id}",
            new ApiEnvelope<PractitionerResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.PractitionersView)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<PractitionerSummaryResponse>>>> List(
        string branchId, CancellationToken cancellationToken) =>
        Ok(new ApiEnvelope<IReadOnlyCollection<PractitionerSummaryResponse>>(
            await service.ListBranchPractitionersAsync(ids.Tenant(PublicIdKind.Branch, branchId), cancellationToken),
            HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PractitionersView)]
    [HttpGet("{practitionerId}")]
    public async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> Get(string branchId, string practitionerId,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<PractitionerResponse>(
            await service.GetAsync(ids.Tenant(PublicIdKind.Branch, branchId),
                ids.Tenant(PublicIdKind.Practitioner, practitionerId), cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PractitionersManage)]
    [HttpPost("{practitionerId}/credentials")]
    public async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> AddCredential(string branchId,
        string practitionerId, AddPractitionerCredentialRequest request, CancellationToken cancellationToken) =>
        Created($"/api/v1/branches/{branchId}/practitioners/{practitionerId}",
            new ApiEnvelope<PractitionerResponse>(await service.AddCredentialAsync(
                ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.Practitioner, practitionerId),
                request, cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PractitionerCredentialsVerify)]
    [HttpPost("{practitionerId}/credentials/{credentialId}/verify")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> VerifyCredential(string branchId,
        string practitionerId, string credentialId, DecidePractitionerCredentialRequest request,
        CancellationToken cancellationToken) => Decision(service.VerifyCredentialAsync, branchId, practitionerId,
            credentialId, request, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PractitionerCredentialsVerify)]
    [HttpPost("{practitionerId}/credentials/{credentialId}/reject")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> RejectCredential(string branchId,
        string practitionerId, string credentialId, DecidePractitionerCredentialRequest request,
        CancellationToken cancellationToken) => Decision(service.RejectCredentialAsync, branchId, practitionerId,
            credentialId, request, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PractitionerAssignmentsManage)]
    [HttpPost("{practitionerId}/assignments")]
    public async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> AddAssignment(string branchId,
        string practitionerId, AddPractitionerAssignmentRequest request, CancellationToken cancellationToken) =>
        Created($"/api/v1/branches/{branchId}/practitioners/{practitionerId}",
            new ApiEnvelope<PractitionerResponse>(await service.AddAssignmentAsync(
                ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.Practitioner, practitionerId),
                request, cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.PractitionersManage)]
    [HttpPost("{practitionerId}/activate")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> Activate(string branchId, string practitionerId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) =>
        ProfileChange(service.ActivateAsync, branchId, practitionerId, request, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PractitionersManage)]
    [HttpPost("{practitionerId}/suspend")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> Suspend(string branchId, string practitionerId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) =>
        ProfileChange(service.SuspendAsync, branchId, practitionerId, request, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PractitionersManage)]
    [HttpPost("{practitionerId}/deactivate")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> Deactivate(string branchId, string practitionerId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) =>
        ProfileChange(service.DeactivateAsync, branchId, practitionerId, request, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PractitionerAssignmentsManage)]
    [HttpPost("{practitionerId}/assignments/{assignmentId}/suspend")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> SuspendAssignment(string branchId,
        string practitionerId, string assignmentId, ChangePractitionerStatusRequest request,
        CancellationToken cancellationToken) => AssignmentChange(service.SuspendAssignmentAsync, branchId,
            practitionerId, assignmentId, request, cancellationToken);

    [Authorize(Policy = FoundationPermissions.PractitionerAssignmentsManage)]
    [HttpPost("{practitionerId}/assignments/{assignmentId}/end")]
    public Task<ActionResult<ApiEnvelope<PractitionerResponse>>> EndAssignment(string branchId,
        string practitionerId, string assignmentId, ChangePractitionerStatusRequest request,
        CancellationToken cancellationToken) => AssignmentChange(service.EndAssignmentAsync, branchId,
            practitionerId, assignmentId, request, cancellationToken);

    private async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> Decision(
        Func<long, long, long, DecidePractitionerCredentialRequest, CancellationToken, Task<PractitionerResponse>> command,
        string branchId, string practitionerId, string credentialId, DecidePractitionerCredentialRequest request,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<PractitionerResponse>(await command(
            ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.Practitioner, practitionerId),
            ids.Tenant(PublicIdKind.PractitionerCredential, credentialId), request, cancellationToken),
            HttpContext.TraceIdentifier));

    private async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> ProfileChange(
        Func<long, long, ChangePractitionerStatusRequest, CancellationToken, Task<PractitionerResponse>> command,
        string branchId, string practitionerId, ChangePractitionerStatusRequest request,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<PractitionerResponse>(await command(
            ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.Practitioner, practitionerId), request,
            cancellationToken), HttpContext.TraceIdentifier));

    private async Task<ActionResult<ApiEnvelope<PractitionerResponse>>> AssignmentChange(
        Func<long, long, long, ChangePractitionerStatusRequest, CancellationToken, Task<PractitionerResponse>> command,
        string branchId, string practitionerId, string assignmentId, ChangePractitionerStatusRequest request,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<PractitionerResponse>(await command(
            ids.Tenant(PublicIdKind.Branch, branchId), ids.Tenant(PublicIdKind.Practitioner, practitionerId),
            ids.Tenant(PublicIdKind.PractitionerAssignment, assignmentId), request, cancellationToken),
            HttpContext.TraceIdentifier));
}
