using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BookDoc2026.DocumentService;

public sealed class OpenXmlWordExporter : IDocumentExporter
{
    public DocumentOutputFormat Format => DocumentOutputFormat.WordOpenXml;

    public ValueTask<GeneratedDocument> ExportAsync(
        DocumentContent document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DocumentExportSupport.Validate(document);

        using var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = package.AddMainDocumentPart();
            var body = new Body();
            body.Append(CreateParagraph(document.Title, isHeading: true));

            foreach (var section in document.Sections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(section.Heading))
                {
                    body.Append(CreateParagraph(section.Heading, isHeading: true));
                }

                foreach (var paragraph in section.Paragraphs)
                {
                    body.Append(CreateParagraph(paragraph ?? string.Empty, isHeading: false));
                }
            }

            mainPart.Document = new Document(body);
            mainPart.Document.Save();
            package.PackageProperties.Title = document.Title;
            package.PackageProperties.Subject = document.Subject;
            package.PackageProperties.Creator = document.Author;
        }

        return ValueTask.FromResult(DocumentExportSupport.Complete(
            document,
            Format,
            ".docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            stream));
    }

    private static Paragraph CreateParagraph(string value, bool isHeading)
    {
        var runProperties = isHeading
            ? new RunProperties(new Bold(), new FontSize { Val = "28" })
            : null;
        var run = new Run();
        if (runProperties is not null)
        {
            run.Append(runProperties);
        }

        run.Append(new Text(value) { Space = SpaceProcessingModeValues.Preserve });
        return new Paragraph(run);
    }
}
