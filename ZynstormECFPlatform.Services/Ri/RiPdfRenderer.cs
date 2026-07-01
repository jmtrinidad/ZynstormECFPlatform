using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Renders a Ri (Representación Impresa) PDF from a <see cref="LayoutDescriptor"/>
/// (extracted visual layout hints) and <see cref="RiData"/> (the e-CF content).
/// Ported from MechanicalServ's ElectronicInvoicePdf, adapted to be flow-based
/// (QuestPDF Column/Table) rather than pixel-absolute, honoring layout hints
/// (palette, page size, item columns, QR placement) where reasonably possible.
/// </summary>
public static class RiPdfRenderer
{
    private static readonly CultureInfo DominicanCulture = new("es-DO");

    static RiPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(LayoutDescriptor layout, RiData data)
    {
        layout ??= new LayoutDescriptor();
        data ??= new RiData();

        var qrPng = BuildQrPng(data.QrUrl);
        var accentColor = ResolveColor(layout, "accent", Colors.Black);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, layout);

                page.Content().Padding(10).Column(col =>
                {
                    col.Spacing(6);

                    ComposeHeader(col, data, accentColor);
                    Separator(col);
                    ComposeDocumentInfo(col, data);
                    Separator(col);
                    ComposeBuyer(col, data);
                    Separator(col);
                    ComposeItems(col, layout, data);
                    Separator(col);
                    ComposeTotals(col, layout, data);
                    ComposeQr(col, layout, data, qrPng);

                    col.Item().AlignCenter().Text("REPRESENTACIÓN IMPRESA DEL e-CF").SemiBold().FontSize(9);
                });
            });
        }).GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page, LayoutDescriptor layout)
    {
        var width = layout.Page.WidthPt;
        var height = layout.Page.HeightPt;

        if (width > 0 && height > 0)
        {
            page.Size((float)width, (float)height, Unit.Point);
        }
        else
        {
            page.Size(PageSizes.Letter);
        }

        var margin = layout.Page.Margin;
        page.Margin(margin > 0 ? (float)margin : 20, Unit.Point);
    }

    private static void ComposeHeader(ColumnDescriptor col, RiData data, string accentColor)
    {
        col.Item().AlignCenter().Column(header =>
        {
            header.Item().AlignCenter().Text(FirstNonEmpty(data.Issuer.Name, "EMISOR")).Bold().FontSize(13).FontColor(accentColor);

            if (!string.IsNullOrWhiteSpace(data.Issuer.Address))
            {
                header.Item().AlignCenter().Text(data.Issuer.Address).FontSize(8);
            }

            if (!string.IsNullOrWhiteSpace(data.Issuer.Phone))
            {
                header.Item().AlignCenter().Text($"Tel.: {data.Issuer.Phone}").FontSize(8);
            }

            if (!string.IsNullOrWhiteSpace(data.Issuer.Document))
            {
                header.Item().AlignCenter().Text($"RNC: {data.Issuer.Document}").FontSize(8);
            }
        });
    }

    private static void ComposeDocumentInfo(ColumnDescriptor col, RiData data)
    {
        col.Item().AlignCenter().Text($"e-CF TIPO {data.TipoeCF}".Trim()).Bold().FontSize(9);

        col.Item().AlignRight().Text(txt =>
        {
            txt.Span("e-NCF: ").SemiBold().FontSize(9);
            txt.Span(data.ENcf).Bold().FontSize(9);
        });

        col.Item().Text(txt =>
        {
            txt.Span("Fecha Emisión: ").SemiBold().FontSize(8);
            txt.Span(data.FechaEmision).FontSize(8);
        });

        if (!string.IsNullOrWhiteSpace(data.FechaFirma))
        {
            col.Item().Text(txt =>
            {
                txt.Span("Fecha Firma: ").SemiBold().FontSize(8);
                txt.Span(data.FechaFirma).FontSize(8);
            });
        }
    }

    private static void ComposeBuyer(ColumnDescriptor col, RiData data)
    {
        col.Item().AlignCenter().Text("DATOS DEL COMPRADOR").SemiBold().FontSize(9);
        LabelValue(col, "Cliente", data.Buyer.Name);
        LabelValue(col, "RNC/Cédula", data.Buyer.Document);
        LabelValue(col, "Dirección", data.Buyer.Address);
        LabelValue(col, "Teléfono", data.Buyer.Phone);
        LabelValue(col, "Correo", data.Buyer.Email);
        LabelValue(col, "País", data.Buyer.Country);
    }

    private static void ComposeItems(ColumnDescriptor col, LayoutDescriptor layout, RiData data)
    {
        var columns = layout.Items.Columns.Count > 0 ? layout.Items.Columns : DefaultItemColumns();

        col.Item().Table(tb =>
        {
            tb.ColumnsDefinition(cd =>
            {
                foreach (var _ in columns)
                {
                    cd.RelativeColumn();
                }
            });

            tb.Header(header =>
            {
                foreach (var column in columns)
                {
                    header.Cell().Padding(2).Text(ColumnLabel(column.Field)).Bold().FontSize(8);
                }
            });

            if (data.Items.Count == 0)
            {
                tb.Cell().ColumnSpan((uint)columns.Count).Padding(2).Text("Sin detalle de bienes o servicios").FontSize(8);
                return;
            }

            foreach (var item in data.Items)
            {
                foreach (var column in columns)
                {
                    var cell = tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2);

                    switch (column.Align?.ToLowerInvariant())
                    {
                        case "right":
                            cell.AlignRight().Text(ItemFieldValue(item, column.Field)).FontSize(8);
                            break;
                        case "center":
                            cell.AlignCenter().Text(ItemFieldValue(item, column.Field)).FontSize(8);
                            break;
                        default:
                            cell.AlignLeft().Text(ItemFieldValue(item, column.Field)).FontSize(8);
                            break;
                    }
                }
            }
        });
    }

    private static void ComposeTotals(ColumnDescriptor col, LayoutDescriptor layout, RiData data)
    {
        var totalRows = layout.Totals.Count > 0 ? layout.Totals : DefaultTotalRows();

        foreach (var row in totalRows)
        {
            var value = TotalFieldValue(data.Totals, row.Field);
            var isTotal = string.Equals(row.Field, "Total", StringComparison.OrdinalIgnoreCase);
            TotalRowItem(col, string.IsNullOrWhiteSpace(row.Label) ? row.Field.ToUpperInvariant() + ":" : row.Label, value, isTotal);
        }
    }

    private static void ComposeQr(ColumnDescriptor col, LayoutDescriptor layout, RiData data, byte[] qrPng)
    {
        var size = layout.Qr.Size > 0 ? (float)layout.Qr.Size : 90;

        col.Item().AlignCenter().Width(size).Height(size).Image(qrPng);
        LabelValue(col, "Código de Seguridad", data.SecurityCode);

        if (!string.IsNullOrWhiteSpace(data.QrUrl))
        {
            col.Item().AlignCenter().Text(data.QrUrl).FontSize(6).FontColor(Colors.Grey.Darken1);
        }
    }

    private static byte[] BuildQrPng(string? url)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(url ?? string.Empty, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(10);
    }

    private static List<ItemColumn> DefaultItemColumns() =>
    [
        new ItemColumn { Field = "Description", Align = "left" },
        new ItemColumn { Field = "Quantity", Align = "right" },
        new ItemColumn { Field = "Price", Align = "right" },
        new ItemColumn { Field = "Itbis", Align = "right" },
        new ItemColumn { Field = "Amount", Align = "right" }
    ];

    private static List<TotalRow> DefaultTotalRows() =>
    [
        new TotalRow { Field = "SubTotal", Label = "SUBTOTAL:" },
        new TotalRow { Field = "Itbis", Label = "ITBIS:" },
        new TotalRow { Field = "Exento", Label = "EXENTO:" },
        new TotalRow { Field = "Total", Label = "TOTAL:" }
    ];

    private static string ColumnLabel(string field) => field switch
    {
        "Description" => "DESCRIPCIÓN",
        "Quantity" => "CANT.",
        "Price" => "PRECIO",
        "Itbis" => "ITBIS",
        "Amount" => "VALOR",
        _ => field.ToUpperInvariant()
    };

    private static string ItemFieldValue(RiItem item, string field) => field switch
    {
        "Description" => item.Description,
        "Quantity" => item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
        "Price" => Money(item.Price),
        "Itbis" => Money(item.Itbis),
        "Amount" => Money(item.Amount),
        _ => string.Empty
    };

    private static decimal TotalFieldValue(RiTotals totals, string field) => field switch
    {
        "SubTotal" => totals.SubTotal,
        "Itbis" => totals.Itbis,
        "Exento" => totals.Exento,
        "Gravado" => totals.Gravado,
        "Total" => totals.Total,
        _ => 0m
    };

    private static void TotalRowItem(ColumnDescriptor col, string label, decimal value, bool isTotal)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem();
            row.ConstantItem(110).AlignRight().Text(label).Bold().FontSize(isTotal ? 11 : 9);
            row.ConstantItem(80).AlignRight().Text(Money(value)).Bold().FontSize(isTotal ? 11 : 9);
        });
    }

    private static void LabelValue(ColumnDescriptor col, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        col.Item().Text(txt =>
        {
            txt.Span($"{label}: ").SemiBold().FontSize(8);
            txt.Span(value).FontSize(8);
        });
    }

    private static void Separator(ColumnDescriptor col) => col.Item().LineHorizontal(0.5f);

    private static string Money(decimal value) => string.Format(DominicanCulture, "{0:C2}", value);

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string ResolveColor(LayoutDescriptor layout, string key, string fallback) =>
        layout.Palette.TryGetValue(key, out var color) && !string.IsNullOrWhiteSpace(color) ? color : fallback;
}
