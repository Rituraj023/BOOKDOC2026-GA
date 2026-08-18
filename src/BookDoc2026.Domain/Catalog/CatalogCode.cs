using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

internal static class CatalogCode
{
    public static string Normalize(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException($"{label} code is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 40 || normalized.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new DomainRuleException($"{label} code may contain only letters, digits, hyphen and underscore, up to 40 characters.");
        }

        return normalized;
    }

    public static string RequiredName(string value, string label, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            throw new DomainRuleException($"{label} name is required and cannot exceed {maxLength} characters.");
        }

        return value.Trim();
    }
}
