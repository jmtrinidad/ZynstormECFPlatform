using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// QuestPDF Ri template ported from EasyInvoice's <c>EasyInvoice.Reports/Expenses/ExpensePdf.cs</c>
/// (rama IsInformal, "GASTOS MENORES ELECTRÓNICO"), adaptada a <see cref="RiExpenseModel"/> y
/// extendida con el bloque QR/código de seguridad de las RI. Renders as an 80mm continuous
/// receipt and covers e-CF type 43.
/// </summary>
public class RiExpensePdf(RiExpenseModel model) : IDocument
{
    private static readonly CultureInfo Culture = new("es-DO");

    private readonly RiExpenseModel _model = model;

    static RiExpensePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var company = _model.Company;

        container.Page(page =>
        {
            page.ContinuousSize(227); // 80mm
            page.MarginHorizontal(12);
            page.MarginVertical(7);

            page.Content().Column(col =>
            {
                col.Spacing(6);

                col.Item().Column(c =>
                {
                    c.Item().AlignCenter().Text(company.Name.ToUpper()).Bold().FontSize(12);
                    c.Item().AlignCenter().Text($"RNC.: {company.Rnc}").FontSize(8.5f);
                    if (!string.IsNullOrEmpty(company.Address))
                    {
                        c.Item().AlignCenter().Text(company.Address).FontSize(8.5f);
                    }
                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        c.Item().AlignCenter().Text($"Tel.: {company.Phone}").FontSize(8.5f);
                    }
                    if (!string.IsNullOrEmpty(company.Whatsapp))
                    {
                        c.Item().AlignCenter().Text($"WA: {company.Whatsapp}").FontSize(8.5f);
                    }

                    c.Item().LineHorizontal(0.5f);

                    c.Item().PaddingVertical(3)
                            .AlignCenter()
                            .Text("GASTOS MENORES ELECTRÓNICO")
                            .Bold()
                            .FontSize(8.5f);

                    c.Item().LineHorizontal(0.5f);
                });

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    tb.Cell().Text("eNCF:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.NcfNumber).FontSize(8.5f);

                    if (!string.IsNullOrEmpty(_model.ValidUntil))
                    {
                        tb.Cell().Text("VÁLIDO HASTA:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.ValidUntil).FontSize(8.5f);
                    }

                    if (!string.IsNullOrEmpty(_model.PaymentMethod))
                    {
                        tb.Cell().Text("MÉTODO PAGO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.PaymentMethod.ToUpper()).FontSize(8.5f);
                    }

                    tb.Cell().Text("FECHA:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.FechaEmision).FontSize(8.5f);

                    tb.Cell().Text("USUARIO:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.UserName).FontSize(8.5f);
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().PaddingVertical(2).Column(c =>
                {
                    c.Item().Text("CONCEPTO:").SemiBold().FontSize(8.5f);
                    c.Item().Text(_model.Concept).FontSize(8.5f);
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    tb.Cell().Text("SUB-TOTAL:").Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.SubTotal)).Bold().FontSize(9);

                    tb.Cell().Text(_model.ItbisLabel).Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(_model.Itbis > 0
                        ? string.Format(Culture, "{0:C2}", _model.Itbis)
                        : "EXENTO").Bold().FontSize(9);

                    tb.Cell().Text("TOTAL RD$:").Bold().FontSize(11);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Total)).Bold().FontSize(11);
                });

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

                    var securityCode = string.IsNullOrEmpty(_model.SecurityCode) ? "N/D" : _model.SecurityCode;
                    col.Item().AlignCenter().Text(txt =>
                    {
                        txt.Span("Codigo seguridad: ").Bold().FontSize(8);
                        txt.Span(securityCode).FontSize(8);
                    });

                    if (!string.IsNullOrEmpty(_model.FechaFirma))
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span("Fecha firma digital: ").Bold().FontSize(8);
                            txt.Span(_model.FechaFirma).FontSize(8);
                        });
                    }
                }

                col.Item().AlignCenter().Text("REPRESENTACIÓN IMPRESA DEL e-CF").Bold().FontSize(7.5f);
                col.Item().AlignCenter().Text("Comprobante Electrónico Gastos Menores E43").Italic().FontSize(7.5f);

                col.Item().Text("");
                col.Item().AlignCenter().Text("¡Gracias!").Italic().FontSize(8.5f);
            });
        });
    }
}
