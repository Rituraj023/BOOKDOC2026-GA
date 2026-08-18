using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace BookDoc2026.DocumentService;

public sealed class PdfDocumentExporter : IDocumentExporter
{
    private const double Margin = 42;
    private const double LineHeight = 16;

    public DocumentOutputFormat Format => DocumentOutputFormat.Pdf;

    public ValueTask<GeneratedDocument> ExportAsync(
        DocumentContent document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DocumentExportSupport.Validate(document);
        BookDocPdfFontResolver.EnsureRegistered();

        using var output = new MemoryStream();
        using (var pdf = new PdfDocument())
        {
            pdf.Info.Title = document.Title;
            pdf.Info.Subject = document.Subject ?? string.Empty;
            pdf.Info.Author = document.Author ?? string.Empty;

            var titleFont = new XFont(BookDocPdfFontResolver.FamilyName, 16, XFontStyleEx.Bold);
            var headingFont = new XFont(BookDocPdfFontResolver.FamilyName, 12, XFontStyleEx.Bold);
            var bodyFont = new XFont(BookDocPdfFontResolver.FamilyName, 10, XFontStyleEx.Regular);
            var page = pdf.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var y = Margin;

            DrawLine(document.Title, titleFont, ref page, ref graphics, ref y);
            y += LineHeight / 2;

            foreach (var section in document.Sections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(section.Heading))
                {
                    DrawLine(section.Heading, headingFont, ref page, ref graphics, ref y);
                }

                foreach (var paragraph in section.Paragraphs)
                {
                    foreach (var line in Wrap(paragraph ?? string.Empty, 96))
                    {
                        DrawLine(line, bodyFont, ref page, ref graphics, ref y);
                    }

                    y += LineHeight / 3;
                }
            }

            graphics.Dispose();
            pdf.Save(output, closeStream: false);
        }

        return ValueTask.FromResult(DocumentExportSupport.Complete(
            document,
            Format,
            ".pdf",
            "application/pdf",
            output));
    }

    private static void DrawLine(
        string value,
        XFont font,
        ref PdfPage page,
        ref XGraphics graphics,
        ref double y)
    {
        if (y + LineHeight > page.Height.Point - Margin)
        {
            graphics.Dispose();
            page = page.Owner.AddPage();
            graphics = XGraphics.FromPdfPage(page);
            y = Margin;
        }

        graphics.DrawString(
            value,
            font,
            XBrushes.Black,
            new XRect(Margin, y, page.Width.Point - (Margin * 2), LineHeight),
            XStringFormats.TopLeft);
        y += LineHeight;
    }

    private static IEnumerable<string> Wrap(string value, int maximumCharacters)
    {
        if (string.IsNullOrEmpty(value))
        {
            yield return string.Empty;
            yield break;
        }

        foreach (var sourceLine in value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var remaining = sourceLine.TrimEnd();
            while (remaining.Length > maximumCharacters)
            {
                var split = remaining.LastIndexOf(' ', maximumCharacters);
                split = split <= 0 ? maximumCharacters : split;
                yield return remaining[..split].TrimEnd();
                remaining = remaining[split..].TrimStart();
            }

            yield return remaining;
        }
    }
}
