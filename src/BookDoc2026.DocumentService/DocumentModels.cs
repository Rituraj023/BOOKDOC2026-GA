namespace BookDoc2026.DocumentService;

public enum DocumentOutputFormat
{
    WordOpenXml = 1,
    Pdf = 2
}

public sealed record DocumentSection(string? Heading, IReadOnlyList<string> Paragraphs);

public sealed record DocumentContent(
    string FileBaseName,
    string Title,
    IReadOnlyList<DocumentSection> Sections,
    string? Subject = null,
    string? Author = null);

public sealed record GeneratedDocument(
    string FileName,
    string ContentType,
    DocumentOutputFormat Format,
    ReadOnlyMemory<byte> Content,
    string Sha256);
