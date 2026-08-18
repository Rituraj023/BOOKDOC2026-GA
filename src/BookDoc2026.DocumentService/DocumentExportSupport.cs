using System.Security.Cryptography;

namespace BookDoc2026.DocumentService;

internal static class DocumentExportSupport
{
    public static void Validate(DocumentContent document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.FileBaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.Title);
        ArgumentNullException.ThrowIfNull(document.Sections);

        foreach (var section in document.Sections)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(section.Paragraphs);
        }
    }

    public static GeneratedDocument Complete(
        DocumentContent document,
        DocumentOutputFormat format,
        string extension,
        string contentType,
        MemoryStream stream)
    {
        var bytes = stream.ToArray();
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        return new GeneratedDocument(
            $"{SafeFileBaseName(document.FileBaseName)}{extension}",
            contentType,
            format,
            bytes,
            hash);
    }

    private static string SafeFileBaseName(string value)
    {
        var normalized = new string(value
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                ? character
                : '-')
            .ToArray());
        normalized = normalized.Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "document" : normalized[..Math.Min(normalized.Length, 120)];
    }
}
