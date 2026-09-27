using BookDoc2026.Contracts.Clinical;

namespace BookDoc2026.Blazor.UI.Clinical;

public sealed class EncounterDraft
{
    public string SpecialtyCode { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
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

    public EncounterContentRequest ToRequest() => new(SpecialtyCode, TemplateKey, TemplateVersion,
        ChiefComplaint, Null(History), Null(Examination), Null(Assessment), Null(Plan), Null(Instructions),
        Null(BodySite), Null(LateralityCode));

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
