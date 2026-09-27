namespace BookDoc2026.Blazor.UI.Clinical;

public enum ClinicalHandoffAction
{
    StartEncounter,
    CompleteDraft,
    StartPhysiotherapyPlan,
    ContinuePhysiotherapyPlan,
    ReviewSignedEncounter
}

public static class ClinicalHandoffActionResolver
{
    public static ClinicalHandoffAction Resolve(string? encounterStatus, string? specialtyCode,
        string? carePlanId)
    {
        if (string.IsNullOrWhiteSpace(encounterStatus)) return ClinicalHandoffAction.StartEncounter;
        if (string.Equals(encounterStatus, "Draft", StringComparison.Ordinal))
            return ClinicalHandoffAction.CompleteDraft;
        if (!string.Equals(encounterStatus, "Signed", StringComparison.Ordinal)
            || !string.Equals(specialtyCode, "PHYSIOTHERAPY", StringComparison.OrdinalIgnoreCase))
            return ClinicalHandoffAction.ReviewSignedEncounter;
        return string.IsNullOrWhiteSpace(carePlanId)
            ? ClinicalHandoffAction.StartPhysiotherapyPlan
            : ClinicalHandoffAction.ContinuePhysiotherapyPlan;
    }
}
