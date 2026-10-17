using ZynstormECFPlatform.Services.Ri;
using ZynstormECFPlatform.Services.Ri.Html;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri.Html;

public class RiHtmlRendererTests
{
    private static readonly RiCompanyHeader Company =
        new("ROYALTRIX SRL", "133009889", "Calle 1, Santo Domingo", "809-555-1234", "809-555-1234");

    private const string Samples = """
{"Cedula":"001-0000000-1","Sexo":"M","FechaNac":"01/01/1990","Edad":"36","SegMed":"ARS Demo",
 "NSS":"123456789","Devolucion":"0.00","Medico":"Dr. Ejemplo","MontoPagado":"RD$100.00","Restante":"RD$0.00"}
""";

    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    private static string Render31(string template = RiHtmlTestTemplates.Reference, string? samples = Samples) =>
        RiHtmlRenderer.Render(template, Load("Paso_1_E310000000402.xml"), Company, samples);

    [Fact]
    public void Render_Type31_FillsHeaderBuyerAndNcf()
    {
        var html = Render31();

        Assert.Contains("Calle 1, Santo Domingo", html);
        Assert.Contains("RNC: 133009889", html);
        Assert.Contains("E310000000402", html);
        Assert.Contains("MORTEROS DE EUROPA", html);
        Assert.Contains("FACTURA DE CRÉDITO FISCAL ELECTRÓNICA", html);
        Assert.DoesNotContain("{{", html);
    }

    [Fact]
    public void Render_Type31_ShowsValidoHasta()
    {
        var html = Render31();

        Assert.Contains("VALIDO HASTA:", html);
        Assert.Contains("31/12/2028", html);
    }

    [Theory]
    [InlineData("Paso_25_E320000000344.xml")]
    [InlineData("Paso_E340000000002.xml")]
    public void Render_Type32And34_RemovesValidoHastaLine(string fixture)
    {
        var html = RiHtmlRenderer.Render(RiHtmlTestTemplates.Reference, Load(fixture), Company, Samples);

        Assert.DoesNotContain("VALIDO HASTA", html);
        Assert.Contains("eNCF:", html);
    }

    [Fact]
    public void Render_RepeatsItemRow_OncePerXmlItem()
    {
        var html = Render31();

        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        var rows = document.QuerySelectorAll("#items tbody tr");
        Assert.True(rows.Length >= 1);
        Assert.All(rows, r => Assert.DoesNotContain("{{", r.TextContent));
        // Los montos de la fila van sin el símbolo de moneda; los totales sí lo llevan.
        Assert.All(rows, r => Assert.DoesNotContain("RD$", r.TextContent));
        Assert.All(rows, r => Assert.Matches(@"\d+\.\d{2}", r.TextContent));
    }

    [Fact]
    public void Render_Type34With10Items_ProducesTenRows()
    {
        var html = RiHtmlRenderer.Render(RiHtmlTestTemplates.Reference, Load("Paso_E340000000002.xml"), Company, Samples);

        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        var rows = document.QuerySelectorAll("#items tbody tr");
        Assert.Equal(10, rows.Length);
        Assert.All(rows, r => Assert.DoesNotContain("{{", r.TextContent));
    }

    private const string DgiiTemplate = """
<!DOCTYPE html>
<html><head><meta charset="utf-8"></head><body>
  <div>{{TipoComprobante}}</div>
  <div class="row"><span>eNCF:</span><span>{{eNCF}}</span></div>
  <div class="row"><span>e-NCF Modificado:</span><span>{{NcfModificado}}</span></div>
  <div class="row"><span>Modificación:</span><span>{{CodigoModificacion}}</span></div>
  <table id="items"><tbody><tr><td>{{Cantidad}} {{Unidad}}</td><td><span>{{Exento}}</span>{{Descripcion}}</td><td>{{Precio}}</td><td>{{ITBIS}}</td><td>{{Monto}}</td></tr></tbody></table>
  <div class="row"><b>Subtotal gravado:</b><b>{{SubtotalGravado}}</b></div>
  <div class="row"><b>Subtotal exento:</b><b>{{SubtotalExento}}</b></div>
  <div class="row"><b>Total:</b><b>{{Total}}</b></div>
  <div>{{QR}}</div>
</body></html>
""";

    [Fact]
    public void Render_ExemptItem_ShowsEIndicator()
    {
        var html = RiHtmlRenderer.Render(DgiiTemplate, Load("Paso_1_E310000000402.xml"), Company, null);

        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        var row = document.QuerySelector("#items tbody tr")!;
        Assert.StartsWith("E", row.QuerySelector("td:nth-child(2) span")!.TextContent);
    }

    [Fact]
    public void Render_Type31_RemovesCreditNoteReferenceLines()
    {
        var html = RiHtmlRenderer.Render(DgiiTemplate, Load("Paso_1_E310000000402.xml"), Company, null);

        Assert.DoesNotContain("e-NCF Modificado", html);
        Assert.DoesNotContain("Modificación:", html);
        Assert.DoesNotContain("{{", html);
    }

    [Fact]
    public void Render_Type34_ShowsModifiedNcfAndModificationCodeInWords()
    {
        var html = RiHtmlRenderer.Render(DgiiTemplate, Load("Paso_E340000000002.xml"), Company, null);

        Assert.Contains("e-NCF Modificado:", html);
        Assert.Contains("E310000000034", html);
        Assert.Contains("Corrige montos del NCF modificado", html);
    }

