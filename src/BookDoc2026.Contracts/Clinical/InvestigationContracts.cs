using BookDoc2026.Contracts.Queues;

namespace BookDoc2026.Contracts.Clinical;

public sealed record CreateInvestigationOrderRequest(
    Guid RequestId,
    string ServiceId,
    string Modality,
    string ClinicalIndication);

public sealed record HandoffInvestigationOrderRequest(
    Guid RequestId,
    long ExpectedVersion,
    string ServicePointId,
    string Priority,
    string? PriorityReason);

public sealed record InvestigationServiceOptionResponse(
    string ServiceId,
    string ServiceCode,
    string ServiceName,
    string Modality);

public sealed record InvestigationOrderEventResponse(
    string Id,
    string Action,
    string ActorId,
    string? QueueTicketId,
    long OrderVersion,
    DateTimeOffset OccurredUtc);

public sealed record InvestigationOrderResponse(
    string Id,
    string EncounterId,
    string PatientId,
    string ServiceId,
    string ServiceCode,
    string ServiceName,
    string OrderNumber,
    string Modality,
    string ClinicalIndication,
    string OrderStatus,
    string ResultStatus,
    string RequestedByActorId,
    DateTimeOffset OrderedUtc,
    DateTimeOffset? QueueHandoffUtc,
    string? QueueHandoffByActorId,
    long Version,
    QueueTicketResponse? QueueTicket,
    IReadOnlyCollection<InvestigationOrderEventResponse> History,
    bool IsReplay);

/// <summary>
/// A deliberately bounded technician projection. It identifies the queued work and the
/// originating patient, Encounter and catalog order without exposing Encounter narrative,
/// result content, clinical documents or contact details.
/// </summary>
public sealed record InvestigationWorklistItemResponse(
    QueueTicketResponse QueueTicket,
    string OrderId,
    string OrderNumber,
    string ServiceId,
    string ServiceCode,
    string ServiceName,
    string Modality,
    string ClinicalIndication,
    DateTimeOffset OrderedUtc,
    string PatientId,
    string PatientNumber,
    string PatientDisplayName,
    string EncounterId,
    string EncounterNumber);
