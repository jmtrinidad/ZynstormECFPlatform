namespace ZynstormECFPlatform.Tests.Certification;

/// <summary>
/// Totales del XML real que la simulación genera para las muestras de Renta de Vehículos. La
/// primera versión salía con TotalITBIS1 = 0 y la DGII la rechazó; la cobertura general de todas
/// las muestras está en SeedSamplesItbisTests.
/// </summary>
public class RentaVehiculosXmlTotalsTests
{
    private const int RentaId = 15;

    private static System.Xml.Linq.XElement Totales(string ecfType)
    {
        var sample = SeedSampleXml.All().Single(s => s.BusinessTypeId == RentaId && s.EcfType == ecfType);
        return SeedSampleXml.GenerateTotales(sample);
    }

    // Gravados con ITBIS 18%: el monto gravado a la tasa 1 existe y el ITBIS cuadra.
    [Theory]
    [InlineData("31")]
    [InlineData("32")]
    [InlineData("33")]
    [InlineData("34")]
    [InlineData("41")]
    [InlineData("45")]
    public void TaxedSamples_DeclareTaxableAmountWithMatchingItbis(string ecfType)
    {
        var totales = Totales(ecfType);

        Assert.True(SeedSampleXml.Value(totales, "MontoGravadoI1") > 0);
        Assert.True(SeedSampleXml.Value(totales, "TotalITBIS1") > 0);
        SeedSampleXml.AssertItbisConsistent(totales);
    }

    // 43 (gastos menores), 44 (regímenes especiales) y 47 (pagos al exterior) no llevan ITBIS:
    // declaran solo monto exento, como las muestras de Transporte y Clínica.
    [Theory]
    [InlineData("43")]
    [InlineData("44")]
    [InlineData("47")]
    public void ExemptSamples_DeclareExemptAmountOnly(string ecfType)
    {
        var totales = Totales(ecfType);

        Assert.Null(SeedSampleXml.Value(totales, "MontoGravadoI1"));
        Assert.True(SeedSampleXml.Value(totales, "MontoExento") > 0);
    }

    [Fact]
    public void Type46_KeepsItsZeroRateExportTreatment()
    {
        var totales = Totales("46");

        Assert.Null(SeedSampleXml.Value(totales, "MontoGravadoI1"));
        Assert.True(SeedSampleXml.Value(totales, "MontoGravadoI3") > 0);
    }
}
