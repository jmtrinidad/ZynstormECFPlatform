using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri.Received;

/// <summary>
/// PDF A4 de un e-CF RECIBIDO de un tercero: cabecera azul marino con el eNCF, tarjetas de
/// emisor y comprador, tabla de ítems con cabecera repetida, caja de totales y pie con el QR de
/// la DGII. Sirve a todos los tipos de e-CF (31 a 47).
/// </summary>
public class ReceivedEcfPdf(ReceivedEcfModel model) : IDocument
{
    private const string Navy = "#0A2540";
    private const string Accent = "#0D9488";
    private const string Ink = "#0F172A";
    private const string Muted = "#64748B";
    private const string Line = "#E2E8F0";
    private const string Soft = "#F1F5F9";
    private const float Gutter = 36;

    private static readonly CultureInfo Culture = new("es-DO");

    private readonly ReceivedEcfModel _model = model;

    static ReceivedEcfPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Mapea el XML firmado y genera el PDF. Lanza si el XML no se puede interpretar.</summary>
    public static byte[] Render(string signedXml, DgiiEnvironment environment) =>
        new ReceivedEcfPdf(ReceivedEcfMapper.Map(signedXml, environment)).GeneratePdf();

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_model.TypeName} {_model.ENcf}",
        Author = _model.Issuer.Name,
        Subject = "Representación impresa de un e-CF recibido"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato).FontSize(9).FontColor(Ink));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingHorizontal(Gutter).PaddingTop(18).PaddingBottom(12).Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(ComposeParties);
                column.Item().Element(ComposeMeta);
                column.Item().Element(ComposeReference);
                column.Item().Element(ComposeItems);
                column.Item().Element(ComposeTotals);
            });
            page.Footer().Element(ComposeFooter);
        });
    }

    // ------------------------------------------------------------------ header

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Background(Navy).PaddingHorizontal(Gutter).PaddingVertical(22).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("COMPROBANTE RECIBIDO")
                        .FontSize(8).Bold().FontColor("#5EEAD4").LetterSpacing(0.12f);
                    left.Item().PaddingTop(5).Text(_model.TypeName)
                        .FontSize(15).Bold().FontColor(Colors.White);
                    left.Item().PaddingTop(3).Text("Documento electrónico fiscal de un tercero")
                        .FontSize(8).FontColor("#94A3B8");
                });

                row.ConstantItem(190).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text("e-NCF").FontSize(8).FontColor("#94A3B8").LetterSpacing(0.12f);
                    right.Item().AlignRight().Text(_model.ENcf).FontSize(20).Bold().FontColor(Colors.White);
                });
            });
            column.Item().Height(3).Background(Accent);
        });
    }

    // ------------------------------------------------------------------ parties

    private void ComposeParties(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => PartyCard(c, "EMISOR", _model.Issuer, "No indicado"));
            row.ConstantItem(12);
            row.RelativeItem().Element(c => PartyCard(c, "COMPRADOR", _model.Buyer, "Consumidor final"));
        });
    }

    private static void PartyCard(IContainer container, string label, Party party, string emptyName)
    {
        container
            .Background(Soft)
            .BorderLeft(3).BorderColor(Accent)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(2);
                column.Item().Text(label).FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.12f);
                column.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(party.Name) ? emptyName : party.Name)
                    .FontSize(11).Bold();

                if (!string.IsNullOrWhiteSpace(party.Document))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("RNC / ID  ").FontColor(Muted);
                        text.Span(party.Document).SemiBold();
                    });
                }

                if (!string.IsNullOrWhiteSpace(party.Address))
                {
                    column.Item().Text(party.Address).FontColor(Muted);
                }

                if (!string.IsNullOrWhiteSpace(party.Phone))
                {
                    column.Item().Text($"Tel. {party.Phone}").FontColor(Muted);
                }

                if (!string.IsNullOrWhiteSpace(party.Email))
                {
                    column.Item().Text(party.Email).FontColor(Muted);
                }
            });
    }

    // ------------------------------------------------------------------ meta strip

    private void ComposeMeta(IContainer container)
    {
        var cells = new List<(string Label, string Value)>
        {
            ("FECHA DE EMISIÓN", _model.FechaEmision),
            ("VÁLIDO HASTA", _model.ValidUntil),
            ("FORMA DE PAGO", string.IsNullOrWhiteSpace(_model.PaymentCondition) ? _model.PaymentType : _model.PaymentCondition),
            ("FACTURA INTERNA", _model.InternalInvoiceNumber)
        }.Where(cell => !string.IsNullOrWhiteSpace(cell.Value)).ToList();

        if (cells.Count == 0)
        {
            return;
        }

        container.BorderTop(0.75f).BorderBottom(0.75f).BorderColor(Line).PaddingVertical(8).Row(row =>
        {
            foreach (var cell in cells)
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(cell.Label).FontSize(7).Bold().FontColor(Muted).LetterSpacing(0.1f);
                    column.Item().PaddingTop(2).Text(cell.Value).FontSize(10).SemiBold();
                });
            }
        });
    }

    private void ComposeReference(IContainer container)
    {
        if (string.IsNullOrWhiteSpace(_model.AffectedNcf))
        {
            return;
        }

        container.Border(0.75f).BorderColor(Line).Padding(10).Column(column =>
        {
            column.Item().Text("DOCUMENTO AFECTADO").FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.12f);
            column.Item().PaddingTop(3).Text(text =>
            {
                text.Span("NCF modificado  ").FontColor(Muted);
                text.Span(_model.AffectedNcf).Bold();
            });

            if (!string.IsNullOrWhiteSpace(_model.ModificationCode))
            {
                column.Item().Text(text =>
                {
                    text.Span("Modificación  ").FontColor(Muted);
                    text.Span(_model.ModificationCode);
                });
            }

            if (!string.IsNullOrWhiteSpace(_model.ModificationReason))
            {
                column.Item().Text(text =>
                {
                    text.Span("Motivo  ").FontColor(Muted);
                    text.Span(_model.ModificationReason);
                });
            }
        });
    }

    // ------------------------------------------------------------------ items

    private void ComposeItems(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                columns.RelativeColumn(5);
                columns.ConstantColumn(42);
                columns.ConstantColumn(66);
                columns.ConstantColumn(58);
                columns.ConstantColumn(62);
                columns.ConstantColumn(76);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#", false);
                HeaderCell(header.Cell(), "Descripción", false);
                HeaderCell(header.Cell(), "Cant.", true);
                HeaderCell(header.Cell(), "Precio (RD$)", true);
                HeaderCell(header.Cell(), "Desc.", true);
                HeaderCell(header.Cell(), "ITBIS", true);
                HeaderCell(header.Cell(), "Importe", true);
            });

            var index = 0;
            foreach (var item in _model.Items)
            {
                var background = index % 2 == 1 ? Soft : "#FFFFFF";
                index++;

                BodyCell(table.Cell(), background, index.ToString(Culture), false, Muted);
                table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Line)
                    .PaddingVertical(5).PaddingHorizontal(4)
                    .Text(text =>
                    {
                        text.Span(item.Description);
                        if (!string.IsNullOrWhiteSpace(item.Unit))
                        {
                            text.Span($"  ·  {item.Unit}").FontColor(Muted).FontSize(7.5f);
                        }
                    });
                BodyCell(table.Cell(), background, item.Quantity.ToString("#,##0.##", Culture), true);
                BodyCell(table.Cell(), background, Amount(item.Price), true);
                BodyCell(table.Cell(), background, item.Discount > 0 ? Amount(item.Discount) : "—", true,
                    item.Discount > 0 ? Ink : Muted);
                BodyCell(table.Cell(), background, item.Itbis > 0 ? Amount(item.Itbis) : "—", true,
                    item.Itbis > 0 ? Ink : Muted);
                BodyCell(table.Cell(), background, Amount(item.Amount), true, Ink, bold: true);
            }
        });
    }

    private static void HeaderCell(IContainer container, string text, bool alignRight)
    {
        var cell = container.Background(Navy).PaddingVertical(6).PaddingHorizontal(4);
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        cell.Text(text).FontSize(8).Bold().FontColor(Colors.White);
    }

    private static void BodyCell(IContainer container, string background, string text, bool alignRight,
        string color = Ink, bool bold = false)
    {
        var cell = container.Background(background).BorderBottom(0.5f).BorderColor(Line)
            .PaddingVertical(5).PaddingHorizontal(4);
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        var span = cell.Text(text).FontColor(color);
        if (bold)
        {
            span.SemiBold();
        }
    }

    // ------------------------------------------------------------------ totals

    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Width(250).ShowEntire().Column(column =>
        {
            column.Item().Element(c => TotalRow(c, "Subtotal", Money(_model.SubTotal)));

            if (_model.Discount > 0)
            {
                column.Item().Element(c => TotalRow(c, "Descuentos", $"-{Money(_model.Discount)}"));
            }

            if (_model.Exento > 0)
            {
                column.Item().Element(c => TotalRow(c, "Monto exento", Money(_model.Exento)));
            }

            var itbisLabel = _model.ItbisRate > 0
                ? $"ITBIS ({_model.ItbisRate.ToString("0.##", Culture)}%)"
                : "ITBIS";
            column.Item().Element(c => TotalRow(c, itbisLabel, Money(_model.Itbis)));

            if (_model.ItbisRetenido > 0)
            {
                column.Item().Element(c => TotalRow(c, "ITBIS retenido", $"-{Money(_model.ItbisRetenido)}"));
            }

            if (_model.IsrRetencion > 0)
            {
                column.Item().Element(c => TotalRow(c, "ISR retenido", $"-{Money(_model.IsrRetencion)}"));
            }

            column.Item().PaddingTop(6).Background(Navy).PaddingVertical(9).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text("TOTAL").FontSize(10).Bold().FontColor("#5EEAD4").LetterSpacing(0.12f);
                row.AutoItem().Text(Money(_model.Total)).FontSize(14).Bold().FontColor(Colors.White);
            });
        });
    }

    private static void TotalRow(IContainer container, string label, string value)
    {
        container.BorderBottom(0.5f).BorderColor(Line).PaddingVertical(4).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(Muted);
            row.AutoItem().Text(value).SemiBold();
        });
    }

    // ------------------------------------------------------------------ footer

    private void ComposeFooter(IContainer container)
    {
        container.PaddingHorizontal(Gutter).PaddingBottom(16).Column(column =>
        {
            column.Item().BorderTop(0.75f).BorderColor(Line).PaddingTop(10).Row(row =>
            {
                var qr = TryBuildQr(_model.Qr);
                if (qr is not null)
                {
                    row.ConstantItem(70).Height(70).Image(qr).FitArea();
                    row.ConstantItem(12);
                }

                row.RelativeItem().Column(info =>
                {
                    info.Spacing(2);
                    if (!string.IsNullOrWhiteSpace(_model.SecurityCode))
                    {
                        info.Item().Text(text =>
                        {
                            text.Span("Código de seguridad  ").FontColor(Muted);
                            text.Span(_model.SecurityCode).Bold();
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_model.FechaFirma))
                    {
                        info.Item().Text(text =>
                        {
                            text.Span("Fecha de firma  ").FontColor(Muted);
                            text.Span(_model.FechaFirma).SemiBold();
                        });
                    }

                    if (qr is not null)
                    {
                        info.Item().PaddingTop(2).Text("Escanee el código para verificar este comprobante en la DGII.")
                            .FontSize(7.5f).FontColor(Muted);
                    }
                });

                row.ConstantItem(185).AlignRight().AlignBottom().Column(right =>
                {
                    right.Item().AlignRight().Text("Representación impresa de un e-CF recibido")
                        .FontSize(7.5f).FontColor(Muted);
                    right.Item().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });
        });
    }

    private static byte[]? TryBuildQr(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M);
            using var code = new QRCoder.PngByteQRCode(data);
            return code.GetGraphic(5);
        }
        catch
        {
            // Sin QR el PDF sigue siendo válido; solo se omite el bloque.
            return null;
        }
    }

    private static string Money(decimal value) => string.Format(Culture, "{0:C2}", value);

    /// <summary>Monto sin símbolo para las filas de la tabla; la moneda va en el encabezado y en los totales.</summary>
    private static string Amount(decimal value) => value.ToString("#,##0.00", Culture);
}
