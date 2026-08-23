using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

internal static class BillingMoney
{
    public static decimal Amount(decimal value, string label, bool allowZero = false)
    {
        if (value < 0 || (!allowZero && value == 0) || value > 999_999_999.99m
            || decimal.Round(value, 2) != value)
            throw new DomainRuleException($"{label} must be {(allowZero ? "a non-negative" : "a positive")} amount with at most two decimal places.");
        return value;
    }

    public static string Currency(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 3 || normalized.Any(character => !char.IsLetter(character)))
            throw new DomainRuleException("Currency must be a three-letter code.");
        return normalized;
    }

    public static string Required(string value, int max, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 || normalized.Length > max)
            throw new DomainRuleException($"{label} is required and cannot exceed {max} characters.");
        return normalized;
    }

    public static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max) throw new DomainRuleException($"Text cannot exceed {max} characters.");
        return normalized;
    }
}
