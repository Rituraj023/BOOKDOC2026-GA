using PdfSharp.Fonts;

namespace BookDoc2026.DocumentService;

internal sealed class BookDocPdfFontResolver : IFontResolver
{
    public const string FamilyName = "BookDoc Open Sans";
    private const string FaceName = "BookDoc.OpenSans.Regular";
    private const string ResourceName = "BookDoc2026.DocumentService.Fonts.OpenSans-Regular.ttf";
    private static readonly object RegistrationLock = new();
    private static readonly Lazy<byte[]> FontBytes = new(LoadFont);

    public static void EnsureRegistered()
    {
        if (GlobalFontSettings.FontResolver is not null)
        {
            return;
        }

        lock (RegistrationLock)
        {
            GlobalFontSettings.FontResolver ??= new BookDocPdfFontResolver();
        }
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(FaceName, mustSimulateBold: isBold, mustSimulateItalic: isItalic);

    public byte[] GetFont(string faceName)
    {
        if (!string.Equals(faceName, FaceName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unknown embedded PDF font face '{faceName}'.");
        }

        return FontBytes.Value;
    }

    private static byte[] LoadFont()
    {
        using var stream = typeof(BookDocPdfFontResolver).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded PDF font resource '{ResourceName}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
