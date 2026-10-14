using QuestPDF.Fluent;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri.Received;

namespace ZynstormECFPlatform.Tests.Ri.Received;

public class ReceivedEcfPreviewTests
{
    /// <summary>
    /// Con RI_PREVIEW_DIR definida deja PDF y PNG de cada fixture en esa carpeta, para revisar el
    /// diseño a ojo. Sin la variable no hace nada.
    /// </summary>
    [Fact]
    public void WritePreviews_WhenRequested()
    {
        var dir = Environment.GetEnvironmentVariable("RI_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        foreach (var fixture in new[] { "Paso_1_E310000000402", "Paso_E340000000002", "Paso_E430000000008" })
        {
            var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", fixture + ".xml"));
            var model = ReceivedEcfMapper.Map(xml, DgiiEnvironment.Production);
            var document = new ReceivedEcfPdf(model);

            File.WriteAllBytes(Path.Combine(dir, fixture + ".pdf"), document.GeneratePdf());
            var page = 1;
            foreach (var png in document.GenerateImages())
            {
                File.WriteAllBytes(Path.Combine(dir, $"{fixture}-p{page++}.png"), png);
            }
        }
    }
}
