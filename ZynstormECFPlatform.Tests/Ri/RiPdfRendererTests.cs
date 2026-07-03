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
        // Paso_E410000000007.xml: TipoeCF=41 -> RiPurchasePdf (full-sheet).
        var bytes = RiPdfRenderer.Render(41, Load("Paso_E410000000007.xml"));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_Type43_DispatchesToExpenseTemplate_AndProducesPdf()
    {
        // Paso_E430000000008.xml: TipoeCF=43 -> RiExpensePdf (recibo de gastos menores).
        var bytes = RiPdfRenderer.Render(43, Load("Paso_E430000000008.xml"));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

        using var pdf = UglyToad.PdfPig.PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));
        Assert.Contains("GASTOS MENORES ELECTRÓNICO", text);
        Assert.Contains("CONCEPTO:", text);
        Assert.Contains("EXENTO", text);
        Assert.Contains("Gastos Menores E43", text);
    }
}
