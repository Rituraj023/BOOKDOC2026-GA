using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public static class SchedulingAvailabilityEvaluator
{
    public static string? GetUnavailableReason(BookableResource resource, long serviceId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, int requested, int reserved,
        IReadOnlyCollection<AvailabilityRule> rules, IReadOnlyCollection<AvailabilityException> exceptions,
        out int effectiveCapacity)
    {
        effectiveCapacity = resource.Capacity;
        if (!resource.IsActive || resource.OperationalStatus != ResourceOperationalStatus.Available) return "Resource is not operationally available.";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(resource.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(startUtc, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(endUtc, timeZone);
        if (localStart.Date != localEnd.Date) return "The requested interval crosses a local calendar day.";
        var date = DateOnly.FromDateTime(localStart.DateTime);
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime = TimeOnly.FromDateTime(localEnd.DateTime);
        var matching = rules.Where(rule => rule.ResourceId == resource.Id && rule.IsActive
            && (rule.ServiceId is null || rule.ServiceId == serviceId) && rule.DayOfWeek == localStart.DayOfWeek
            && rule.EffectiveFrom <= date && (rule.EffectiveTo is null || rule.EffectiveTo >= date)
            && rule.LocalStart <= startTime && rule.LocalEnd >= endTime
            && (startTime.ToTimeSpan() - rule.LocalStart.ToTimeSpan()).TotalMinutes % rule.SlotIntervalMinutes == 0).ToArray();
        if (matching.Length == 0) return "No availability rule covers the requested interval.";
        effectiveCapacity = Math.Min(resource.Capacity, matching.Max(rule => rule.Capacity));
        var overlapping = exceptions.Where(item => item.ResourceId == resource.Id && item.StartUtc < endUtc && item.EndUtc > startUtc).ToArray();
        if (overlapping.Any(item => item.Kind == AvailabilityExceptionKind.Unavailable)) return "An availability exception blocks the requested interval.";
        var overrides = overlapping.Where(item => item.Kind == AvailabilityExceptionKind.CapacityOverride).Select(item => item.CapacityOverride!.Value).ToArray();
        if (overrides.Length > 0) effectiveCapacity = Math.Min(effectiveCapacity, overrides.Min());
        return reserved + requested <= effectiveCapacity ? null : "Requested capacity is no longer available.";
    }
}
