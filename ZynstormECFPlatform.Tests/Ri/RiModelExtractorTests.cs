using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiModelExtractorTests
{
    static RiModelExtractorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void KnownFormat_ClassifiesStaticVsDynamicAndDetectsColumnsAndFields()
    {
        var pdfBytes = BuildKnownFormatPdf();

        var result = RiModelExtractor.Extract(pdfBytes);

        Assert.True(result.Success);
        Assert.NotNull(result.Page);

        var page = result.Page!;

        var staticTexts = page.StaticRuns.Select(r => r.Text.ToUpperInvariant()).ToList();
        Assert.Contains(staticTexts, t => t.Contains("SUB-TOTAL"));
        Assert.Contains(staticTexts, t => t.Contains("DESCRIPCI"));

        // Emisor header (above the "FACTURA DE CREDITO FISCAL" banner) is fully static,
        // including the emisor's own RNC - it must never be picked up as rncComprador.
        Assert.Contains(staticTexts, t => t.Contains("EMPRESA"));
        Assert.Contains(staticTexts, t => t.Contains("132293894"));

        var fieldKeys = page.Fields.Select(f => f.FieldKey).ToList();
        Assert.Contains("eNCF", fieldKeys);
        Assert.Contains("total", fieldKeys);
        Assert.Contains("rncComprador", fieldKeys);

        // The buyer RNC/CED value (not the emisor's bare RNC) must be the one mapped.
        var rncComprador = page.Fields.Single(f => f.FieldKey == "rncComprador");
        var rncCompradorRun = page.StaticRuns.SingleOrDefault(r => r.Text.Contains("131880681"));
        Assert.Null(rncCompradorRun); // buyer RNC value must be excluded from StaticRuns, not just relabeled.

        Assert.True(page.Items.Columns.Count >= 2);
        Assert.Contains(page.Items.Columns, c => string.Equals(c.Field, "importe", StringComparison.OrdinalIgnoreCase));

        // Sample values must NOT leak into StaticRuns.
        Assert.DoesNotContain(staticTexts, t => t.Contains("E320000000028"));
        Assert.DoesNotContain(staticTexts, t => t.Contains("PRODUCTO X"));
        Assert.DoesNotContain(staticTexts, t => t.Contains("131880681"));

        // Blank (non e-CF) receipt label sample value must be blanked, with no FieldSlot.
        Assert.DoesNotContain(staticTexts, t => t.Contains("MARIA"));
        Assert.Contains(staticTexts, t => t.Contains("CAJERO"));
        Assert.DoesNotContain(fieldKeys, k => k == "cajero");
    }

    [Fact]
    public void PdfSinTexto_MarcaFailed_ConWarning()
    {
        var pdfBytes = BuildNoTextPdf();

        var result = RiModelExtractor.Extract(pdfBytes);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Warnings);
        Assert.Null(result.Page);
    }

    private static byte[] BuildKnownFormatPdf()
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().Text("MI EMPRESA").FontSize(16).Bold();
                    col.Item().Text("RNC: 132293894");

                    col.Item().PaddingTop(6).Text("FACTURA DE CREDITO FISCAL ELECTRONICA").Bold();

                    col.Item().PaddingTop(6).Text("NCF: E320000000028");
                    col.Item().Text("Fecha: 30-06-2026");
                    col.Item().Text("Cajero: Maria Diaz");

                    col.Item().PaddingTop(10).Text("Cliente: Juan Perez");
                    col.Item().Text("RNC/CED: 131880681");

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Descripción");
                            header.Cell().Text("Cantidad");
                            header.Cell().Text("Precio");
                            header.Cell().Text("ITBIS");
                            header.Cell().Text("Total");
                        });

                        table.Cell().Text("Producto X");
                        table.Cell().Text("1");
                        table.Cell().Text("100.00");
                        table.Cell().Text("18.00");
                        table.Cell().Text("118.00");
                    });

                    col.Item().PaddingTop(15).Text("Sub-Total: 100.00");
                    col.Item().Text("ITBIS: 18.00");
                    col.Item().Text("Total: 118.00");

                    col.Item().PaddingTop(15).Text("Código de Seguridad: N4J8CY");
                });
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] BuildNoTextPdf()
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(20);

                page.Content().Container()
                    .Width(200)
                    .Height(200)
                    .Background(Colors.Blue.Medium);
            });
        });

        return document.GeneratePdf();
    }
}
