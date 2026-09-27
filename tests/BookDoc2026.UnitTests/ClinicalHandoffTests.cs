using BookDoc2026.Blazor.UI.Clinical;
using BookDoc2026.Contracts.Clinical;

namespace BookDoc2026.UnitTests;

public sealed class ClinicalHandoffTests
{
    [Theory]
    [InlineData(null, null, null, ClinicalHandoffAction.StartEncounter)]
    [InlineData("Draft", "PHYSIOTHERAPY", null, ClinicalHandoffAction.CompleteDraft)]
    [InlineData("Signed", "ORTHOPAEDICS", null, ClinicalHandoffAction.ReviewSignedEncounter)]
    [InlineData("Signed", "PHYSIOTHERAPY", null, ClinicalHandoffAction.StartPhysiotherapyPlan)]
    [InlineData("Signed", "physiotherapy", "plan-1", ClinicalHandoffAction.ContinuePhysiotherapyPlan)]
    public void Resolves_only_policy_neutral_encounter_to_physiotherapy_handoffs(string? encounterStatus,
        string? specialtyCode, string? carePlanId, ClinicalHandoffAction expected) =>
        Assert.Equal(expected, ClinicalHandoffActionResolver.Resolve(encounterStatus, specialtyCode, carePlanId));

    [Fact]
    public void Encounter_draft_separates_save_requirements_from_signing_requirements()
    {
        var draft = new EncounterDraft
        {
            SpecialtyCode = "PHYSIOTHERAPY", TemplateKey = "COMMON", TemplateVersion = "1",
            ChiefComplaint = "Knee pain"
        };

        Assert.True(draft.CanSave);
        Assert.False(draft.CanSign);
        draft.Assessment = "Policy-neutral assessment";
        draft.Plan = "Policy-neutral plan";
        Assert.True(draft.CanSign);
        Assert.Null(draft.ToRequest().History);

        draft.Load(new EncounterContentRequest("PHYSIOTHERAPY", "COMMON", "2", "Updated", "History",
            null, "Assessment", "Plan", null, "Knee", "RIGHT"));
        Assert.Equal("2", draft.TemplateVersion);
        draft.Reset();
        Assert.False(draft.CanSave);
    }
}
