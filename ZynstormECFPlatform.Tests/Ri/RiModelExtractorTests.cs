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
    public void KnownFormat_DetectsColumnsAndFieldSlots()
    {
        var pdfBytes = BuildKnownFormatPdf();

        var result = RiModelExtractor.Extract(pdfBytes);

        Assert.True(result.Success);
        Assert.NotNull(result.Layout);
        Assert.True(result.Layout!.Items.Columns.Count >= 3);
        Assert.Contains("eNCF", result.Layout.FieldSlots.Keys);
    }

    [Fact]
    public void PdfSinTexto_MarcaFailed_ConWarning()
    {
        var pdfBytes = BuildNoTextPdf();

        var result = RiModelExtractor.Extract(pdfBytes);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Warnings);
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
                    col.Item().Text("MI EMPRESA SRL").FontSize(16).Bold();
                    col.Item().Text("RNC: 132293894");
                    col.Item().Text("NCF: E320000000028");
                    col.Item().Text("Fecha: 30-06-2026");

                    col.Item().PaddingTop(10).Text("Cliente: Juan Perez");
                    col.Item().Text("RNC: 131880681");

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
                            header.Cell().Text("Valor");
                        });

                        table.Cell().Text("Servicio X");
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
