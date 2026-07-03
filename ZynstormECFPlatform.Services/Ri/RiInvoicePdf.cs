using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// QuestPDF Ri (Representación Impresa) template ported from EasyInvoice's
/// <c>EasyInvoice.Reports/Invoices/InvoicePdf.cs</c>, adapted to read <see cref="RiInvoiceModel"/>
/// instead of EasyInvoice's <c>Invoice</c> entity. Renders as an 80mm continuous receipt
/// and covers most e-CF types (31, 32, 33, 34, 44-47) con las variantes del original:
/// VALIDO HASTA oculto para 32/34, bloques de factura afectada/modificación en notas,
/// footer contado (recibido/cambio) vs crédito (FIRMA REQUERIDA). Type 41 (Compras) uses
/// RiPurchasePdf and type 43 (Gastos Menores) uses RiExpensePdf.
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

        // DGII: 33 = Nota de Débito, 34 = Nota de Crédito (paridad EasyInvoice E33/E34).
        var isCreditNote = _model.EcfType == 34;
        var isDebitNote = _model.EcfType == 33;
        var isNote = isCreditNote || isDebitNote;
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

                    if (!string.IsNullOrEmpty(company.Address))
                    {
                        c.Item().AlignCenter()
                                 .Text($"{company.Address}")
                                 .FontSize(9.5f);
                    }

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

                    // Oculto para 32 y 34, como el isEcfNote del InvoicePdf original.
                    var hideValidUntil = _model.EcfType == 32 || _model.EcfType == 34;
                    if (!hideValidUntil && !string.IsNullOrEmpty(_model.ValidUntil))
                    {
                        tb.Cell().Text("VALIDO HASTA:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.ValidUntil).FontSize(8.5f);
                    }

                    if (!string.IsNullOrEmpty(_model.InternalInvoiceNumber))
                    {
                        tb.Cell().Text(docTypeLabel).SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.InternalInvoiceNumber).FontSize(8.5f);
                    }

                    if (!isNote && !string.IsNullOrEmpty(_model.PaymentType))
                    {
                        tb.Cell().Text("TIPO DE PAGO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.PaymentType.ToUpper()).FontSize(8.5f);

                        if (!string.IsNullOrEmpty(_model.PaymentCondition))
                        {
                            tb.Cell().Text("COND. PAGO:").SemiBold().FontSize(8.5f);
                            tb.Cell().AlignRight().Text(_model.PaymentCondition.ToUpper()).FontSize(8.5f);
                        }
                    }

                    tb.Cell().Text("FECHA:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.FechaEmision).FontSize(8.5f);

                    if (!string.IsNullOrEmpty(_model.Cashier))
                    {
                        tb.Cell().Text("CAJERO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.Cashier).FontSize(8.5f);
                    }
                });

                col.Item().LineHorizontal(0.5f);

                if (!string.IsNullOrEmpty(client.Name) || !string.IsNullOrEmpty(client.Rnc))
                {
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
                }

                if (isNote && !string.IsNullOrEmpty(_model.AffectedNcf))
                {
                    col.Item().Table(tb =>
                    {
                        tb.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        tb.Cell().ColumnSpan(2).PaddingBottom(2).Text("DATOS FACTURA AFECTADA").Bold().FontSize(8.5f);

                        tb.Cell().Text("NCF MODIFICADO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.AffectedNcf).FontSize(8.5f);

                        if (!string.IsNullOrEmpty(_model.PaymentType))
                        {
                            tb.Cell().Text("TIPO DE PAGO:").SemiBold().FontSize(8.5f);
                            tb.Cell().AlignRight().Text(_model.PaymentType.ToUpper()).FontSize(8.5f);
                        }

                        if (!string.IsNullOrEmpty(_model.PaymentCondition))
                        {
                            tb.Cell().Text("COND. PAGO:").SemiBold().FontSize(8.5f);
                            tb.Cell().AlignRight().Text(_model.PaymentCondition.ToUpper()).FontSize(8.5f);
                        }
                    });

                    col.Item().LineHorizontal(0.5f);
                }

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(65);
                        columns.ConstantColumn(70);
                    });

                    tb.Header(header =>
                    {
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).Text("DESCRIPCIÓN").Bold().FontSize(8);
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).Text("ITBIS").Bold().FontSize(8);
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).AlignRight().Text("TOTAL").Bold().FontSize(8);
                    });

                    foreach (var item in _model.Items)
                    {
                        // La descripción ocupa toda la línea; debajo van la cantidad,
                        // el ITBIS y el total alineados con las columnas del header.
                        tb.Cell().ColumnSpan(3).PaddingHorizontal(2).PaddingTop(2).Text(item.Description).FontSize(8);

                        var quantity = item.Quantity.ToString("0.##", CultureInfo.InvariantCulture);
                        var qtyLabel = string.IsNullOrEmpty(item.Unit)
                            ? $"{quantity}  x  {item.Price:F2}"
                            : $"{quantity} {item.Unit}  x  {item.Price:F2}";

                        tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").PaddingHorizontal(2).PaddingBottom(2).Text(qtyLabel).FontSize(7.5f).FontColor("#7F7F7F");
                        tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").PaddingHorizontal(2).PaddingBottom(2).Text(string.Format(Culture, "{0:C2}", item.Itbis)).FontSize(8);
                        tb.Cell().AlignRight().BorderBottom(0.5f).BorderColor("#D9D9D9").PaddingHorizontal(2).PaddingBottom(2).Text(string.Format(Culture, "{0:C2}", item.Amount)).FontSize(8);
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

                if (isNote && (!string.IsNullOrEmpty(_model.ModificationCode) || !string.IsNullOrEmpty(_model.ModificationReason)))
                {
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingVertical(2).Column(noteCol =>
                    {
                        noteCol.Item().Text("INFORMACIÓN DE MODIFICACIÓN").Bold().FontSize(8);
                        if (!string.IsNullOrEmpty(_model.ModificationCode))
                        {
                            noteCol.Item().Text(txt =>
                            {
                                txt.Span("Código Mod.: ").Bold().FontSize(7.5f);
                                txt.Span(_model.ModificationCode).FontSize(7.5f);
                            });
                        }
                        if (!string.IsNullOrEmpty(_model.ModificationReason))
                        {
                            noteCol.Item().Text(txt =>
                            {
                                txt.Span("Razón / Concepto: ").Bold().FontSize(7.5f);
                                txt.Span(_model.ModificationReason).FontSize(7.5f);
                            });
                        }
                    });
                }

                if (!string.IsNullOrEmpty(_model.Note))
                {
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingVertical(2).Column(noteCol =>
                    {
                        noteCol.Item().Text("NOTA").Bold().FontSize(8);
                        noteCol.Item().Text(_model.Note).FontSize(7.5f);
                    });
                }

                col.Item().Text("");

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("ATENDIDO POR:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.Cashier).FontSize(8.5f);

                    tb.Cell().Text("ARTÍCULOS:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text($"{_model.Items.Count}").FontSize(8.5f);

                    tb.Cell().Text(_model.IsCredit ? "CRÉDITO:" : "CONTADO:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Total)).FontSize(8.5f);

                    if (!_model.IsCredit)
                    {
                        tb.Cell().Text("TOTAL RECIBIDO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.ReceivedAmount)).FontSize(8.5f);

                        tb.Cell().Text("SU CAMBIO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.ChangeAmount)).FontSize(8.5f);
                    }
                });

                if (_model.IsCredit)
                {
                    col.Item().Text("");
                    col.Item().LineHorizontal(0.5f);
                    col.Item().AlignCenter().Text("FIRMA REQUERIDA").SemiBold().FontSize(8.5f);
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
