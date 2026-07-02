using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class EcfRiTemplateMapperTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void MapInvoice_Rfce_PopulatesModel_AndUsesFcPortal()
    {
        // Paso_25_E320000000344.xml: RFCE, TipoeCF=32, MontoTotal=6029.00 (<250000),
        // CodigoSeguridadeCF=QuGoOA, no ítems -> resumen consumidor.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_25_E320000000344.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E320000000344", model.NcfNumber);
        Assert.Equal(32, model.EcfType);
        Assert.Equal(6029.00m, model.Total);
        Assert.NotEmpty(model.Items);
        Assert.Equal("QuGoOA", model.SecurityCode);
        Assert.Contains("fc.dgii.gov.do/CerteCF/ConsultaTimbreFC", model.Qr);
        Assert.Contains("CodigoSeguridad=QuGoOA", model.Qr);
    }

    [Fact]
    public void MapInvoice_FullEcf_PopulatesModel_AndUsesRegularPortal()
    {
        // Paso_1_E310000000402.xml: ECF, TipoeCF=31, RNCComprador=102620717,
        // 1 <Item>, MontoTotal=6001.00.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E310000000402", model.NcfNumber);
        Assert.Equal(31, model.EcfType);
        Assert.Equal(6001.00m, model.Total);
        Assert.Single(model.Items);
        // EcfRiDataMapper prefers DescripcionItem over NombreItem when both are present.
        Assert.Equal(
            "Factura: 310013315 | Fecha: 01/04/2026 | Cliente: Tiles Import y Export | Origen: Morteros Santiago | Destino: Santiago",
            model.Items[0].Description);
        Assert.Equal("MORTEROS DE EUROPA", model.Client.Name);
        Assert.Equal("102620717", model.Client.Rnc);
        Assert.Contains("ecf.dgii.gov.do/CerteCF/ConsultaTimbre", model.Qr);
        Assert.Contains("RncComprador=102620717", model.Qr);
        Assert.NotEmpty(model.SecurityCode);
    }
}
