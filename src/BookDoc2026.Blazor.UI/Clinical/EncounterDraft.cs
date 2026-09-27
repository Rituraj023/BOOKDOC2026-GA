using BookDoc2026.Contracts.Clinical;

namespace BookDoc2026.Blazor.UI.Clinical;

public sealed class PrescriptionItem
{
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = "1-0-1";
    public string Duration { get; set; } = "5 days";
    public string Instructions { get; set; } = "After meals";
}

public sealed class EncounterDraft
{
    public string? SupervisingPractitionerId { get; set; }
    public List<PrescriptionItem> Prescriptions { get; } = [];
    public string SpecialtyCode { get; set; } = "OPD";
    public string TemplateKey { get; set; } = "GENERAL_OPD";
    public string TemplateVersion { get; set; } = "1.0";
    public string ChiefComplaint { get; set; } = string.Empty;
    public string History { get; set; } = string.Empty;
    public string Examination { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string BodySite { get; set; } = string.Empty;
    public string LateralityCode { get; set; } = string.Empty;

    public bool CanSave => Required(SpecialtyCode, TemplateKey, TemplateVersion, ChiefComplaint);
    public bool CanSign => CanSave && Required(Assessment, Plan);

    public EncounterContentRequest ToRequest()
    {
        var instructionsText = Instructions;
        if (Prescriptions.Count > 0)
        {
            var rxSummary = "Prescription (Rx):\n" + string.Join("\n", Prescriptions
                .Where(p => !string.IsNullOrWhiteSpace(p.DrugName))
                .Select((p, i) => $"{i + 1}. {p.DrugName} {p.Dosage} | {p.Frequency} | {p.Duration} | {p.Instructions}"));
            instructionsText = string.IsNullOrWhiteSpace(instructionsText)
                ? rxSummary
                : instructionsText + "\n\n" + rxSummary;
        }

        return new(SpecialtyCode, TemplateKey, TemplateVersion,
            ChiefComplaint, Null(History), Null(Examination), Null(Assessment), Null(Plan), Null(instructionsText),
            Null(BodySite), Null(LateralityCode));
    }

    public void Load(EncounterContentRequest content)
    {
        SpecialtyCode = content.SpecialtyCode; TemplateKey = content.TemplateKey;
        TemplateVersion = content.TemplateVersion; ChiefComplaint = content.ChiefComplaint;
        History = content.History ?? string.Empty; Examination = content.Examination ?? string.Empty;
        Assessment = content.Assessment ?? string.Empty; Plan = content.Plan ?? string.Empty;
        Instructions = content.Instructions ?? string.Empty; BodySite = content.BodySite ?? string.Empty;
        LateralityCode = content.LateralityCode ?? string.Empty;
    }

    public void Reset() => SpecialtyCode = TemplateKey = TemplateVersion = ChiefComplaint = History =
        Examination = Assessment = Plan = Instructions = BodySite = LateralityCode = string.Empty;

    private static bool Required(params string[] values) => values.All(value => !string.IsNullOrWhiteSpace(value));
    private static string? Null(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
