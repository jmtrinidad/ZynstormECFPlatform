using System.Globalization;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Result of attempting to extract a <see cref="PageModel"/> from a PDF model.
/// </summary>
public record RiExtractionResult(PageModel? Page, List<string> Warnings, bool Success);

/// <summary>
/// Best-effort extractor that reads a PDF (typically an existing invoice/RI model exported
/// from another system) with PdfPig and derives a <see cref="PageModel"/> by matching a
/// dictionary of known anchor labels (RNC, NCF, Fecha, table headers, totals, etc.), and
/// classifying every text run on the page as either static (kept verbatim on render) or
/// dynamic (a value slot to be replaced with e-CF data at render time).
/// This is heuristic: it aims to find "good enough" positions/classification, not to
/// perfectly reconstruct the original design.
/// </summary>
public static class RiModelExtractor
{
    // Label -> field key in PageModel.Fields (same vocabulary the renderer uses).
    // Labels may be multi-word phrases (e.g. "CODIGO DE SEGURIDAD"); matching joins
    // consecutive words on the same line before comparing.
    private static readonly (string[] Labels, string Field)[] HeaderFieldAnchors =
    [
        (["E-NCF", "NCF ELECTRONICO", "NCF"], "eNCF"),
        (["FECHA DE FIRMA"], "fechaFirma"),
        (["FECHA"], "fechaEmision"),
        (["RNC/CEDULA", "RNC"], "rncComprador"),
        (["CLIENTE", "RAZON SOCIAL"], "razonSocialComprador"),
        (["DIRECCION"], "direccionComprador"),
        (["TELEFONO"], "telefonoComprador"),
    ];

    // Totals-region anchors: searched only among words BELOW the item table (so the table
    // header's own "ITBIS"/"Total" column labels are never mistaken for these).
    private static readonly (string[] Labels, string Field)[] TotalsFieldAnchors =
    [
        (["CODIGO DE SEGURIDAD"], "codigoSeguridad"),
        (["SUB-TOTAL", "SUBTOTAL"], "subtotal"),
        (["EXENTO"], "exento"),
        (["ITBIS"], "itbis"),
        (["TOTAL"], "total"),
    ];

