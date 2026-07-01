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
}
