namespace BookDoc2026.Contracts.Queues;

public sealed record CreateImagingServicePointRequest(
    string Code,
    string Name,
    string Modality,
    string? ResourceId);

public sealed record ImagingServicePointResponse(
    string Id,
    string Code,
    string Name,
    string Modality,
    string? ResourceId,
    bool IsActive,
    long Version);

public sealed record CheckInQueueTicketRequest(
    Guid RequestId,
    string ServicePointId,
    string PatientId,
    string? BookingId,
    string Priority,
    string? PriorityReason);

public sealed record QueueTransitionRequest(long ExpectedVersion, string? Reason = null);

public sealed record QueueTicketResponse(
    string Id,
    string ServicePointId,
    string PatientId,
    string? BookingId,
    string DisplayToken,
    string Priority,
    string Status,
    DateTimeOffset ArrivedUtc,
    DateTimeOffset? CalledUtc,
    DateTimeOffset? ServiceStartedUtc,
    DateTimeOffset? CompletedUtc,
    DateTimeOffset? CancelledUtc,
    int CallCount,
    long Version,
    bool IsReplay);

public sealed record QueueDisplayTicketResponse(
    string DisplayToken,
    string Status,
    int CallCount,
    long Version,
    DateTimeOffset UpdatedUtc);

public sealed record QueueRealtimeEvent(
    string EventType,
    string ServicePointId,
    string TicketId,
    string Status,
    long Version,
    DateTimeOffset OccurredUtc);
