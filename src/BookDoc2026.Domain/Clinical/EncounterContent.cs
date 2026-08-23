using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed record EncounterContent
{
    public string SpecialtyCode { get; }
    public string TemplateKey { get; }
    public string TemplateVersion { get; }
    public string ChiefComplaint { get; }
    public string? History { get; }
    public string? Examination { get; }
    public string? Assessment { get; }
    public string? Plan { get; }
    public string? Instructions { get; }
    public string? BodySite { get; }
    public string? LateralityCode { get; }

    private EncounterContent(string specialtyCode, string templateKey, string templateVersion,
        string chiefComplaint, string? history, string? examination, string? assessment, string? plan,
        string? instructions, string? bodySite, string? lateralityCode)
    {
        SpecialtyCode = specialtyCode;
        TemplateKey = templateKey;
        TemplateVersion = templateVersion;
        ChiefComplaint = chiefComplaint;
        History = history;
        Examination = examination;
        Assessment = assessment;
        Plan = plan;
        Instructions = instructions;
        BodySite = bodySite;
        LateralityCode = lateralityCode;
    }

    public static EncounterContent Create(string specialtyCode, string templateKey, string templateVersion,
        string chiefComplaint, string? history, string? examination, string? assessment, string? plan,
        string? instructions, string? bodySite, string? lateralityCode) => new(
        Code(specialtyCode, 3, 40, "Specialty code"),
        Code(templateKey, 3, 80, "Template key"),
        Required(templateVersion, 1, 40, "Template version"),
        Required(chiefComplaint, 1, 2000, "Chief complaint"),
        Optional(history, 8000), Optional(examination, 8000), Optional(assessment, 8000),
        Optional(plan, 8000), Optional(instructions, 8000), Optional(bodySite, 200),
        string.IsNullOrWhiteSpace(lateralityCode) ? null : Code(lateralityCode, 2, 30, "Laterality code"));

    public void ValidateForSigning()
    {
        if (string.IsNullOrWhiteSpace(Assessment) || string.IsNullOrWhiteSpace(Plan))
            throw new DomainRuleException("Assessment and plan are required before an encounter can be signed.");
    }

    public string Hash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new
        {
            SpecialtyCode, TemplateKey, TemplateVersion, ChiefComplaint, History, Examination,
            Assessment, Plan, Instructions, BodySite, LateralityCode
        }))));

    private static string Code(string value, int min, int max, string label) =>
        Required(value, min, max, label).ToUpperInvariant();

    private static string Required(string value, int min, int max, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length < min || normalized.Length > max)
            throw new DomainRuleException($"{label} must contain between {min} and {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max) throw new DomainRuleException($"Clinical text cannot exceed {max} characters.");
        return normalized;
    }
}
