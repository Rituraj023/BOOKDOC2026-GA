using BookDoc2026.Domain.Communications;

namespace BookDoc2026.Application.Communications;

public sealed record CommunicationPreferenceEvaluation(bool CanSend, string Code);

public static class CommunicationPreferenceEvaluator
{
    public static CommunicationPreferenceEvaluation Evaluate(
        CommunicationPreferenceEvent? latest,
        DateTimeOffset now)
    {
        if (latest is null) return new(false, "preference_not_recorded");
        if (latest.Decision == CommunicationPreferenceDecision.Denied)
            return new(false, "preference_denied");
        if (latest.Decision == CommunicationPreferenceDecision.Withdrawn)
            return new(false, "preference_withdrawn");
        if (latest.Decision != CommunicationPreferenceDecision.Allowed)
            return new(false, "preference_invalid");
        if (!latest.QuietHoursStart.HasValue) return new(true, "allowed");

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(latest.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return new(false, "preference_timezone_invalid");
        }
        catch (InvalidTimeZoneException)
        {
            return new(false, "preference_timezone_invalid");
        }

        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        var start = latest.QuietHoursStart.Value;
        var end = latest.QuietHoursEnd!.Value;
        var isQuiet = start < end
            ? localTime >= start && localTime < end
            : localTime >= start || localTime < end;
        return isQuiet
            ? new(false, "preference_quiet_hours")
            : new(true, "allowed");
    }
}
