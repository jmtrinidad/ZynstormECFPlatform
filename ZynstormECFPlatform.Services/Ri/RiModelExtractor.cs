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
        (["E-NCF", "E NCF", "ENCF", "NCF ELECTRONICO", "NCF"], "eNCF"),
        (["FECHA DE FIRMA"], "fechaFirma"),
        (["FECHA"], "fechaEmision"),
        (["RNC/CEDULA", "RNC/CED", "CEDULA"], "rncComprador"),
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

    // Labels for receipt/invoice fields that are NOT e-CF data fields, but whose sample
    // VALUES on a real-world model PDF must still be blanked (excluded from StaticRuns)
    // because they don't correspond to anything we control at render time. The label text
    // itself remains static; only the value run(s) to its right are removed.
    private static readonly string[] BlankValueLabels =
    [
        "VALIDO HASTA",
        "FACTURA",
        "NUMERO",
        "TIPO DE PAGO",
        "TIPO PAGO",
        "CONDICION DE PAGO",
        "COND. PAGO",
        "COND PAGO",
        "CAJERO",
        "ATENDIDO POR",
        "ARTICULOS",
        "CONTADO",
        "EFECTIVO",
        "TOTAL RECIBIDO",
        "RECIBIDO",
        "CAMBIO",
        "SU CAMBIO",
        "VUELTO",
        "DESCUENTO",
    ];

    // Words normalizing to any of these (joined on the same line) form the
    // "FACTURA ... FISCAL/CREDITO/ELECTRONICA" banner that marks the bottom edge of the
    // fully-static emisor header block.
    private static readonly string[] BannerRequiredTokens = ["FACTURA"];
    private static readonly string[] BannerAnyTokens = ["FISCAL", "CREDITO", "ELECTRONICA"];

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

        // Words already consumed as a FieldSlot VALUE, a blanked sample value, a table
        // header, or a sample item row are excluded from StaticRuns. The anchor LABEL
        // words themselves stay static.
        var consumed = new HashSet<Word>();

        // 0) Locate the emisor banner ("FACTURA ... FISCAL/CREDITO/ELECTRONICA"). Everything
        // with Y strictly above the banner's line (i.e. higher on the page - larger
        // BoundingBox.Top, since PdfPig's origin is bottom-left) belongs to the fully-static
        // emisor header (name, RNC, address, Tel, WA) and must never be touched by
        // value-exclusion logic below.
        var bannerBottom = FindBannerBottom(words);

        // 1) Table header row -> item columns, and the FULL item band beneath it down to the
        // first totals label (SUB-TOTAL/SUBTOTAL), so every sample item row/sub-line is
        // excluded from StaticRuns. Done first so totals anchors below the table are never
        // confused with the column headers above it (both use ITBIS/TOTAL).
        var (columns, headerWords, itemBandWords, headerTopY, headerBottom) = BuildColumns(words, TopY);
        pageModel.Items.Columns = columns;
        pageModel.Items.TopY = headerTopY;
        pageModel.Items.RowHeight = 14;

        if (columns.Count == 0)
        {
            warnings.Add("No se detecto la fila de encabezados de la tabla de items.");
        }

        foreach (var w in itemBandWords) consumed.Add(w);

        // 2) Header/buyer field anchors (RNC, NCF, Fecha, Cliente, etc.) -> FieldSlot at the
        // position of the FIRST VALUE run to the right of the label; ALL value runs on that
        // label's line are excluded from StaticRuns. Restricted to words at/above the item
        // table (or all words if no table was found) so column headers/rows are never matched
        // here. The emisor header (above the banner) is excluded so e.g. the emisor's bare
        // "RNC.:" can never be mistaken for the buyer's "RNC/CED:".
        var aboveTable = headerBottom.HasValue
            ? words.Where(w => w.BoundingBox.Bottom >= headerBottom.Value).ToList()
            : words;

        var searchableForFields = bannerBottom.HasValue
            ? aboveTable.Where(w => w.BoundingBox.Top < bannerBottom.Value).ToList()
            : aboveTable;

        foreach (var (labels, field) in HeaderFieldAnchors)
        {
            TryAddFieldSlot(pageModel, searchableForFields, labels, field, consumed, TopY, warnings);
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

        // 4) Blank non-e-CF sample values (receipt-style labels like "CAJERO:", "CONTADO:",
        // "SU CAMBIO:", the invoice-number "FACTURA:" line, etc.). Only below the emisor
        // banner - the emisor header itself is never touched. No FieldSlot is created; the
        // label stays static and its value run(s) are simply excluded.
        var searchableForBlanks = bannerBottom.HasValue
            ? words.Where(w => w.BoundingBox.Top < bannerBottom.Value).ToList()
            : words;

        foreach (var label in BlankValueLabels)
        {
            BlankLabelValues(searchableForBlanks, [label], consumed);
        }

        // 5) Lines/boxes.
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

        // 6) Images (logo, etc.), best effort.
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

        // 7) QR position: prefer anchoring to the "Codigo de Seguridad" label if present.
        // Otherwise place it in the largest vertical gap between text lines in the lower
        // ~60% of the page (a blank model has no QR to anchor to), falling back to
        // bottom-center above the footer greeting.
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
            pageModel.Qr = FindDefaultQrSlot(words, width, height, TopY);
            warnings.Add("No se detecto la region del QR (Codigo de Seguridad); se uso posicion por defecto.");
        }

        // Header words are excluded from StaticRuns only once we no longer need them for
        // anchor matching above (their text is still the column header, drawn separately
        // isn't needed - they ARE static labels, so keep them out of "consumed" too).
        // (headerWords intentionally NOT added to consumed: table headers are static text.)
        _ = headerWords;

        // 8) StaticRuns = all runs minus consumed value/sample-item runs.
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
    /// Finds the bottom edge (in PdfPig's bottom-left-origin Y - i.e. the lowest
    /// BoundingBox.Bottom of the matched line's words) of the "FACTURA ... FISCAL/CREDITO/
    /// ELECTRONICA" banner line, if present. Words with BoundingBox.Top >= this value sit at
    /// or above the banner and belong to the fully-static emisor header.
    /// </summary>
    private static double? FindBannerBottom(List<Word> words)
    {
        // Group words into visual lines (same Bottom within tolerance), then check whether
        // the joined, normalized line text contains "FACTURA" plus at least one of
        // FISCAL/CREDITO/ELECTRONICA (they may be split across two stacked lines in some
        // layouts, so we also check 2-line windows).
        var byLine = words
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom))
            .OrderByDescending(g => g.Key)
            .ToList();

        for (int i = 0; i < byLine.Count; i++)
        {
            var lineText = NormalizeText(string.Join(" ", byLine[i].OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
            var combinedText = lineText;
            var combinedBottom = byLine[i].Min(w => w.BoundingBox.Bottom);

            if (i + 1 < byLine.Count)
            {
                var nextLineText = NormalizeText(string.Join(" ", byLine[i + 1].OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
                combinedText = lineText + " " + nextLineText;
            }

            if (BannerRequiredTokens.All(t => combinedText.Contains(t)) &&
                BannerAnyTokens.Any(t => combinedText.Contains(t)))
            {
                // Use the lower of the (up to two) lines involved as the banner bottom.
                var bottom = i + 1 < byLine.Count && NormalizeText(string.Join(" ", byLine[i + 1].Select(w => w.Text))).Length > 0
                    ? Math.Min(combinedBottom, byLine[i + 1].Min(w => w.BoundingBox.Bottom))
                    : combinedBottom;
                return bottom;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the largest vertical gap between consecutive text lines in the lower ~60% of the
    /// page and centers a QR slot horizontally within it. Falls back to bottom-center above
    /// the last (footer) line if no usable gap is found.
    /// </summary>
    private static QrSlot FindDefaultQrSlot(List<Word> words, double width, double height, Func<Word, double> topY)
    {
        const double minSize = 40;
        double defaultSize = Math.Max(Math.Min(width, height) * 0.15, minSize);

        var lineYs = words
            .Select(w => Math.Round(topY(w)))
            .Distinct()
            .OrderBy(y => y)
            .ToList();

        double lowerBoundY = height * 0.40; // lower ~60% of the page starts here (Y grows downward)

        var candidateYs = lineYs.Where(y => y >= lowerBoundY).ToList();

        if (candidateYs.Count >= 2)
        {
            double bestGap = -1;
            double bestGapStart = 0;
            double bestGapEnd = 0;

            for (int i = 0; i < candidateYs.Count - 1; i++)
            {
                var gap = candidateYs[i + 1] - candidateYs[i];
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestGapStart = candidateYs[i];
                    bestGapEnd = candidateYs[i + 1];
                }
            }

            if (bestGap >= minSize + 6)
            {
                double size = Math.Max(Math.Min(bestGap - 6, defaultSize * 1.3), minSize);
                double centerY = (bestGapStart + bestGapEnd) / 2.0;
                return new QrSlot
                {
                    X = (width - size) / 2.0,
                    Y = centerY - size / 2.0,
                    Size = size
                };
            }
        }

        // Fallback: bottom-center, above the footer (last text line) if we know where it is.
        double footerY = lineYs.Count > 0 ? lineYs[^1] : height * 0.95;
        double fallbackSize = defaultSize;
        double fallbackY = Math.Max(footerY - fallbackSize - 10, height * 0.55);

        return new QrSlot
        {
            X = (width - fallbackSize) / 2.0,
            Y = fallbackY,
            Size = fallbackSize
        };
    }

    /// <summary>
    /// Finds the anchor label for <paramref name="field"/> within <paramref name="candidateWords"/>,
    /// then ALL value run(s) to the right of it on the same line, and adds a
    /// <see cref="FieldSlot"/> positioned at the PRIMARY value run - the first run that looks
    /// like actual data (contains a letter or digit) rather than a decorative currency symbol
    /// glued between the label and the value (e.g. a standalone "RD$:" token) - skips if
    /// already present. All matched value words are marked consumed; the label stays in
    /// StaticRuns.
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

        var valueRuns = FindValueRuns(candidateWords, anchor, consumed);

        double x;
        double y;
        double fontSize;

        if (valueRuns.Count > 0)
        {
            var primary = valueRuns.FirstOrDefault(IsDataToken) ?? valueRuns[0];
            foreach (var v in valueRuns) consumed.Add(v);
            x = primary.BoundingBox.Left;
            y = topY(primary);
            fontSize = primary.Letters.Count > 0 ? primary.Letters[0].GlyphRectangle.Height : 10;
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
    /// Currency-prefix tokens that sometimes appear as their own separate word between a
    /// label and its actual value (e.g. "RD$:", "RD$", "US$") - these must never be picked as
    /// the FieldSlot's anchor position, even though they contain letters.
    /// </summary>
    private static readonly string[] CurrencyPrefixTokens = ["RD$", "RD$:", "US$", "US$:", "$", "$:"];

    /// <summary>
    /// True if the word looks like real data (contains a digit, or is a letter-only token that
    /// isn't a bare currency-prefix placeholder) rather than a purely decorative/punctuation
    /// token (e.g. "RD$:", "$", ":") glued between a label and its actual value.
    /// </summary>
    private static bool IsDataToken(Word word)
    {
        var trimmed = word.Text.Trim();
        if (CurrencyPrefixTokens.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return trimmed.Any(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Finds a "blank value" label (not an e-CF field) and excludes ALL its right-side value
    /// run(s) from StaticRuns. The label itself stays static; no FieldSlot is created.
    /// </summary>
    private static void BlankLabelValues(List<Word> candidateWords, string[] labels, HashSet<Word> consumed)
    {
        var anchor = FindPhraseAnchor(candidateWords, labels, consumed);
        if (anchor is null)
        {
            return;
        }

        var valueRuns = FindValueRuns(candidateWords, anchor, consumed);
        foreach (var v in valueRuns) consumed.Add(v);
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
    /// Given a label anchor word, finds ALL value runs to its right on the same visual line
    /// (within <paramref name="baselineTolerance"/> points on the baseline - handles slightly
    /// different font sizes/baselines between label and value), ordered left-to-right. Falls
    /// back to the nearest single word below the anchor (labels stacked vertically) if none
    /// are found on the same line. Skips words already consumed and words that are themselves
    /// known anchor labels (so the NEXT label on the page is never swallowed as a value).
    /// </summary>
    private static List<Word> FindValueRuns(List<Word> words, Word anchor, HashSet<Word> consumed, double baselineTolerance = 5.0)
    {
        var candidatesSameLine = words
            .Where(w => w != anchor && !consumed.Contains(w))
            .Where(w => Math.Abs(w.BoundingBox.Bottom - anchor.BoundingBox.Bottom) <= baselineTolerance)
            .Where(w => w.BoundingBox.Left >= anchor.BoundingBox.Right - 1)
            .Where(w => !IsKnownAnchorLabel(w))
            .OrderBy(w => w.BoundingBox.Left)
            .ToList();

        if (candidatesSameLine.Count > 0)
        {
            return candidatesSameLine;
        }

        // Fall back to the closest word directly below the anchor (labels stacked vertically).
        var candidatesBelow = words
            .Where(w => w != anchor && !consumed.Contains(w))
            .Where(w => w.BoundingBox.Top < anchor.BoundingBox.Bottom)
            .Where(w => Math.Abs(w.BoundingBox.Left - anchor.BoundingBox.Left) <= 5.0)
            .Where(w => !IsKnownAnchorLabel(w))
            .OrderByDescending(w => w.BoundingBox.Top)
            .ToList();

        return candidatesBelow.Count > 0 ? [candidatesBelow[0]] : [];
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
    /// captures every word in the FULL item band beneath the header down to (but not
    /// including) the first totals label (SUB-TOTAL/SUBTOTAL), so every sample item row and
    /// its sub-lines (unit, quantity, "x", unit price, etc.) are excluded from StaticRuns -
    /// not just the first row. The header words themselves remain static.
    /// </summary>
    private static (List<ItemColumn> Columns, List<Word> HeaderWords, List<Word> ItemBandWords, double TopY, double? HeaderBottom) BuildColumns(
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

        // Full item band: every word strictly below the header row down to (but not
        // including) the first totals label line (SUB-TOTAL/SUBTOTAL). This removes ALL
        // sample item rows/sub-lines, not just the first.
        var normalizedTotalsLabels = new[] { "SUB-TOTAL", "SUBTOTAL" };
        double bandBottom = words
            .Where(w =>
            {
                var normalized = NormalizeText(w.Text);
                return normalizedTotalsLabels.Any(l => normalized == l || normalized.StartsWith(l, StringComparison.Ordinal));
            })
            .Select(w => w.BoundingBox.Bottom)
            .DefaultIfEmpty(double.NegativeInfinity)
            .Max();

        var itemBandWords = words
            .Where(w => w.BoundingBox.Top < headerBottom && w.BoundingBox.Bottom > bandBottom)
            .ToList();

        return (columns, headerWords.Select(h => h.Word).ToList(), itemBandWords, topYValue, headerBottom);
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
