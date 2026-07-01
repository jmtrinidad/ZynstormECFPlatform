using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Core.Ecf;
using Xunit;

public class EcfQrUrlBuilderTests
{
    [Fact]
    public void E32_Under250k_UsesFcPortal_MontoSinCerosSobrantes()
    {
        var url = EcfQrUrlBuilder.Build(DgiiEnvironment.CerteCF, 32, "1-32-29389-4", "", "E320000000028",
            "30-06-2026", 1180.00m, "30-06-2026 18:34:41", "N4J8CY");
        Assert.Equal(
            "https://fc.dgii.gov.do/CerteCF/ConsultaTimbreFC?RncEmisor=132293894&ENCF=E320000000028&MontoTotal=1180&CodigoSeguridad=N4J8CY",
            url);
    }

    [Fact]
    public void E31_UsesRegularPortal_ConComprador()
    {
        var url = EcfQrUrlBuilder.Build(DgiiEnvironment.CerteCF, 31, "132293894", "131880681", "E310000000001",
            "30-06-2026", 6029.5m, "30-06-2026 10:00:00", "AbC123");
        Assert.StartsWith("https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?RncEmisor=132293894&RncComprador=131880681", url);
        Assert.Contains("MontoTotal=6029.5", url);
        Assert.Contains("FechaFirma=30-06-2026%2010:00:00", url);
    }
}
