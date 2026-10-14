using System.Text;
using QuestPDF.Fluent;
using UglyToad.PdfPig;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri;
using ZynstormECFPlatform.Services.Ri.Received;

namespace ZynstormECFPlatform.Tests.Ri.Received;

public class ReceivedEcfPdfTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    private static string PdfText(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        return string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));
    }

    private static int PageCount(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        return pdf.NumberOfPages;
    }

    [Theory]
    [InlineData("Paso_1_E310000000402.xml", "E310000000402")]
    [InlineData("Paso_25_E320000000344.xml", "E320000000344")]
    [InlineData("Paso_E340000000002.xml", "E340000000002")]
    [InlineData("Paso_E410000000007.xml", "E410000000007")]
    [InlineData("Paso_E430000000008.xml", "E430000000008")]
    public void Render_AllTypes_ProducesPdfWithNcf(string fixture, string encf)
    {
        var bytes = ReceivedEcfPdf.Render(Load(fixture), DgiiEnvironment.Production);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        var text = PdfText(bytes);
        Assert.Contains(encf, text);
        Assert.Contains("COMPROBANTE", text);
        Assert.Contains("RECIBIDO", text);
    }

    [Fact]
    public void Render_Type34_ShowsReferenceAndTotals()
    {
        var text = PdfText(ReceivedEcfPdf.Render(Load("Paso_E340000000002.xml"), DgiiEnvironment.Production));

        Assert.Contains("DOCUMENTOS ELECTRONICOS DE 02", text);
        Assert.Contains("DOCUMENTOS ELECTRONICOS DE 03", text);
        Assert.Contains("E310000000034", text);
        Assert.Contains("Error en monto", text);
        Assert.Contains("566,695.00", text);
        Assert.Contains("ITBIS", text);
    }

    [Fact]
    public void GeneratePdf_BuyerEmpty_ShowsConsumidorFinal()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E430000000008.xml"), DgiiEnvironment.Production);

        Assert.Contains("Consumidor", PdfText(new ReceivedEcfPdf(model).GeneratePdf()));
    }

    [Fact]
    public void GeneratePdf_ManyItems_SpansPagesAndRepeatsTableHeader()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E340000000002.xml"), DgiiEnvironment.Production);
        model.Items = Enumerable.Range(1, 80).Select(i => new ReceivedEcfItem
        {
            Description = $"Artículo número {i}",
            Quantity = 1,
            Unit = "Und",
            Price = 100m,
            Itbis = 18m,
            Amount = 100m
        }).ToList();

        var bytes = new ReceivedEcfPdf(model).GeneratePdf();

        Assert.True(PageCount(bytes) >= 2);
        var occurrences = PdfText(bytes).Split("Descripción").Length - 1;
        Assert.True(occurrences >= 2, "la cabecera de la tabla debe repetirse en cada página");
    }

    [Fact]
    public void Render_InvalidXml_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ReceivedEcfPdf.Render("no es xml", DgiiEnvironment.Production));
    }
}
