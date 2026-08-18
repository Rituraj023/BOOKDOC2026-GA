using System.Net;
using System.Text.RegularExpressions;

namespace BookDoc2026.Templates;

public sealed partial class StrictTemplateRenderer : ITemplateRenderer
{
    public RenderedTemplate Render(TemplateRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request.Definition);
        ArgumentNullException.ThrowIfNull(request.Values);

        var subject = request.Definition.SubjectTemplate is null
            ? null
            : Replace(request.Definition.SubjectTemplate, request.Values, htmlEncode: false);
        var body = Replace(
            request.Definition.BodyTemplate,
            request.Values,
            request.Definition.ContentKind == TemplateContentKind.Html);

        return new RenderedTemplate(
            request.Definition.Key,
            request.Definition.Version,
            request.Definition.Channel,
            request.Definition.Culture,
            request.Definition.ContentKind,
            subject,
            body);
    }

    private static string Replace(
        string template,
        IReadOnlyDictionary<string, string?> values,
        bool htmlEncode)
    {
        var missing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = PlaceholderPattern().Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            if (!values.TryGetValue(key, out var value))
            {
                missing.Add(key);
                return match.Value;
            }

            var normalized = value ?? string.Empty;
            return htmlEncode ? WebUtility.HtmlEncode(normalized) : normalized;
        });

        if (missing.Count > 0)
        {
            throw new TemplateRenderException(
                $"Template is missing required values: {string.Join(", ", missing)}.");
        }

        return result;
    }

    private static void Validate(TemplateDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.BodyTemplate);

        if (definition.Version <= 0)
        {
            throw new TemplateRenderException("Template version must be positive.");
        }

        if (definition.Key.Length > 160 || definition.Culture.Length > 20)
        {
            throw new TemplateRenderException("Template identity exceeds the supported length.");
        }
    }

    [GeneratedRegex(@"\{\{\s*(?<key>[A-Za-z][A-Za-z0-9_.-]*)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}
