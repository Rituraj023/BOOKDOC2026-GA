using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Communications;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Communications;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/communications")]
public sealed class CommunicationsController(
    CommunicationService communicationService,
    HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.CommunicationPreferencesView)]
    [HttpGet("stakeholders/{stakeholderId}/preferences")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<CommunicationPreferenceResponse>>>> ListPreferences(
        string branchId,
        string stakeholderId,
        CancellationToken cancellationToken = default)
    {
        var response = await communicationService.ListPreferencesAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Stakeholder, stakeholderId),
            cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<CommunicationPreferenceResponse>>(
            response,
            HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.CommunicationPreferencesManage)]
    [HttpPost("stakeholders/{stakeholderId}/preferences")]
    public async Task<ActionResult<ApiEnvelope<CommunicationPreferenceResponse>>> RecordPreference(
        string branchId,
        string stakeholderId,
        RecordCommunicationPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await communicationService.RecordPreferenceAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Stakeholder, stakeholderId),
            request,
            cancellationToken);
        return Ok(new ApiEnvelope<CommunicationPreferenceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.ProviderCallbacksView)]
    [HttpGet("provider-callbacks")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<ProviderCallbackInboxResponse>>>> ListCallbacks(
        string branchId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await communicationService.ListCallbacksAsync(
            ids.Tenant(PublicIdKind.Branch, branchId), take, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<ProviderCallbackInboxResponse>>(
            response,
            HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.MessageTemplatesView)]
    [HttpGet("templates")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<MessageTemplateResponse>>>> ListTemplates(
        string branchId,
        CancellationToken cancellationToken = default)
    {
        var response = await communicationService.ListTemplatesAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<MessageTemplateResponse>>(
            response,
            HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.MessageTemplatesManage)]
    [HttpPost("templates/versions")]
    public async Task<ActionResult<ApiEnvelope<MessageTemplateResponse>>> CreateBranchVersion(
        string branchId,
        CreateMessageTemplateVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await communicationService.CreateBranchVersionAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            request,
            cancellationToken);
        return Ok(new ApiEnvelope<MessageTemplateResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.MessageTemplatesView)]
    [HttpPost("templates/{templateId}/preview")]
    public async Task<ActionResult<ApiEnvelope<MessageTemplatePreviewResponse>>> Preview(
        string branchId,
        string templateId,
        PreviewMessageTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantBranchId = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await communicationService.PreviewAsync(
            tenantBranchId,
            ids.Tenant(PublicIdKind.MessageTemplate, templateId),
            request,
            cancellationToken);
        return Ok(new ApiEnvelope<MessageTemplatePreviewResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.MessageTemplatesPublish)]
    [HttpPost("templates/{templateId}/publish")]
    public async Task<ActionResult<ApiEnvelope<MessageTemplateResponse>>> Publish(
        string branchId,
        string templateId,
        ChangeMessageTemplateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantBranchId = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await communicationService.PublishAsync(
            tenantBranchId,
            ids.Tenant(PublicIdKind.MessageTemplate, templateId),
            request.ExpectedRevision,
            cancellationToken);
        return Ok(new ApiEnvelope<MessageTemplateResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.MessageTemplatesManage)]
    [HttpPost("templates/{templateId}/retire")]
    public async Task<ActionResult<ApiEnvelope<MessageTemplateResponse>>> Retire(
        string branchId,
        string templateId,
        ChangeMessageTemplateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantBranchId = ids.Tenant(PublicIdKind.Branch, branchId);
        var response = await communicationService.RetireAsync(
            tenantBranchId,
            ids.Tenant(PublicIdKind.MessageTemplate, templateId),
            request.ExpectedRevision,
            cancellationToken);
        return Ok(new ApiEnvelope<MessageTemplateResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.MessageDeliveriesView)]
    [HttpGet("delivery-attempts")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<MessageDeliveryAttemptResponse>>>> ListDeliveryAttempts(
        string branchId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await communicationService.ListDeliveryAttemptsAsync(
            ids.Tenant(PublicIdKind.Branch, branchId),
            take,
            cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<MessageDeliveryAttemptResponse>>(
            response,
            HttpContext.TraceIdentifier));
    }
}
