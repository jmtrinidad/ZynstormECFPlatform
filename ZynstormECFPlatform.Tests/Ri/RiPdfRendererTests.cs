using UglyToad.PdfPig;
using ZynstormECFPlatform.Services.Ri;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiPdfRendererTests
{
    [Fact]
    public void Render_ProducesNonEmptyPdf_WithQr()
    {
        var layout = new LayoutDescriptor(); // defaults
        var data = new RiData
        {
            ENcf = "E320000000028",
            TipoeCF = "32",
            FechaEmision = "30-06-2026",
            SecurityCode = "N4J8CY",
            QrUrl = "https://fc.dgii.gov.do/CerteCF/ConsultaTimbreFC?...",
            Issuer = new Party { Name = "MULTI SERVICE ICAAYSI SRL", Document = "132293894" },
            Buyer = new Party { Name = "CONSUMIDOR FINAL" },
            Items = [new RiItem { Description = "Servicio", Quantity = 1, Price = 1180, Amount = 1180 }],
            Totals = new RiTotals { Total = 1180 }
        };

        var bytes = RiPdfRenderer.Render(layout, data);

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_WithExtractorFieldKeys_PopulatesItemCellsAndTotals()
    {
        // Locking test for C1: RiModelExtractor emits lowercase-Spanish Field keys
        // (descripcion, cantidad, valor / subTotal, itbis, total). The renderer must
        // map those exact keys to RiData properties, not PascalCase-English keys.
        var layout = new LayoutDescriptor();
        layout.Items.Columns =
        [
            new ItemColumn { Field = "descripcion", Align = "Left" },
            new ItemColumn { Field = "cantidad", Align = "Right" },
            new ItemColumn { Field = "valor", Align = "Right" }
        ];
        layout.Totals =
        [
            new TotalRow { Field = "total", Label = "TOTAL:" }
        ];

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

        var bytes = RiPdfRenderer.Render(layout, data);

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPage(1).GetWords().Select(w => w.Text));

        Assert.Contains("Servicio ABC", text);
        Assert.Contains("1,180", text);
    }
}