    // Column header label -> item column field key. TOTAL/VALOR/IMPORTE all map to "importe".
    private static readonly (string[] Labels, string Field)[] ColumnAnchors =
    [
        (["DESCRIPCION"], "descripcion"),
        (["CANTIDAD"], "cantidad"),
        (["PRECIO"], "precio"),
        (["ITBIS"], "itbis"),
        (["TOTAL", "VALOR", "IMPORTE"], "importe"),
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

        double TopY(Word w) => height - w.BoundingBox.Top;

        var pageModel = new PageModel
        {
            WidthPt = width,
            HeightPt = height
        };

        // Words already consumed as a FieldSlot VALUE, a table header, or a sample item row
        // are excluded from StaticRuns. The anchor LABEL words themselves stay static.
        var consumed = new HashSet<Word>();

        // 1) Table header row -> item columns, and the sample item row(s) beneath it
        // (values excluded from StaticRuns). Done first so totals anchors below the table
        // are never confused with the column headers above it (both use ITBIS/TOTAL).
        var (columns, headerWords, itemRowWords, headerTopY, headerBottom) = BuildColumns(words, TopY);
        pageModel.Items.Columns = columns;
        pageModel.Items.TopY = headerTopY;
        pageModel.Items.RowHeight = 14;

        if (columns.Count == 0)
        {
            warnings.Add("No se detecto la fila de encabezados de la tabla de items.");
        }

        foreach (var w in itemRowWords) consumed.Add(w);

        // 2) Header/buyer field anchors (RNC, NCF, Fecha, Cliente, etc.) -> FieldSlot at the
        // position of the VALUE run. Restricted to words at/above the item table (or all
        // words if no table was found) so column headers/rows are never matched here.
        var aboveTable = headerBottom.HasValue
            ? words.Where(w => w.BoundingBox.Bottom >= headerBottom.Value).ToList()
            : words;

        foreach (var (labels, field) in HeaderFieldAnchors)
        {
            TryAddFieldSlot(pageModel, aboveTable, labels, field, consumed, TopY, warnings);
        }

        // 3) Totals-region field anchors (Sub-Total, ITBIS, Total, Exento, Codigo de
        // Seguridad) -> searched only among words BELOW the item table.
        var belowTable = headerBottom.HasValue
            ? words.Where(w => w.BoundingBox.Top < headerBottom.Value).ToList()
            : words;

        foreach (var (labels, field) in TotalsFieldAnchors)
        {
            TryAddFieldSlot(pageModel, belowTable, labels, field, consumed, TopY, warnings);
        }

        // 4) Lines/boxes.
        try
        {
            foreach (var path in page.Paths)
            {
                foreach (var subpath in path)
                {
                    foreach (var command in subpath.Commands)
                    {
                        if (command is PdfSubpath.Line line)
                        {
                            pageModel.Lines.Add(new LineSeg
                            {
                                X1 = line.From.X,
                                Y1 = height - line.From.Y,
                                X2 = line.To.X,
                                Y2 = height - line.To.Y,
                                Thickness = 1
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            warnings.Add("No se pudieron extraer las lineas/recuadros del PDF.");
        }

        // 5) Images (logo, etc.), best effort.
        try
        {
            foreach (var img in page.GetImages())
            {
                if (img.TryGetPng(out var png))
                {
                    pageModel.Images.Add(new ImageEl
                    {
                        X = img.Bounds.Left,
                        Y = height - img.Bounds.Top,
                        W = img.Bounds.Width,
                        H = img.Bounds.Height,
                        Base64 = Convert.ToBase64String(png)
                    });
                }
            }
        }
        catch
        {
            warnings.Add("No se pudo extraer alguna imagen del PDF (formato no soportado).");
        }

        // 6) QR default region unless we can anchor it to the "Codigo de Seguridad" label.
        var qrAnchor = FindPhraseAnchor(words, [QrAnchorLabel], new HashSet<Word>());
        if (qrAnchor is not null)
        {
            pageModel.Qr = new QrSlot
            {
                X = qrAnchor.BoundingBox.Left,
                Y = TopY(qrAnchor) + 12,
                Size = Math.Min(width, height) * 0.15
            };
        }
        else
        {
            pageModel.Qr = new QrSlot { X = width * 0.08, Y = height * 0.80, Size = Math.Min(width, height) * 0.15 };
            warnings.Add("No se detecto la region del QR (Codigo de Seguridad); se uso posicion por defecto.");
        }

        // Header words are excluded from StaticRuns only once we no longer need them for
        // anchor matching above (their text is still the column header, drawn separately
        // isn't needed - they ARE static labels, so keep them out of "consumed" too).
        // (headerWords intentionally NOT added to consumed: table headers are static text.)
        _ = headerWords;

        // 7) StaticRuns = all runs minus consumed value/sample-item runs.
        foreach (var w in words)
        {
            if (consumed.Contains(w))
            {
                continue;
            }

            if (w.Letters.Count == 0)
            {
                continue;
            }

            var firstLetter = w.Letters[0];
            pageModel.StaticRuns.Add(new TextRun
            {
                Text = w.Text,
                X = w.BoundingBox.Left,
                Y = TopY(w),
                FontSize = firstLetter.GlyphRectangle.Height > 0 ? firstLetter.GlyphRectangle.Height : 10,
                Bold = firstLetter.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true,
                ColorHex = null
            });
        }

        pageModel.Warnings = warnings;

        return new RiExtractionResult(pageModel, warnings, true);
    }

    /// <summary>
    /// Finds the anchor label for <paramref name="field"/> within <paramref name="candidateWords"/>,
    /// then the nearby value run, and adds a <see cref="FieldSlot"/> (skips if already present).
    /// Only the VALUE word is marked consumed; the label stays in StaticRuns.
    /// </summary>
    private static void TryAddFieldSlot(
        PageModel pageModel, List<Word> candidateWords, string[] labels, string field,
        HashSet<Word> consumed, Func<Word, double> topY, List<string> warnings)
    {
        if (pageModel.Fields.Any(f => f.FieldKey == field))
        {
            return;
        }

        var anchor = FindPhraseAnchor(candidateWords, labels, consumed);
        if (anchor is null)
        {
            warnings.Add($"No se detecto la ancla para el campo '{field}' ({string.Join("/", labels)}).");
            return;
        }

        var valueRun = FindValueRun(candidateWords, anchor, consumed);

        double x;
        double y;
        double fontSize;

        if (valueRun is not null)
        {
            consumed.Add(valueRun);
            x = valueRun.BoundingBox.Left;
            y = topY(valueRun);
            fontSize = valueRun.Letters.Count > 0 ? valueRun.Letters[0].GlyphRectangle.Height : 10;
        }
        else
        {
            // No separate value token found (e.g. blank template) - place slot right after the label.
            x = anchor.BoundingBox.Right + 4;
            y = topY(anchor);
            fontSize = anchor.Letters.Count > 0 ? anchor.Letters[0].GlyphRectangle.Height : 10;
        }

        pageModel.Fields.Add(new FieldSlot
        {
            FieldKey = field,
            X = x,
            Y = y,
            FontSize = fontSize > 0 ? fontSize : 10,
            Align = "Left"
        });
    }

    /// <summary>
    /// Finds the first not-yet-consumed word (or run of up to 3 consecutive words on the same
    /// line, to support multi-word labels like "Codigo de Seguridad") whose normalized text
    /// matches (exactly or via StartsWith) any of the candidate labels. Returns the LAST word
    /// of the matched phrase (closest to where the value would follow).
    /// </summary>
    private static Word? FindPhraseAnchor(List<Word> words, string[] labels, HashSet<Word> consumed)
    {
        var normalizedLabels = labels.Select(NormalizeText).ToArray();

        for (int i = 0; i < words.Count; i++)
        {
            var word = words[i];
            if (consumed.Contains(word))
            {
                continue;
            }

            // Try growing phrases of 1..3 words starting at i, joined with a space,
            // as long as they stay on the same line (small Y tolerance).
            var phraseWords = new List<Word> { word };
            var phraseText = NormalizeText(word.Text);

            if (MatchesAny(phraseText, normalizedLabels))
            {
                return word;
            }

            for (int extra = 1; extra <= 2 && i + extra < words.Count; extra++)
            {
                var next = words[i + extra];
                if (Math.Abs(next.BoundingBox.Bottom - word.BoundingBox.Bottom) > 3.0)
                {
                    break;
                }

                phraseWords.Add(next);
                phraseText = NormalizeText(string.Join(" ", phraseWords.Select(w => w.Text)));

                if (MatchesAny(phraseText, normalizedLabels))
                {
                    return next;
                }
            }
        }

        return null;
    }

    private static bool MatchesAny(string normalized, string[] normalizedLabels) =>
        normalizedLabels.Any(l => normalized == l || normalized.StartsWith(l, StringComparison.Ordinal));

    /// <summary>
    /// Given a label anchor word, finds the nearby value run: prefers a word to the right on
    /// the same line (small Y difference), falling back to the nearest word below it.
    /// Skips words already consumed and words that are themselves known anchor labels.
    /// </summary>
    private static Word? FindValueRun(List<Word> words, Word anchor, HashSet<Word> consumed)
    {
        const double sameLineTolerance = 3.0;

        var candidatesSameLine = words
            .Where(w => w != anchor && !consumed.Contains(w))
            .Where(w => Math.Abs(w.BoundingBox.Bottom - anchor.BoundingBox.Bottom) <= sameLineTolerance)
            .Where(w => w.BoundingBox.Left >= anchor.BoundingBox.Right - 1)
            .Where(w => !IsKnownAnchorLabel(w))
            .OrderBy(w => w.BoundingBox.Left)
            .ToList();

        if (candidatesSameLine.Count > 0)
        {
            return candidatesSameLine[0];
        }

        // Fall back to the closest word directly below the anchor (labels stacked vertically).
        var candidatesBelow = words
            .Where(w => w != anchor && !consumed.Contains(w))
            .Where(w => w.BoundingBox.Top < anchor.BoundingBox.Bottom)
            .Where(w => Math.Abs(w.BoundingBox.Left - anchor.BoundingBox.Left) <= 5.0)
            .Where(w => !IsKnownAnchorLabel(w))
            .OrderByDescending(w => w.BoundingBox.Top)
            .ToList();

        return candidatesBelow.Count > 0 ? candidatesBelow[0] : null;
    }

    private static bool IsKnownAnchorLabel(Word word)
    {
        var normalized = NormalizeText(word.Text);
        return HeaderFieldAnchors.Any(a => a.Labels.Select(NormalizeText).Any(l => normalized == l || normalized.StartsWith(l, StringComparison.Ordinal)))
            || TotalsFieldAnchors.Any(a => a.Labels.Select(NormalizeText).Any(l => normalized == l || normalized.StartsWith(l, StringComparison.Ordinal)))
            || ColumnAnchors.Any(a => a.Labels.Select(NormalizeText).Any(l => normalized == l || normalized.StartsWith(l, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Attempts to locate the table header row by finding words that match the known column
    /// anchor labels and sit roughly on the same horizontal line (min Y variance). Also
    /// captures the first data row beneath the header (the sample item row) so its VALUES can
    /// be excluded from StaticRuns (the header words themselves remain static).
    /// </summary>
    private static (List<ItemColumn> Columns, List<Word> HeaderWords, List<Word> ItemRowWords, double TopY, double? HeaderBottom) BuildColumns(
        List<Word> words, Func<Word, double> topY)
    {
        var headerWords = new List<(Word Word, string Field)>();

        foreach (var (labels, field) in ColumnAnchors)
        {
            if (headerWords.Any(h => h.Field == field))
            {
                continue;
            }

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
            return (new List<ItemColumn>(), new List<Word>(), new List<Word>(), 0, null);
        }

        var orderedByX = headerWords.OrderBy(h => h.Word.BoundingBox.Left).ToList();
        var headerBottom = orderedByX.Min(h => h.Word.BoundingBox.Bottom);
        var topYValue = topY(orderedByX.OrderByDescending(h => h.Word.BoundingBox.Top).First().Word);

        var columns = new List<ItemColumn>();
        for (int i = 0; i < orderedByX.Count; i++)
        {
            var (word, field) = orderedByX[i];
            double x = word.BoundingBox.Left;
            double nextX = i + 1 < orderedByX.Count ? orderedByX[i + 1].Word.BoundingBox.Left : x + 80;
            double w = Math.Max(nextX - x, 20);

            columns.Add(new ItemColumn
            {
                Field = field,
                X = x,
                Width = w,
                Align = field is "cantidad" or "precio" or "itbis" or "importe" ? "Right" : "Left"
            });
        }

        // Sample item row: words below the header row, on the first line encountered
        // (closest Y below headerBottom).
        var belowHeader = words
            .Where(w => w.BoundingBox.Top < headerBottom)
            .OrderByDescending(w => w.BoundingBox.Top)
            .ToList();

        var itemRowWords = new List<Word>();
        if (belowHeader.Count > 0)
        {
            var firstRowTop = belowHeader[0].BoundingBox.Top;
            const double rowTolerance = 3.0;
            itemRowWords = belowHeader
                .Where(w => Math.Abs(w.BoundingBox.Top - firstRowTop) <= rowTolerance)
                .ToList();
        }

        return (columns, headerWords.Select(h => h.Word).ToList(), itemRowWords, topYValue, headerBottom);
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
