namespace BookDoc2026.DocumentService;

public sealed class DocumentGenerationService(IEnumerable<IDocumentExporter> exporters)
    : IDocumentGenerationService
{
    private readonly IReadOnlyDictionary<DocumentOutputFormat, IDocumentExporter> _exporters = exporters
        .GroupBy(exporter => exporter.Format)
        .ToDictionary(group => group.Key, group => group.Single());

    public ValueTask<GeneratedDocument> GenerateAsync(
        DocumentOutputFormat format,
        DocumentContent document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!_exporters.TryGetValue(format, out var exporter))
        {
            throw new NotSupportedException($"No document exporter is registered for '{format}'.");
        }

        return exporter.ExportAsync(document, cancellationToken);
    }
}
