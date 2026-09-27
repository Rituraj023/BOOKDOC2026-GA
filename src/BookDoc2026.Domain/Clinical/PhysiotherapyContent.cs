using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed record PhysiotherapyCarePlanContent
{
    public string GoalSummary { get; }
    public string FrequencyAndDuration { get; }
    public string PlannedInterventions { get; }
    public string? Precautions { get; }
    public DateOnly? ReviewOn { get; }

    private PhysiotherapyCarePlanContent(string goalSummary, string frequencyAndDuration,
        string plannedInterventions, string? precautions, DateOnly? reviewOn)
    {
        GoalSummary = goalSummary;
        FrequencyAndDuration = frequencyAndDuration;
        PlannedInterventions = plannedInterventions;
        Precautions = precautions;
        ReviewOn = reviewOn;
    }

    public static PhysiotherapyCarePlanContent Create(string goalSummary, string frequencyAndDuration,
        string plannedInterventions, string? precautions, DateOnly? reviewOn) => new(
        PhysiotherapyText.Required(goalSummary, 5, 4000, "Goal summary"),
        PhysiotherapyText.Required(frequencyAndDuration, 3, 1000, "Frequency and duration"),
        PhysiotherapyText.Required(plannedInterventions, 3, 6000, "Planned interventions"),
        PhysiotherapyText.Optional(precautions, 4000), reviewOn);

    public string Hash() => PhysiotherapyText.Hash(new
        { GoalSummary, FrequencyAndDuration, PlannedInterventions, Precautions, ReviewOn });
}

public sealed record PhysiotherapySessionContent
{
    public string SubjectiveResponse { get; }
    public string Interventions { get; }
    public string Tolerance { get; }
    public string NextPlan { get; }
    public bool HadAdverseEvent { get; }
    public string? AdverseEventDetails { get; }

    private PhysiotherapySessionContent(string subjectiveResponse, string interventions, string tolerance,
        string nextPlan, bool hadAdverseEvent, string? adverseEventDetails)
    {
        SubjectiveResponse = subjectiveResponse;
        Interventions = interventions;
        Tolerance = tolerance;
        NextPlan = nextPlan;
        HadAdverseEvent = hadAdverseEvent;
        AdverseEventDetails = adverseEventDetails;
    }

    public static PhysiotherapySessionContent Create(string subjectiveResponse, string interventions,
        string tolerance, string nextPlan, bool hadAdverseEvent, string? adverseEventDetails)
    {
        var details = PhysiotherapyText.Optional(adverseEventDetails, 4000);
        if (hadAdverseEvent && string.IsNullOrWhiteSpace(details))
            throw new DomainRuleException("Adverse-event details are required when an event is recorded.");
        if (!hadAdverseEvent && details is not null)
            throw new DomainRuleException("Adverse-event details require an adverse-event indicator.");
        return new(
            PhysiotherapyText.Required(subjectiveResponse, 1, 4000, "Subjective response"),
            PhysiotherapyText.Required(interventions, 3, 8000, "Interventions"),
            PhysiotherapyText.Required(tolerance, 1, 4000, "Tolerance"),
            PhysiotherapyText.Required(nextPlan, 1, 4000, "Next plan"), hadAdverseEvent, details);
    }

    public string Hash() => PhysiotherapyText.Hash(new
        { SubjectiveResponse, Interventions, Tolerance, NextPlan, HadAdverseEvent, AdverseEventDetails });
}

internal static class PhysiotherapyText
{
    public static string Required(string value, int min, int max, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length < min || normalized.Length > max)
            throw new DomainRuleException($"{label} must contain between {min} and {max} characters.");
        return normalized;
    }

    public static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max)
            throw new DomainRuleException($"Clinical text cannot exceed {max} characters.");
        return normalized;
    }

    public static string Code(string value, int min, int max, string label) =>
        Required(value, min, max, label).ToUpperInvariant();

    public static string Hash(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
}
