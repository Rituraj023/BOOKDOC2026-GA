namespace BookDoc2026.DocumentService;

public interface IDocumentExporter
{
    DocumentOutputFormat Format { get; }

    ValueTask<GeneratedDocument> ExportAsync(
        DocumentContent document,
        CancellationToken cancellationToken = default);
}

public interface IDocumentGenerationService
{
    ValueTask<GeneratedDocument> GenerateAsync(
        DocumentOutputFormat format,
        DocumentContent document,
        CancellationToken cancellationToken = default);
}
