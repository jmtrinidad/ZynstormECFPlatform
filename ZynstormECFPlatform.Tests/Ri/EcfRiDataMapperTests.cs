using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Core.Ecf;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

public class EcfRiDataMapperTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void Rfce_UsesCodigoSeguridadeCF_AndFcPortal()
    {
        // Paso_25_E320000000344.xml: root <RFCE>, has <CodigoSeguridadeCF>QuGoOA</CodigoSeguridadeCF>,
        // TipoeCF=32, MontoTotal=6029.00 (<250000).
        var data = EcfRiDataMapper.Map(Load("Paso_25_E320000000344.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E320000000344", data.ENcf);
        Assert.Equal("32", data.TipoeCF);
        Assert.Equal("QuGoOA", data.SecurityCode);
        Assert.Contains("fc.dgii.gov.do/CerteCF/ConsultaTimbreFC", data.QrUrl);
        Assert.Contains($"CodigoSeguridad={data.SecurityCode}", data.QrUrl);
    }

    [Fact]
    public void FullEcf_NoCodigoSeguridadeCF_UsesSignatureValuePrefix_AndFcPortal()
    {
        // SUBIR_DGII_Paso_29_E320000000344.xml: full <ECF>, no <CodigoSeguridadeCF>.
        // <SignatureValue> starts with "QuGoOApeOTL0..." -> first 6 chars = "QuGoOA".
        // TipoeCF=32, MontoTotal=6029.00 (<250000).
        var data = EcfRiDataMapper.Map(Load("SUBIR_DGII_Paso_29_E320000000344.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E320000000344", data.ENcf);
        Assert.Equal("32", data.TipoeCF);
        Assert.Equal(6, data.SecurityCode.Length);
        Assert.Equal("QuGoOA", data.SecurityCode);
        Assert.Contains("fc.dgii.gov.do/CerteCF/ConsultaTimbreFC", data.QrUrl);
        Assert.Contains($"CodigoSeguridad={data.SecurityCode}", data.QrUrl);
    }

    [Fact]
    public void E31_UsesRegularPortal_AndIncludesRncComprador()
    {
        // Paso_1_E310000000402.xml: TipoeCF=31, has <RNCComprador>102620717</RNCComprador>.
        var data = EcfRiDataMapper.Map(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E310000000402", data.ENcf);
        Assert.Equal("31", data.TipoeCF);
        Assert.Contains("ecf.dgii.gov.do/CerteCF/ConsultaTimbre", data.QrUrl);
        Assert.Contains("RncComprador=102620717", data.QrUrl);
        Assert.Equal("102620717", data.Buyer.Document);
        Assert.Equal("MORTEROS DE EUROPA", data.Buyer.Name);
    }
}
