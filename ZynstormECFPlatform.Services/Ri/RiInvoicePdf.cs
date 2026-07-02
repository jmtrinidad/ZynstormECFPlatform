using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// QuestPDF Ri (Representación Impresa) template ported from EasyInvoice's
/// <c>EasyInvoice.Reports/Invoices/InvoicePdf.cs</c>, adapted to read <see cref="RiInvoiceModel"/>
/// instead of EasyInvoice's <c>Invoice</c> entity. Renders as an 80mm continuous receipt
/// and covers most e-CF types (31, 32, 33, 34, 43-47); type 41 (Compras) uses a different,
/// full-sheet template (RiPurchasePdf, added separately).
/// </summary>
public class RiInvoicePdf(RiInvoiceModel model) : IDocument
{
    private static readonly CultureInfo Culture = new("es-DO");

    private readonly RiInvoiceModel _model = model;

    static RiInvoicePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var company = _model.Company;
        var client = _model.Client;

        var isCreditNote = _model.EcfType == 33;
        var isDebitNote = _model.EcfType == 34;
        var docTypeLabel = isCreditNote ? "NOTA CRÉDITO:" : (isDebitNote ? "NOTA DÉBITO:" : "FACTURA:");

        container.Page(page =>
        {
            page.ContinuousSize(227); // 80mm paper width
            page.MarginHorizontal(12);
            page.MarginVertical(7);
            page.Content().Column(col =>
            {
                col.Spacing(7);
                col.Item().Column(c =>
                {
                    c.Item().AlignCenter()
                              .Text($"{company.Name.ToUpper()}")
                              .Bold()
                              .FontSize(12);

                    c.Item().AlignCenter()
                              .Text($"RNC.: {company.Rnc}")
                              .FontSize(9.5f);

                    c.Item().AlignCenter()
                              .Text($"{company.Address}")
                              .FontSize(9.5f);

                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        c.Item().AlignCenter()
                                 .Text($"Tel.: {company.Phone}")
                                 .FontSize(9.5f);
                    }

                    if (!string.IsNullOrEmpty(company.Whatsapp))
                    {
                        c.Item().AlignCenter()
                                 .Text($"WA: {company.Whatsapp}")
                                 .FontSize(9.5f);
                    }

                    c.Item().LineHorizontal(0.5f);

                    c.Item().PaddingVertical(3).AlignCenter()
                             .Text($"{_model.NcfTypeName.ToUpper()}")
                             .Bold()
                             .FontSize(8.5f);

                    c.Item().LineHorizontal(0.5f);
                });

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("eNCF:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.NcfNumber).FontSize(8.5f);

                    tb.Cell().Text(docTypeLabel).SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.NcfNumber).FontSize(8.5f);

                    if (!isCreditNote && !isDebitNote && !string.IsNullOrEmpty(_model.PaymentType))
                    {
                        tb.Cell().Text("TIPO DE PAGO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.PaymentType.ToUpper()).FontSize(8.5f);
                    }

                    tb.Cell().Text("FECHA:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.FechaEmision).FontSize(8.5f);
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("CLIENTE:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text($"{client.Name}").FontSize(8.5f);

                    tb.Cell().Text("RNC/CED:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(string.IsNullOrEmpty(client.Rnc) ? "N/D" : client.Rnc).FontSize(8.5f);
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(60);
                    });

                    tb.Header(header =>
                    {
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).Text("DESCRIPCIÓN").Bold().FontSize(8);
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).Text("ITBIS").Bold().FontSize(8);
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).AlignRight().Text("TOTAL").Bold().FontSize(8);
                    });

                    foreach (var item in _model.Items)
                    {
                        tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2).Column(productCol =>
                        {
                            productCol.Item().Text(item.Description).FontSize(8);
                            var qtyLabel = $"{item.Quantity.ToString("0.##", CultureInfo.InvariantCulture)}  x  {item.Price:F2}";
                            productCol.Item().Text(qtyLabel).FontSize(7.5f).FontColor("#7F7F7F");
                        });
                        tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2).Text(string.Format(Culture, "{0:C2}", item.Itbis)).FontSize(8);
                        tb.Cell().AlignRight().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2).Text(string.Format(Culture, "{0:C2}", item.Amount)).FontSize(8);
                    }
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("SUB-TOTAL:").Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.SubTotal)).Bold().FontSize(9);

                    if (_model.Discount > 0)
                    {
                        tb.Cell().Text("DESC:").Bold().FontSize(9);
                        tb.Cell().AlignRight().Text(string.Format(Culture, "-{0:C2}", _model.Discount)).Bold().FontSize(9);
                    }

                    tb.Cell().Text("ITBIS:").Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Itbis)).Bold().FontSize(9);

                    tb.Cell().Text("TOTAL RD$:").Bold().FontSize(11);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Total)).Bold().FontSize(11);
                });

                if (!string.IsNullOrEmpty(_model.Note))
                {
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingVertical(2).Column(noteCol =>
                    {
                        noteCol.Item().Text("NOTA").Bold().FontSize(8);
                        noteCol.Item().Text(_model.Note).FontSize(7.5f);
                    });
                }

                col.Item().LineHorizontal(0.5f);
                col.Item().Text("");

                if (!string.IsNullOrEmpty(_model.Qr))
                {
                    byte[]? qrCodeBytes = null;
                    try
                    {
                        using var qrGenerator = new QRCoder.QRCodeGenerator();
                        using var qrCodeData = qrGenerator.CreateQrCode(_model.Qr, QRCoder.QRCodeGenerator.ECCLevel.M);
                        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                        qrCodeBytes = qrCode.GetGraphic(5);
                    }
                    catch
                    {
                        // Fallback: skip the QR image if it can't be generated.
                    }

                    if (qrCodeBytes != null)
                    {
                        col.Item().AlignCenter().Width(110).Image(qrCodeBytes);
                    }

                    var securityCode = GetQueryParam(_model.Qr, "CodigoSeguridad");
                    if (string.IsNullOrEmpty(securityCode) || securityCode == "N/D")
                    {
                        securityCode = string.IsNullOrEmpty(_model.SecurityCode) ? "N/D" : _model.SecurityCode;
                    }

                    var signatureDate = GetQueryParam(_model.Qr, "FechaFirma");
                    if (string.IsNullOrEmpty(signatureDate) || signatureDate == "N/D")
                    {
                        signatureDate = string.IsNullOrEmpty(_model.FechaFirma) ? "N/D" : _model.FechaFirma;
                    }

                    col.Item().AlignCenter().Text(txt =>
                    {
                        txt.Span("Codigo seguridad: ").Bold().FontSize(8);
                        txt.Span(securityCode).FontSize(8);
                    });

                    col.Item().AlignCenter().Text(txt =>
                    {
                        txt.Span("Fecha firma digital: ").Bold().FontSize(8);
                        txt.Span(signatureDate).FontSize(8);
                    });

                    col.Item().AlignCenter().Text("REPRESENTACION IMPRESA DEL e-CF")
                              .Bold()
                              .FontSize(8);

                    col.Item().Text("");
                }

                col.Item().AlignCenter().Text("¡Gracias por preferirnos!").Italic().FontSize(8.5f);
            });
        });
    }

    private static string GetQueryParam(string url, string paramName)
    {
        if (string.IsNullOrEmpty(url)) return "N/D";
        try
        {
            var queryStart = url.IndexOf('?');
            if (queryStart == -1) return "N/D";
            var query = url.Substring(queryStart + 1);
            var parts = query.Split('&');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2 && string.Equals(keyValue[0], paramName, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(keyValue[1]);
                }
            }
        }
        catch
        {
            // ignored
        }
        return "N/D";
    }
}
