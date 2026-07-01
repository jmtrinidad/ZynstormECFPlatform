using System.Globalization;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Result of attempting to extract a <see cref="LayoutDescriptor"/> from a PDF model.
/// </summary>
public record RiExtractionResult(LayoutDescriptor? Layout, List<string> Warnings, bool Success);

/// <summary>
/// Best-effort extractor that reads a PDF (typically an existing invoice/RI model exported
/// from another system) with PdfPig and derives a <see cref="LayoutDescriptor"/> by matching
/// a dictionary of known anchor labels (RNC, NCF, Fecha, table headers, totals, etc.).
/// This is heuristic: it aims to find "good enough" positions to seed a layout, not to
/// perfectly reconstruct the original design.
/// </summary>
public static class RiModelExtractor
{
    // Label -> field key in LayoutDescriptor.FieldSlots
    private static readonly (string[] Labels, string Field)[] FieldAnchors =
    [
        (["E-NCF", "NCF ELECTRONICO", "NCF"], "eNCF"),
        (["FECHA"], "fechaEmision"),
        (["CLIENTE", "RAZON SOCIAL"], "razonSocialComprador"),
        (["RNC/CEDULA", "RNC"], "rncComprador"),
        (["CODIGO DE SEGURIDAD"], "codigoSeguridad"),
        (["FECHA DE FIRMA"], "fechaFirma"),
    ];

    // Column header label -> item column field key
    private static readonly (string[] Labels, string Field)[] ColumnAnchors =
    [
        (["DESCRIPCION"], "descripcion"),
        (["CANTIDAD"], "cantidad"),
        (["PRECIO"], "precio"),
        (["ITBIS"], "itbis"),
        (["VALOR", "IMPORTE"], "valor"),
    ];

    // Totals label -> total row field key
    private static readonly (string[] Labels, string Field)[] TotalAnchors =
    [
        (["SUB-TOTAL", "SUBTOTAL"], "subTotal"),
        (["ITBIS"], "itbis"),
        (["TOTAL"], "total"),
    ];

    private const string QrAnchorLabel = "CODIGO DE SEGURIDAD";

    public static RiExtractionResult Extract(byte[] pdfBytes)
    {
        var warnings = new List<string>();

        using var pdf = PdfDocument.Open(pdfBytes);
        var page = pdf.GetPage(1);
        var words = page.GetWords().ToList();

        if (words.Count == 0)
        {
            return new RiExtractionResult(null, new()
            {
                "El PDF no contiene texto extraible (posible escaneo/imagen). Usa un PDF con texto seleccionable."
            }, false);
        }

        double width = page.Width;
        double height = page.Height;

        double NormX(double x) => x / width;
        double NormY(double topY) => (height - topY) / height;

        var layout = new LayoutDescriptor
        {
            Page = new PageInfo { WidthPt = width, HeightPt = height, Margin = 0.04 }
        };

        // 1) Field anchors (RNC, NCF, Fecha, Cliente, etc.)
        foreach (var (labels, field) in FieldAnchors)
        {
            var match = FindAnchor(words, labels);
            if (match is null)
            {
                warnings.Add($"No se detecto la ancla para el campo '{field}' ({string.Join("/", labels)}).");
                continue;
            }

            layout.FieldSlots[field] = new FieldSlot
            {
                X = NormX(match.BoundingBox.Right),
                Y = NormY(match.BoundingBox.Top),
                Label = match.Text
            };
        }

        // 2) Table header row -> item columns
        var (columns, headerTopY) = BuildColumns(words, NormX, NormY);
        layout.Items.Columns = columns;
        layout.Items.TopY = headerTopY;
        if (columns.Count == 0)
        {
            warnings.Add("No se detecto la fila de encabezados de la tabla de items.");
        }

        // 3) Totals (Sub-Total, ITBIS, Total)
        foreach (var (labels, field) in TotalAnchors)
        {
            var match = FindAnchor(words, labels);
            if (match is null)
            {
                warnings.Add($"No se detecto el total '{field}' ({string.Join("/", labels)}).");
                continue;
            }

            layout.Totals.Add(new TotalRow { Field = field, Label = match.Text });
        }

        // 4) Logo: largest image in the top area of the page (best effort).
        try
        {
            var images = page.GetImages().ToList();
            var topImage = images
                .Where(img => NormY(img.Bounds.Top) < 0.35)
                .OrderByDescending(img => img.Bounds.Width * img.Bounds.Height)
                .FirstOrDefault();

            if (topImage is not null)
            {
                layout.Logo = new LogoSlot
                {
                    X = NormX(topImage.Bounds.Left),
                    Y = NormY(topImage.Bounds.Top),
                    W = topImage.Bounds.Width / width,
                    H = topImage.Bounds.Height / height,
                    ImageRef = "logo1"
                };

                if (topImage.TryGetPng(out var png))
                {
                    layout.Images["logo1"] = Convert.ToBase64String(png);
                }
            }
        }
        catch
        {
            // Image extraction is best-effort; ignore failures (e.g. unsupported encodings).
            warnings.Add("No se pudo extraer el logo de la imagen del PDF.");
        }

        // 5) Palette: dominant font size (rough heuristic; PdfPig text color needs letters).
        var dominantFontSize = words
            .SelectMany(w => w.Letters)
            .GroupBy(l => Math.Round(l.GlyphRectangle.Height, 1))
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        layout.Palette["baseFontSize"] = dominantFontSize.ToString(CultureInfo.InvariantCulture);
        layout.Palette["primary"] = "#000000";

        // 6) QR default region unless we can anchor it to the "Codigo de Seguridad" label.
        var qrAnchor = FindAnchor(words, [QrAnchorLabel]);
        layout.Qr = qrAnchor is not null
            ? new QrSlot { X = NormX(qrAnchor.BoundingBox.Left), Y = NormY(qrAnchor.BoundingBox.Bottom), Size = 0.15 }
            : new QrSlot { X = 0.08, Y = 0.80, Size = 0.15 };

        if (qrAnchor is null)
        {
            warnings.Add("No se detecto la region del QR (Codigo de Seguridad); se uso posicion por defecto.");
        }

        return new RiExtractionResult(layout, warnings, true);
    }

