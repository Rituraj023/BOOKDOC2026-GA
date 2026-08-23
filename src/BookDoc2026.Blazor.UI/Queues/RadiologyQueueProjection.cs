using BookDoc2026.Contracts.Queues;

namespace BookDoc2026.Blazor.UI.Queues;

/// <summary>
/// Maintains the latest API-authoritative queue projection. SignalR events only
/// indicate that a newer snapshot may be available; they never mutate durable state.
/// </summary>
public sealed class RadiologyQueueProjection
{
    public IReadOnlyList<QueueTicketResponse> Tickets { get; private set; } = [];
    public IReadOnlyList<QueueDisplayTicketResponse> DisplayTickets { get; private set; } = [];

    public void Replace(
        IEnumerable<QueueTicketResponse> tickets,
        IEnumerable<QueueDisplayTicketResponse> displayTickets)
    {
        Tickets = tickets.ToArray();
        DisplayTickets = displayTickets.ToArray();
    }

    public bool RequiresRefresh(QueueRealtimeEvent change, string selectedServicePointId)
    {
        // DTO identifiers are protected with probabilistic encryption. The same numeric ID
        // can therefore have different valid ciphertext in the API snapshot and live event.
        // The hub already limits events to authorized branch groups, so every well-formed
        // queue change invalidates the selected API projection.
        return !string.IsNullOrWhiteSpace(selectedServicePointId)
            && string.Equals(change.EventType, "QueueTicketChanged", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(change.TicketId);
    }
}