    [Fact]
    public void Render_Subtotals_ShowOnlyTheLinesThatApply()
    {
        // Paso_1 solo trae monto exento: la línea de gravado se quita.
        var html = RiHtmlRenderer.Render(DgiiTemplate, Load("Paso_1_E310000000402.xml"), Company, null);

        Assert.Contains("Subtotal exento:", html);
        Assert.DoesNotContain("Subtotal gravado:", html);
    }

    [Fact]
    public void Render_UsesSampleDataForFieldsMissingInXml()
    {
        var html = Render31();

        Assert.Contains("Dr. Ejemplo", html);
        Assert.Contains("ARS Demo", html);
        Assert.Contains("123456789", html);
    }

    [Fact]
    public void Render_RemovesLinesWhoseValueIsEmpty()
    {
        var html = Render31(samples: null);

        Assert.DoesNotContain("Médico:", html);
        Assert.DoesNotContain("NSS:", html);
        // El logo vacío elimina el recuadro.
        Assert.DoesNotContain("id=\"logo\"", html);
    }

    [Fact]
    public void Render_EmbedsQrImage_AndSecurityBlock()
    {
        var html = Render31();

        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("Codigo seguridad", html);
        Assert.Contains("REPRESENTACION IMPRESA DEL e-CF", html);
    }

    [Fact]
    public void Render_UsesDataImageLogoOnlyWhenValid()
    {
        var good = Render31(samples: "{\"EmisorLogo\":\"data:image/png;base64,AAAA\"}");
        var bad = Render31(samples: "{\"EmisorLogo\":\"http://evil/x.png\"}");

        Assert.Contains("data:image/png;base64,AAAA", good);
        Assert.DoesNotContain("evil", bad);
    }

    [Fact]
    public void Render_ClientLogo_WinsOverSampleLogo()
    {
        var html = RiHtmlRenderer.Render(
            RiHtmlTestTemplates.Reference,
            Load("Paso_1_E310000000402.xml"),
            Company,
            "{\"EmisorLogo\":\"data:image/png;base64,SAMPLE\"}",
            "data:image/png;base64,CLIENTE");

        Assert.Contains("data:image/png;base64,CLIENTE", html);
        Assert.DoesNotContain("SAMPLE", html);
    }

    [Fact]
    public void Render_ClientLogo_ShowsLogoBox_WithoutSampleData()
    {
        var html = RiHtmlRenderer.Render(
            RiHtmlTestTemplates.Reference, Load("Paso_1_E310000000402.xml"), Company, null, "data:image/png;base64,CLIENTE");

        Assert.Contains("id=\"logo\"", html);
        Assert.Contains("data:image/png;base64,CLIENTE", html);
    }

    [Fact]
    public void Render_InvalidClientLogo_FallsBackToSampleLogo_OrHidesBox()
    {
        var withSample = RiHtmlRenderer.Render(
            RiHtmlTestTemplates.Reference, Load("Paso_1_E310000000402.xml"), Company,
            "{\"EmisorLogo\":\"data:image/png;base64,SAMPLE\"}", "http://evil/x.png");
        var withoutSample = RiHtmlRenderer.Render(
            RiHtmlTestTemplates.Reference, Load("Paso_1_E310000000402.xml"), Company, null, "http://evil/x.png");

        Assert.Contains("SAMPLE", withSample);
        Assert.DoesNotContain("evil", withSample);
        Assert.DoesNotContain("evil", withoutSample);
        Assert.DoesNotContain("id=\"logo\"", withoutSample);
    }

    [Fact]
    public void Render_EscapesValuesFromXmlAndCompany()
    {
        var company = new RiCompanyHeader("<b>X</b> & Co", "1", "<script>alert(1)</script>", "1", "1");

        var html = RiHtmlRenderer.Render(RiHtmlTestTemplates.Reference, Load("Paso_1_E310000000402.xml"), company, Samples);

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
    }

    [Fact]
    public void Render_InjectsMissingEncfAndQrBlocks()
    {
        const string minimal = """
<html><body>
<h1>FACTURA</h1>
<table><tr><td>{{Descripcion}}</td><td>{{Monto}}</td></tr></table>
<div>Total: {{Total}}</div>
</body></html>
""";

        var html = Render31(minimal);

        Assert.Contains("E310000000402", html);
        Assert.Contains("VALIDO HASTA:", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("REPRESENTACION IMPRESA DEL e-CF", html);
        // El bloque eNCF queda debajo del título y antes de la tabla de ítems.
        Assert.True(html.IndexOf("FACTURA", StringComparison.Ordinal) < html.IndexOf("E310000000402", StringComparison.Ordinal));
        Assert.True(html.IndexOf("E310000000402", StringComparison.Ordinal) < html.IndexOf("<table", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_ItbisMarkers_FillPerItemAndTotal_EvenWhenExempt()
    {
        const string t = "<html><body><table id=\"items\"><tbody><tr><td>{{Descripcion}}</td><td>{{ITBIS}}</td><td>{{Monto}}</td></tr></tbody></table>" +
                         "<div id=\"ti\">Total ITBIS: {{TotalITBIS}}</div>{{Total}}</body></html>";

        var html = Render31(t);

        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        Assert.DoesNotContain("{{", html);
        Assert.All(document.QuerySelectorAll("#items tbody tr"), r => Assert.Equal("0.00", r.Children[1].TextContent.Trim()));
        Assert.Contains("RD$0.00", document.QuerySelector("#ti")!.TextContent);
    }

    [Fact]
    public void Render_KeepsUnknownMarkersAsIs()
    {
        const string t = "<html><body><table><tr><td>{{Descripcion}}</td></tr></table>{{Total}} {{Inventado}}</body></html>";

        var html = Render31(t);

        Assert.Contains("{{Inventado}}", html);
    }
}
