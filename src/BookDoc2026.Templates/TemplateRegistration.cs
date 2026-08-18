using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookDoc2026.Templates;

public static class TemplateRegistration
{
    public static IServiceCollection AddBookDocTemplates(this IServiceCollection services)
    {
        services.TryAddSingleton<ITemplateRenderer, StrictTemplateRenderer>();
        services.TryAddSingleton<ITemplateCatalog, EmptyTemplateCatalog>();
        return services;
    }
}

internal sealed class EmptyTemplateCatalog : ITemplateCatalog
{
    public ValueTask<TemplateDefinition?> ResolvePublishedAsync(
        TemplateSelection selection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<TemplateDefinition?>(null);
    }
}
