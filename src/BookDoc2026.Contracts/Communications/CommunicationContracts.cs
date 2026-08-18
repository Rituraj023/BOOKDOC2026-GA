namespace BookDoc2026.Contracts.Communications;

public sealed record MessageDeliveryAttemptResponse(
    string Id,
    string Channel,
    string TemplateKey,
    int TemplateVersion,
    int AttemptNumber,
    string ProviderCode,
    string RecipientHint,
    string Status,
    string? ProviderMessageId,
    string? ErrorCode,
    DateTimeOffset AttemptedUtc);

public sealed record MessageTemplateResponse(
    string Id,
    string Scope,
    string Key,
    int Version,
    string Channel,
    string Culture,
    string ContentKind,
    string? SubjectTemplate,
    string BodyTemplate,
    string Status,
    DateTimeOffset? PublishedUtc,
    long Revision);

public sealed record CreateMessageTemplateVersionRequest(
    string Key,
    string Channel,
    string Culture,
    string ContentKind,
    string BodyTemplate,
    string? SubjectTemplate);

public sealed record PreviewMessageTemplateRequest(
    IReadOnlyDictionary<string, string?> Values);

public sealed record MessageTemplatePreviewResponse(
    string? Subject,
    string Body,
    string ContentKind);

public sealed record ChangeMessageTemplateStatusRequest(long ExpectedRevision);

public sealed record RecordCommunicationPreferenceRequest(
    string PurposeCode,
    string MessageClass,
    string Channel,
    string Decision,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string TimeZoneId,
    string EvidenceSource,
    string? EvidenceReference);

public sealed record CommunicationPreferenceResponse(
    string Id,
    string StakeholderId,
    string PurposeCode,
    string MessageClass,
    string Channel,
    string Decision,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string TimeZoneId,
    string EvidenceSource,
    string? EvidenceReference,
    DateTimeOffset RecordedUtc);

public sealed record ProviderCallbackAcceptedResponse(
    string Id,
    string Status,
    bool IsReplay);

public sealed record ProviderCallbackInboxResponse(
    string Id,
    string ProviderCode,
    string ExternalEventId,
    string ProviderMessageId,
    string DeliveryStatus,
    string InboxStatus,
    string? ErrorCode,
    DateTimeOffset OccurredUtc,
    DateTimeOffset ReceivedUtc,
    DateTimeOffset? ProcessedUtc);
