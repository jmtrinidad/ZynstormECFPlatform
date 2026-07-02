using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Renders a Ri (Representación Impresa) PDF by repainting, from scratch, the positioned
/// content extracted from a PDF model (<see cref="PageModel"/>) combined with the e-CF
/// content (<see cref="RiData"/>). Static content (labels, header, table headers, footer,
/// lines, images) is drawn verbatim at its extracted position; dynamic slots (Fields,
/// Items, Qr) are drawn with the corresponding value from <paramref name="data"/>.
/// Uses PDFsharp for absolute-coordinate drawing (QuestPDF, flow-based, is not suitable here
/// and remains only for <c>ReportPdfGenerator</c>).
/// </summary>
public static class RiPdfRenderer
{
    private static readonly CultureInfo DominicanCulture = new("es-DO");

    public static byte[] Render(PageModel page, RiData data)
    {
        page ??= new PageModel();
        data ??= new RiData();

        RiFontResolver.EnsureRegistered();

        using var document = new PdfDocument();
        var pdfPage = document.AddPage();
        pdfPage.Width = XUnit.FromPoint(page.WidthPt > 0 ? page.WidthPt : 612);
        pdfPage.Height = XUnit.FromPoint(page.HeightPt > 0 ? page.HeightPt : 792);

        using (var gfx = XGraphics.FromPdfPage(pdfPage))
        {
            DrawImages(gfx, page);
            DrawLines(gfx, page);
            DrawStaticRuns(gfx, page);
            DrawFields(gfx, page, data);
            DrawItems(gfx, page, data);
            DrawQr(gfx, page, data);
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static void DrawStaticRuns(XGraphics gfx, PageModel page)
    {
        foreach (var run in page.StaticRuns)
        {
            if (string.IsNullOrEmpty(run.Text))
            {
                continue;
            }

            var font = new XFont(RiFontResolver.BaseFamily, run.FontSize > 0 ? run.FontSize : 9,
                run.Bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
            var brush = ResolveBrush(run.ColorHex);

            gfx.DrawString(run.Text, font, brush, new XPoint(run.X, run.Y));
        }
    }

    private static void DrawLines(XGraphics gfx, PageModel page)
    {
        foreach (var line in page.Lines)
        {
            var pen = new XPen(XColors.Black, line.Thickness > 0 ? line.Thickness : 1);
            gfx.DrawLine(pen, line.X1, line.Y1, line.X2, line.Y2);
        }
    }

    private static void DrawImages(XGraphics gfx, PageModel page)
    {
        foreach (var img in page.Images)
        {
            if (string.IsNullOrEmpty(img.Base64))
            {
                continue;
            }

            try
            {
                var bytes = Convert.FromBase64String(img.Base64);
                using var ms = new MemoryStream(bytes);
                using var xImage = XImage.FromStream(ms);
                gfx.DrawImage(xImage, img.X, img.Y, img.W > 0 ? img.W : xImage.PointWidth, img.H > 0 ? img.H : xImage.PointHeight);
            }
            catch
            {
                // Best-effort: skip images that can't be decoded by PdfSharp.
            }
        }
    }

    private static void DrawFields(XGraphics gfx, PageModel page, RiData data)
    {
        foreach (var field in page.Fields)
        {
            var value = FieldValue(data, field.FieldKey);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            var font = new XFont(RiFontResolver.BaseFamily, field.FontSize > 0 ? field.FontSize : 9, XFontStyleEx.Regular);
            DrawAligned(gfx, value, font, XBrushes.Black, field.X, field.Y, field.Align, field.MaxWidth);
        }
    }

    private static void DrawItems(XGraphics gfx, PageModel page, RiData data)
    {
        var columns = page.Items.Columns;
        if (columns.Count == 0 || data.Items.Count == 0)
        {
            return;
        }

        var rowHeight = page.Items.RowHeight > 0 ? page.Items.RowHeight : 14;
        var font = new XFont(RiFontResolver.BaseFamily, 8, XFontStyleEx.Regular);

        for (int i = 0; i < data.Items.Count; i++)
        {
            var item = data.Items[i];
            var y = page.Items.TopY + i * rowHeight;

            foreach (var col in columns)
            {
                var value = ItemColumnValue(item, col.Field);
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                DrawAligned(gfx, value, font, XBrushes.Black, col.X, y, col.Align, col.Width);
            }
        }
    }

    private static void DrawQr(XGraphics gfx, PageModel page, RiData data)
    {
        var qr = page.Qr;
        var size = qr.Size > 0 ? qr.Size : 90;

        DrawQrModules(gfx, data.QrUrl, qr.X, qr.Y, size);

        var font = new XFont(RiFontResolver.BaseFamily, 8, XFontStyleEx.Regular);
        gfx.DrawString($"Código de Seguridad: {data.SecurityCode}", font, XBrushes.Black, new XPoint(qr.X, qr.Y + size + 12));
    }

    /// <summary>
    /// Draws the QR as vector-filled squares (one per module) rather than rasterizing to a
    /// PNG and decoding it through <see cref="XImage"/>: QRCoder emits a 1-bit grayscale PNG
    /// that PdfSharp's PNG importer cannot decode ("Unsupported image format"), so we read the
    /// module matrix directly instead.
    /// </summary>
    private static void DrawQrModules(XGraphics gfx, string? url, double x, double y, double size)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(url ?? string.Empty, QRCodeGenerator.ECCLevel.M);
        var matrix = qrData.ModuleMatrix;
        var moduleCount = matrix.Count;

        if (moduleCount == 0)
        {
            return;
        }

        var moduleSize = size / moduleCount;

        gfx.DrawRectangle(XBrushes.White, x, y, size, size);

        for (int row = 0; row < moduleCount; row++)
        {
            for (int col = 0; col < moduleCount; col++)
            {
                if (matrix[row][col])
                {
                    gfx.DrawRectangle(XBrushes.Black,
                        x + col * moduleSize, y + row * moduleSize,
                        moduleSize, moduleSize);
                }
            }
        }
    }

    private static void DrawAligned(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y, string? align, double maxWidth)
    {
        if (string.Equals(align, "Right", StringComparison.OrdinalIgnoreCase) && maxWidth > 0)
        {
            var size = gfx.MeasureString(text, font);
            var offsetX = Math.Max(0, maxWidth - size.Width);
            gfx.DrawString(text, font, brush, new XPoint(x + offsetX, y));
        }
        else if (string.Equals(align, "Center", StringComparison.OrdinalIgnoreCase) && maxWidth > 0)
        {
            var size = gfx.MeasureString(text, font);
            var offsetX = Math.Max(0, (maxWidth - size.Width) / 2);
            gfx.DrawString(text, font, brush, new XPoint(x + offsetX, y));
        }
        else
        {
            gfx.DrawString(text, font, brush, new XPoint(x, y));
        }
    }

    private static XBrush ResolveBrush(string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            return XBrushes.Black;
        }

        try
        {
            var color = XColor.FromArgb(int.Parse(colorHex.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            return new XSolidBrush(color);
        }
        catch
        {
            return XBrushes.Black;
        }
    }

    // Field keys are the lowercase-Spanish vocabulary emitted by RiModelExtractor:
    // eNCF, fechaEmision, fechaFirma, rncComprador, razonSocialComprador,
    // direccionComprador, telefonoComprador, subtotal, itbis, total, exento, codigoSeguridad.
    // Match case-insensitively so both extracted and hand-built PageModels agree.
    private static string FieldValue(RiData data, string fieldKey) => (fieldKey ?? string.Empty).ToLowerInvariant() switch
    {
        "encf" => data.ENcf,
        "fechaemision" => data.FechaEmision,
        "fechafirma" => data.FechaFirma,
        "rnccomprador" => data.Buyer.Document,
        "razonsocialcomprador" => data.Buyer.Name,
        "direccioncomprador" => data.Buyer.Address,
        "telefonocomprador" => data.Buyer.Phone,
        "subtotal" => Money(data.Totals.SubTotal),
        "itbis" => Money(data.Totals.Itbis),
        "total" => Money(data.Totals.Total),
        "exento" => Money(data.Totals.Exento),
        "codigoseguridad" => data.SecurityCode,
        _ => string.Empty
    };

    // Item column keys: descripcion, cantidad, precio, itbis, valor, importe
    // (TOTAL/VALOR/IMPORTE header all map to column key "importe").
    private static string ItemColumnValue(RiItem item, string field) => (field ?? string.Empty).ToLowerInvariant() switch
    {
        "descripcion" => item.Description,
        "cantidad" => item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
        "precio" => Money(item.Price),
        "itbis" => Money(item.Itbis),
        "valor" or "importe" => Money(item.Amount),
        _ => string.Empty
    };

    private static string Money(decimal value) => string.Format(DominicanCulture, "{0:N2}", value);
}
