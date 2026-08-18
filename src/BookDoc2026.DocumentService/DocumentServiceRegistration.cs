using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookDoc2026.DocumentService;

public static class DocumentServiceRegistration
{
    public static IServiceCollection AddBookDocDocumentService(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentExporter, OpenXmlWordExporter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentExporter, PdfDocumentExporter>());
        services.TryAddSingleton<IDocumentGenerationService, DocumentGenerationService>();
        return services;
    }
}
