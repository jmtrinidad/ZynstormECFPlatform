using UglyToad.PdfPig;
using ZynstormECFPlatform.Services.Ri;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiPdfRendererTests
{
    [Fact]
    public void Render_ProducesPdf_WithStaticLabelsFieldsItemsAndQr()
    {
        var page = new PageModel
        {
            WidthPt = 612, // Letter
            HeightPt = 792,
            StaticRuns =
            [
                new TextRun { Text = "SUB-TOTAL:", X = 40, Y = 600, FontSize = 9, Bold = true }
            ],
            Fields =
            [
                new FieldSlot { FieldKey = "total", X = 150, Y = 600, FontSize = 9, Align = "Left" }
            ],
            Items = new ItemsRegion
            {
                TopY = 400,
                RowHeight = 16,
                Columns =
                [
                    new ItemColumn { Field = "descripcion", X = 40, Width = 200, Align = "Left" },
                    new ItemColumn { Field = "importe", X = 300, Width = 80, Align = "Right" }
                ]
            },
            Qr = new QrSlot { X = 40, Y = 650, Size = 90 }
        };

        var data = new RiData
        {
            ENcf = "E320000000028",
            TipoeCF = "32",
            FechaEmision = "30-06-2026",
            SecurityCode = "N4J8CY",
            QrUrl = "https://fc.dgii.gov.do/CerteCF/ConsultaTimbreFC?...",
            Issuer = new Party { Name = "MULTI SERVICE ICAAYSI SRL", Document = "132293894" },
            Buyer = new Party { Name = "CONSUMIDOR FINAL" },
            Items = [new RiItem { Description = "Servicio ABC", Quantity = 1, Price = 1180, Amount = 1180 }],
            Totals = new RiTotals { Total = 1180 }
        };

        var bytes = RiPdfRenderer.Render(page, data);

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPage(1).GetWords().Select(w => w.Text));

        Assert.Contains("SUB-TOTAL", text);
        Assert.Contains("Servicio ABC", text);
        Assert.Contains("1,180", text);
    }
}
