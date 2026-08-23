using BookDoc2026.Blazor.UI.Queues;
using BookDoc2026.Contracts.Queues;

namespace BookDoc2026.UnitTests;

public sealed class RadiologyCheckInDraftTests
{
    [Fact]
    public void Retry_keeps_request_identity_and_reset_rotates_it_after_completed_work()
    {
        var draft = new RadiologyCheckInDraft();
        var firstRequest = draft.RequestId;

        Assert.Equal(firstRequest, draft.RequestId);
        draft.Complete(new("ticket", "point", "patient", null, "Q-001", "Normal", "Waiting",
            DateTimeOffset.UtcNow, null, null, null, null, 0, 1, false));
        Assert.True(draft.IsComplete);

        draft.Reset();

        Assert.False(draft.IsComplete);
        Assert.NotEqual(firstRequest, draft.RequestId);
    }
}