    /// <summary>
    /// Finds the first word whose normalized text matches (exactly or via StartsWith,
    /// to tolerate trailing colons/values glued to the label) any of the candidate labels.
    /// </summary>
    private static Word? FindAnchor(List<Word> words, string[] labels)
    {
        var normalizedLabels = labels.Select(NormalizeText).ToArray();

        foreach (var word in words)
        {
            var normalized = NormalizeText(word.Text);
            foreach (var label in normalizedLabels)
            {
                if (normalized == label || normalized.StartsWith(label, StringComparison.Ordinal))
                {
                    return word;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to locate the table header row by finding words that match the known column
    /// anchor labels and sit roughly on the same horizontal line (min Y variance). Falls back
    /// to building one column per detected header word, ordered by X position.
    /// </summary>
    private static (List<ItemColumn> Columns, double TopY) BuildColumns(
        List<Word> words, Func<double, double> normX, Func<double, double> normY)
    {
        var headerWords = new List<(Word Word, string Field)>();

        foreach (var (labels, field) in ColumnAnchors)
        {
            var normalizedLabels = labels.Select(NormalizeText).ToArray();
            var match = words.FirstOrDefault(w =>
            {
                var normalized = NormalizeText(w.Text);
                return normalizedLabels.Any(l => normalized == l || normalized.StartsWith(l, StringComparison.Ordinal));
            });

            if (match is not null)
            {
                headerWords.Add((match, field));
            }
        }

        if (headerWords.Count == 0)
        {
            return (new List<ItemColumn>(), 0);
        }

        var orderedByX = headerWords.OrderBy(h => h.Word.BoundingBox.Left).ToList();
        var topY = normY(orderedByX.Max(h => h.Word.BoundingBox.Top));

        var columns = new List<ItemColumn>();
        for (int i = 0; i < orderedByX.Count; i++)
        {
            var (word, field) = orderedByX[i];
            double x = normX(word.BoundingBox.Left);
            double nextX = i + 1 < orderedByX.Count ? normX(orderedByX[i + 1].Word.BoundingBox.Left) : 1.0;
            double w = Math.Max(nextX - x, 0.01);

            columns.Add(new ItemColumn
            {
                Field = field,
                X = x,
                W = w,
                Align = field is "cantidad" or "precio" or "itbis" or "valor" ? "Right" : "Left"
            });
        }

        return (columns, topY);
    }

    /// <summary>
    /// Uppercases and strips diacritics so anchor matching is accent/case-insensitive
    /// (e.g. "Descripción" -> "DESCRIPCION").
    /// </summary>
    private static string NormalizeText(string text)
    {
        var trimmed = text.Trim().TrimEnd(':').Trim();
        var normalized = trimmed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
