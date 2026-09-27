using BookDoc2026.Blazor.UI.Radiology;

namespace BookDoc2026.UnitTests;

public sealed class RadiologyExecutionDraftTests
{
    [Fact]
    public void Acquisition_retry_keeps_request_identity_and_completed_reset_rotates_it()
    {
        var draft = new RadiologyAcquisitionDraft
        {
            EquipmentResourceId = "protected-equipment",
            ProtocolCode = " xr-knee-ap ",
            ProtocolVersion = " v1 ",
            StartedLocal = DateTime.Now.AddMinutes(-2),
            CompletedLocal = DateTime.Now.AddMinutes(-1)
        };
        var requestId = draft.RequestId;

        var first = draft.ToRequest(2);
        var retry = draft.ToRequest(2);

        Assert.True(draft.CanSubmit);
        Assert.Equal(requestId, first.RequestId);
        Assert.Equal(first, retry);
        Assert.Equal("xr-knee-ap", first.ProtocolCode);
        draft.Reset("next-equipment");
        Assert.NotEqual(requestId, draft.RequestId);
        Assert.Equal("next-equipment", draft.EquipmentResourceId);
    }

    [Fact]
    public void Acquisition_requires_structured_deviation_and_abort_reasons()
    {
        var draft = new RadiologyAcquisitionDraft
        {
            EquipmentResourceId = "protected-equipment",
            HasProtocolDeviation = true
        };

        Assert.False(draft.CanSubmit);
        draft.DeviationCode = "POSITIONING";
        Assert.True(draft.CanSubmit);
        draft.Outcome = "Aborted";
        Assert.False(draft.CanSubmit);
        draft.OutcomeReasonCode = "EQUIPMENT_FAULT";
        Assert.True(draft.CanSubmit);
    }

    [Fact]
    public void Quality_draft_binds_latest_attempt_and_preserves_identity_for_retry()
    {
        var draft = new RadiologyQualityDraft();
        draft.LoadAttempt("attempt-one");
        var requestId = draft.RequestId;
        draft.Decision = "RepeatRequired";
        draft.ReasonCode = "MOTION";

        var first = draft.ToRequest(3);
        var retry = draft.ToRequest(3);

        Assert.True(draft.CanSubmit);
        Assert.Equal(first, retry);
        Assert.Equal(requestId, first.RequestId);
        draft.LoadAttempt("attempt-one");
        Assert.Equal(requestId, draft.RequestId);
        draft.LoadAttempt("attempt-two");
        Assert.NotEqual(requestId, draft.RequestId);
        Assert.Equal("Accepted", draft.Decision);
    }
}
