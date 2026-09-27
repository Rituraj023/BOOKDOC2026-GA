using BookDoc2026.Blazor.UI.Queues;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Queues;

namespace BookDoc2026.UnitTests;

public sealed class RadiologyQueueProjectionTests
{
    [Fact]
    public void Authorized_branch_event_invalidates_selected_projection_without_comparing_ciphertext_ids()
    {
        var projection = new RadiologyQueueProjection();
        projection.Replace([Work("ticket-1", "point-1", 3)], []);

        Assert.True(projection.RequiresRefresh(Change("different-ciphertext", "different-point-ciphertext", 3), "point-1"));
        Assert.True(projection.RequiresRefresh(Change("ticket-1", "point-1", 4), "point-1"));
        Assert.False(projection.RequiresRefresh(Change("ticket-2", "point-1", 1), string.Empty));
        Assert.False(projection.RequiresRefresh(
            new("Unknown", "point-1", "ticket-2", "Called", 1, DateTimeOffset.UtcNow), "point-1"));
    }

    [Fact]
    public void Api_snapshot_replaces_worklist_and_safe_display_together()
    {
        var projection = new RadiologyQueueProjection();
        var display = new QueueDisplayTicketResponse("XR-009", "Called", 1, 2, DateTimeOffset.UtcNow);

        projection.Replace([Work("ticket-9", "point-1", 2)], [display]);

        var work = Assert.Single(projection.Items);
        Assert.Equal("ENC-REFERENCE", work.EncounterNumber);
        Assert.Equal("Persistent knee pain", work.ClinicalIndication);
        Assert.Equal("XR-009", Assert.Single(projection.DisplayTickets).DisplayToken);
    }

    private static InvestigationWorklistItemResponse Work(string id, string point, long version) => new(
        Ticket(id, point, version), "order", "INV-REFERENCE", "service", "XR-KNEE", "Knee X-ray",
        "XRay", "Persistent knee pain", DateTimeOffset.UtcNow, "patient", "PAT-REFERENCE",
        "Test Patient", "encounter", "ENC-REFERENCE");

    private static QueueTicketResponse Ticket(string id, string point, long version) => new(
        id, point, "patient", null, null, "XR-009", "Normal", "Waiting", DateTimeOffset.UtcNow,
        null, null, null, null, 0, version, false);

    private static QueueRealtimeEvent Change(string id, string point, long version) =>
        new("QueueTicketChanged", point, id, "Called", version, DateTimeOffset.UtcNow);
}
