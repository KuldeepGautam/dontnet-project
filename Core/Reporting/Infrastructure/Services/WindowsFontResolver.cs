namespace UBIS.Services.Reporting.Infrastructure.Services;

using PdfSharp.Fonts;

/// <summary>
/// PdfSharp 6.x ships with no bundled fonts and no default GDI font lookup (even on Windows) —
/// every font used by MigraDoc, including its own internal fallback fonts, must go through an
/// explicit IFontResolver. Since every deployment target for this solution is Windows/IIS, this
/// resolver reads straight from %WINDIR%\Fonts rather than embedding font files or taking on a
/// cross-platform font package. Verdana is used everywhere (bold/italic requests fall back to it
/// too) since PdfReportGenerator only ever asks for one font family.
/// </summary>
public class WindowsFontResolver : IFontResolver
{
    private static readonly string FontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    public byte[] GetFont(string faceName)
    {
        var fileName = faceName switch
        {
            "Verdana#Bold" => "verdanab.ttf",
            "Verdana#Italic" => "verdanai.ttf",
            "Verdana#BoldItalic" => "verdanaz.ttf",
            _ => "verdana.ttf"
        };

        var path = Path.Combine(FontsDir, fileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : File.ReadAllBytes(Path.Combine(FontsDir, "verdana.ttf"));
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var faceName = (isBold, isItalic) switch
        {
            (true, true) => "Verdana#BoldItalic",
            (true, false) => "Verdana#Bold",
            (false, true) => "Verdana#Italic",
            _ => "Verdana"
        };
        return new FontResolverInfo(faceName);
    }
}
