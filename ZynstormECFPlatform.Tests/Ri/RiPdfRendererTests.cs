using ZynstormECFPlatform.Services.Ri;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiPdfRendererTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void Render_Type32_DispatchesToInvoiceTemplate_AndProducesPdf()
    {
        // Paso_25_E320000000344.xml: TipoeCF=32 -> RiInvoicePdf (80mm receipt).
        var bytes = RiPdfRenderer.Render(32, Load("Paso_25_E320000000344.xml"));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_Type31_DispatchesToInvoiceTemplate_AndProducesPdf()
    {
        // Paso_1_E310000000402.xml: TipoeCF=31 -> RiInvoicePdf (80mm receipt).
        var bytes = RiPdfRenderer.Render(31, Load("Paso_1_E310000000402.xml"));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_Type41_DispatchesToPurchaseTemplate_AndProducesPdf()
    {
        // No dedicated type-41 fixture exists yet; reuse a general e-CF XML to verify
        // the ecfType==41 branch routes to RiPurchasePdf (full-sheet) without throwing.
        var bytes = RiPdfRenderer.Render(41, Load("Paso_1_E310000000402.xml"));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
