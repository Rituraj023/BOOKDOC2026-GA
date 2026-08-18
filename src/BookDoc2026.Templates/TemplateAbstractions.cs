namespace BookDoc2026.Templates;

public interface ITemplateCatalog
{
    ValueTask<TemplateDefinition?> ResolvePublishedAsync(
        TemplateSelection selection,
        CancellationToken cancellationToken = default);
}

public interface ITemplateRenderer
{
    RenderedTemplate Render(TemplateRenderRequest request);
}
