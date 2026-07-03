using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// QuestPDF Ri (Representación Impresa) template ported from EasyInvoice's
/// <c>EasyInvoice.Reports/Purchases/PurchasePdf.cs</c>, adapted to read
/// <see cref="RiPurchaseModel"/> instead of EasyInvoice's <c>Purchase</c> entity, and
/// extended with a DGII QR code block. Renders as a full Letter-size sheet and covers
/// e-CF type 41 (Comprobante de Compras).
/// </summary>
public class RiPurchasePdf(RiPurchaseModel model) : IDocument
{
    private static readonly CultureInfo Culture = new("es-DO");

    private readonly RiPurchaseModel _model = model;

    static RiPurchasePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var company = _model.Company;
        var supplier = _model.Supplier;

        const string title = "COMPROBANTE DE COMPRA";

        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(30);

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Left: Company Info
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(company.Name.ToUpper()).Bold().FontSize(12).FontColor("#1A365D");
                        c.Item().Text($"RNC: {company.Rnc}").FontSize(8).FontColor("#4A5568");
                        c.Item().Text(company.Address).FontSize(8).FontColor("#4A5568");
                        if (!string.IsNullOrEmpty(company.Phone))
                        {
                            c.Item().Text($"Tel: {company.Phone}").FontSize(8).FontColor("#4A5568");
                        }
                        if (!string.IsNullOrEmpty(company.Whatsapp))
                        {
                            c.Item().Text($"WA: {company.Whatsapp}").FontSize(8).FontColor("#4A5568");
                        }
                    });

                    // Right: Title & Purchase Details
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text(title).Bold().FontSize(11).FontColor("#2B6CB0");
                        c.Item().Text($"eNCF: {_model.NcfNumber}").Bold().FontSize(9);
                        if (!string.IsNullOrEmpty(_model.ValidUntil))
                        {
                            c.Item().Text($"Válido hasta: {_model.ValidUntil}").FontSize(8);
                        }
                        c.Item().Text($"Fecha: {_model.FechaEmision}").FontSize(8);
                    });
                });

                col.Item().PaddingVertical(8).LineHorizontal(1f).LineColor("#E2E8F0");
            });

            page.Content().Column(col =>
            {
                col.Spacing(12);

                // Supplier Info
                col.Item().Row(row =>
                {
                    row.RelativeItem().Border(0.5f).BorderColor("#E2E8F0").Background("#F7FAFC").Padding(8).Column(c =>
                    {
                        c.Item().Text("SUPLIDOR").Bold().FontSize(8).FontColor("#4A5568");
                        c.Item().Text(supplier.Name).Bold().FontSize(10).FontColor("#1A365D");
                        c.Item().Text($"RNC/CÉD: {(string.IsNullOrEmpty(supplier.Rnc) ? "N/D" : supplier.Rnc)}").FontSize(8);
                        if (!string.IsNullOrEmpty(supplier.Address))
                        {
                            c.Item().Text(supplier.Address).FontSize(8);
                        }
                    });
                });

                // Items Table
                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);     // Description
                        columns.RelativeColumn(0.8f);  // Quantity
                        columns.RelativeColumn(1.2f);  // Unit Cost
                        columns.RelativeColumn(0.9f);  // ITBIS%
                        columns.RelativeColumn(1.0f);  // ITBIS
                        columns.RelativeColumn(1.2f);  // Total
                    });

                    tb.Header(header =>
                    {
                        header.Cell().Background("#2B6CB0").Padding(4).Text("DESCRIPCIÓN").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                        header.Cell().Background("#2B6CB0").Padding(4).Text("CANT.").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                        header.Cell().Background("#2B6CB0").Padding(4).Text("COSTO UNIT.").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                        header.Cell().Background("#2B6CB0").Padding(4).Text("ITBIS%").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                        header.Cell().Background("#2B6CB0").Padding(4).Text("ITBIS").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                        header.Cell().Background("#2B6CB0").Padding(4).AlignRight().Text("TOTAL").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                    });

                    foreach (var item in _model.Items)
                    {
                        var itbisPct = item.ItbisRate;
                        tb.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(item.Description).FontSize(7.5f);
                        tb.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text($"{item.Quantity:F2}").FontSize(7.5f);
                        tb.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(string.Format(Culture, "{0:C2}", item.Price)).FontSize(7.5f);
                        tb.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text($"{itbisPct:0.##}%").FontSize(7.5f);
                        tb.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(string.Format(Culture, "{0:C2}", item.Itbis)).FontSize(7.5f);
                        tb.Cell().AlignRight().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(string.Format(Culture, "{0:C2}", item.Amount)).FontSize(7.5f);
                    }
                });

                // Spacing and Totals
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        if (!string.IsNullOrEmpty(_model.Note))
                        {
                            c.Item().Text("Notas:").Bold().FontSize(7.5f).FontColor("#4A5568");
                            c.Item().Text(_model.Note).FontSize(7.5f).Italic();
                        }
                    });

                    row.ConstantItem(40);

                    // Totals box
                    row.RelativeItem(0.8f).Column(totalsCol =>
                    {
                        totalsCol.Spacing(3);

                        // Subtotal
                        totalsCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Sub-Total:").FontSize(7.5f);
                            r.RelativeItem().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.SubTotal)).FontSize(7.5f);
                        });

                        // Discount
                        if (_model.Discount > 0)
                        {
                            totalsCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Descuento:").FontSize(7.5f);
                                r.RelativeItem().AlignRight().Text(string.Format(Culture, "-{0:C2}", _model.Discount)).FontSize(7.5f);
                            });
                        }

                        // ITBIS
                        totalsCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("ITBIS:").FontSize(7.5f);
                            r.RelativeItem().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Itbis)).FontSize(7.5f);
                        });

                        // ISR Retention
                        if (_model.IsrRetentionAmount > 0)
                        {
                            totalsCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"Retención ISR ({_model.IsrRetentionRate:0.0}%):").FontSize(7.5f);
                                r.RelativeItem().AlignRight().Text($"-{string.Format(Culture, "{0:C2}", _model.IsrRetentionAmount)}").FontSize(7.5f);
                            });
                        }

                        // ITBIS Retention
                        if (_model.ItbisRetentionAmount > 0)
                        {
                            totalsCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Retención ITBIS:").FontSize(7.5f);
                                r.RelativeItem().AlignRight().Text($"-{string.Format(Culture, "{0:C2}", _model.ItbisRetentionAmount)}").FontSize(7.5f);
                            });
                        }

                        totalsCol.Item().LineHorizontal(0.5f).LineColor("#E2E8F0");

                        // Total Neto
                        var finalTotal = _model.Total - _model.IsrRetentionAmount - _model.ItbisRetentionAmount;
                        totalsCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Neto RD$:").Bold().FontSize(10).FontColor("#1A365D");
                            r.RelativeItem().AlignRight().Text(string.Format(Culture, "{0:C2}", finalTotal)).Bold().FontSize(10).FontColor("#1A365D");
                        });
                    });
                });

                // QR / Security footer block (added for the e-CF Ri; not present in the
                // ported EasyInvoice PurchasePdf). Placed lower-right, with room to spare
                // on the full Letter sheet.
                if (!string.IsNullOrEmpty(_model.Qr))
                {
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem();

                        row.ConstantItem(160).Column(c =>
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
                                c.Item().AlignCenter().Width(110).Image(qrCodeBytes);
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

                            c.Item().AlignCenter().Text(txt =>
                            {
                                txt.Span("Código de Seguridad: ").Bold().FontSize(7.5f);
                                txt.Span(securityCode).FontSize(7.5f);
                            });

                            c.Item().AlignCenter().Text(txt =>
                            {
                                txt.Span("Fecha firma digital: ").Bold().FontSize(7.5f);
                                txt.Span(signatureDate).FontSize(7.5f);
                            });

                            c.Item().PaddingTop(4).AlignCenter().Text("REPRESENTACIÓN IMPRESA DEL e-CF")
                                .Bold()
                                .FontSize(7.5f).FontColor("#718096");
                        });
                    });
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Pág. ").FontSize(7).FontColor("#A0AEC0");
                t.CurrentPageNumber().FontSize(7).FontColor("#A0AEC0");
                t.Span(" de ").FontSize(7).FontColor("#A0AEC0");
                t.TotalPages().FontSize(7).FontColor("#A0AEC0");
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
