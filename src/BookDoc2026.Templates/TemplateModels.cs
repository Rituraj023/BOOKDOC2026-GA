namespace BookDoc2026.Templates;

public enum TemplateChannel
{
    Email = 1,
    Sms = 2,
    WhatsApp = 3,
    Push = 4,
    Document = 5,
    Report = 6
}

public enum TemplateContentKind
{
    PlainText = 1,
    Html = 2
}

public sealed record TemplateScope(
    long TenantId,
    long? OrganizationId = null,
    long? BranchId = null);

public sealed record TemplateSelection(
    TemplateScope Scope,
    string Key,
    TemplateChannel Channel,
    string Culture,
    int? Version = null);

public sealed record TemplateDefinition(
    string Key,
    int Version,
    TemplateChannel Channel,
    string Culture,
    TemplateContentKind ContentKind,
    string BodyTemplate,
    string? SubjectTemplate = null);

public sealed record TemplateRenderRequest(
    TemplateDefinition Definition,
    IReadOnlyDictionary<string, string?> Values);

public sealed record RenderedTemplate(
    string Key,
    int Version,
    TemplateChannel Channel,
    string Culture,
    TemplateContentKind ContentKind,
    string? Subject,
    string Body);

public sealed class TemplateRenderException(string message) : InvalidOperationException(message);
