using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using ZynstormECFPlatform.Services.Ri;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiFontResolverTests
{
    [Fact]
    public void EnsureRegistered_AllowsRenderingPdfWithEmbeddedFont_Headless()
    {
        // Arrange: register the embedded-font resolver so PdfSharp never touches
        // system fonts (there are none on a headless WSL/Linux box).
        RiFontResolver.EnsureRegistered();

        using var document = new PdfDocument();
        var page = document.AddPage();

        using (var gfx = XGraphics.FromPdfPage(page))
        {
            var font = new XFont(RiFontResolver.BaseFamily, 10);
            gfx.DrawString("Hola", font, XBrushes.Black, new XPoint(20, 20));
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        var bytes = stream.ToArray();

        // Assert: a real PDF came out, proving headless rendering works end to end.
        var header = Encoding.ASCII.GetString(bytes, 0, 4);
        Assert.Equal("%PDF", header);
        Assert.True(bytes.Length > 500, $"Expected a non-trivial PDF, got {bytes.Length} bytes.");
    }

    [Fact]
    public void EnsureRegistered_IsIdempotent_WhenCalledMultipleTimes()
    {
        RiFontResolver.EnsureRegistered();
        RiFontResolver.EnsureRegistered();
        RiFontResolver.EnsureRegistered();

        Assert.NotNull(PdfSharp.Fonts.GlobalFontSettings.FontResolver);
    }
}
