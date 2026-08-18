using System.Net.Http.Json;
using BookDoc2026.Contracts.Communications;

namespace BookDoc2026.Client;

public sealed class BookDocApiClient(HttpClient httpClient)
{
    public Task<IReadOnlyCollection<MessageTemplateResponse>> ListMessageTemplatesAsync(
        string branchId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<MessageTemplateResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/templates",
            cancellationToken);

    public Task<IReadOnlyCollection<CommunicationPreferenceResponse>> ListCommunicationPreferencesAsync(
        string branchId,
        string stakeholderId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<CommunicationPreferenceResponse>>(
            PreferenceUrl(branchId, stakeholderId), cancellationToken);

    public Task<CommunicationPreferenceResponse> RecordCommunicationPreferenceAsync(
        string branchId,
        string stakeholderId,
        RecordCommunicationPreferenceRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<RecordCommunicationPreferenceRequest, CommunicationPreferenceResponse>(
            PreferenceUrl(branchId, stakeholderId), request, cancellationToken);

    public Task<IReadOnlyCollection<ProviderCallbackInboxResponse>> ListProviderCallbacksAsync(
        string branchId,
        int take = 50,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<ProviderCallbackInboxResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/provider-callbacks?take={take}",
            cancellationToken);

    public Task<MessageTemplateResponse> CreateMessageTemplateVersionAsync(
        string branchId,
        CreateMessageTemplateVersionRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateMessageTemplateVersionRequest, MessageTemplateResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/templates/versions",
            request,
            cancellationToken);

    public Task<MessageTemplatePreviewResponse> PreviewMessageTemplateAsync(
        string branchId,
        string templateId,
        PreviewMessageTemplateRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<PreviewMessageTemplateRequest, MessageTemplatePreviewResponse>(
            TemplateActionUrl(branchId, templateId, "preview"),
            request,
            cancellationToken);

    public Task<MessageTemplateResponse> PublishMessageTemplateAsync(
        string branchId,
        string templateId,
        long expectedRevision,
        CancellationToken cancellationToken = default) =>
        PostAsync<ChangeMessageTemplateStatusRequest, MessageTemplateResponse>(
            TemplateActionUrl(branchId, templateId, "publish"),
            new ChangeMessageTemplateStatusRequest(expectedRevision),
            cancellationToken);

    public Task<MessageTemplateResponse> RetireMessageTemplateAsync(
        string branchId,
        string templateId,
        long expectedRevision,
        CancellationToken cancellationToken = default) =>
        PostAsync<ChangeMessageTemplateStatusRequest, MessageTemplateResponse>(
            TemplateActionUrl(branchId, templateId, "retire"),
            new ChangeMessageTemplateStatusRequest(expectedRevision),
            cancellationToken);

    public Task<IReadOnlyCollection<MessageDeliveryAttemptResponse>> ListMessageDeliveryAttemptsAsync(
        string branchId,
        int take = 50,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<MessageDeliveryAttemptResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/delivery-attempts?take={take}",
            cancellationToken);

    public async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
        return await ApiResponseReader.ReadAsync<T>(response, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(relativeUrl, request, cancellationToken);
        return await ApiResponseReader.ReadAsync<TResponse>(response, cancellationToken);
    }

    private static string TemplateActionUrl(string branchId, string templateId, string action) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/templates/" +
        $"{Uri.EscapeDataString(templateId)}/{action}";

    private static string PreferenceUrl(string branchId, string stakeholderId) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/stakeholders/" +
        $"{Uri.EscapeDataString(stakeholderId)}/preferences";
}
