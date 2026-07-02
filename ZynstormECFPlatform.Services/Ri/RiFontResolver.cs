using System.Reflection;
using PdfSharp.Fonts;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Supplies PdfSharp with an embedded TrueType font (DejaVu Sans) so PDF rendering
/// works headlessly, with no dependency on system/OS fonts being installed
/// (there are none on a bare WSL/Linux container or a locked-down Windows host).
/// </summary>
public sealed class RiFontResolver : IFontResolver
{
    /// <summary>Logical font family name PdfSharp callers should use, e.g. <c>new XFont(RiFontResolver.BaseFamily, 10)</c>.</summary>
    public const string BaseFamily = "RiBase";

    private const string RegularFaceName = "RiBase#Regular";
    private const string BoldFaceName = "RiBase#Bold";

    private const string RegularResourceName = "ZynstormECFPlatform.Services.Ri.Fonts.DejaVuSans.ttf";
    private const string BoldResourceName = "ZynstormECFPlatform.Services.Ri.Fonts.DejaVuSans-Bold.ttf";

    private static readonly object RegistrationLock = new();
    private static bool _registered;

    private static byte[]? _regularFontBytes;
    private static byte[]? _boldFontBytes;

    /// <summary>
    /// Registers this resolver as the global PdfSharp <see cref="GlobalFontSettings.FontResolver"/>.
    /// Safe to call multiple times/concurrently; only the first call takes effect.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        lock (RegistrationLock)
        {
            if (_registered)
            {
                return;
            }

            GlobalFontSettings.FontResolver = new RiFontResolver();
            _registered = true;
        }
    }

    public byte[] GetFont(string faceName)
    {
        return faceName switch
        {
            BoldFaceName => _boldFontBytes ??= LoadEmbeddedFont(BoldResourceName),
            _ => _regularFontBytes ??= LoadEmbeddedFont(RegularResourceName),
        };
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // DejaVu Sans has no dedicated italic embedded here; PdfSharp will
        // synthesize italics/slant from the regular/bold face as needed.
        var faceName = isBold ? BoldFaceName : RegularFaceName;
        return new FontResolverInfo(faceName);
    }

    private static byte[] LoadEmbeddedFont(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded font resource '{resourceName}' not found in assembly '{assembly.FullName}'. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
