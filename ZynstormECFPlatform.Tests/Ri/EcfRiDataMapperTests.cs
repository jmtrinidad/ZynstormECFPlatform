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
        // I1: MontoTotal must be populated (drives the FC-portal decision and the QR amount).
        Assert.Equal(6029.00m, data.Totals.Total);
        Assert.Contains("MontoTotal=6029", data.QrUrl);
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

    [Fact]
    public void E31_Exento_SubTotalIncludesMontoExento_AndExtractsIdDocFields()
    {
        var data = EcfRiDataMapper.Map(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(6001.00m, data.Totals.SubTotal); // MontoGravadoTotal(0) + MontoExento(6001)
        Assert.Equal("31-12-2028", data.FechaVencimientoSecuencia);
        Assert.Equal(2, data.TipoPago);
        Assert.Equal("02-05-2026", data.FechaLimitePago);
        Assert.Equal("15 DIAS", data.TerminoPago);
        Assert.Equal(string.Empty, data.NumeroFacturaInterna);
        Assert.Equal(43, data.Items[0].UnidadMedida);
    }

    [Fact]
    public void E41_ExtractsRetentions_AndItbisRates()
    {
        var data = EcfRiDataMapper.Map(Load("Paso_E410000000007.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(16064.05m, data.Totals.SubTotal);
        Assert.Equal(2846.53m, data.Totals.ItbisRetenido);
        Assert.Equal(1606.41m, data.Totals.IsrRetencion);
        Assert.Equal(18m, data.Totals.Itbis1Rate);
        Assert.Equal(5, data.Items.Count);
        Assert.Equal(1, data.Items[0].IndicadorFacturacion);
    }

    [Fact]
    public void E34_ExtractsInformacionReferencia()
    {
        var data = EcfRiDataMapper.Map(Load("Paso_E340000000002.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E310000000034", data.NcfModificado);
        Assert.Equal("3", data.CodigoModificacion);
        Assert.Equal("Error en monto", data.RazonModificacion);
        Assert.Equal("123456789016", data.NumeroFacturaInterna);
        Assert.Equal(string.Empty, data.FechaVencimientoSecuencia);
    }
}
