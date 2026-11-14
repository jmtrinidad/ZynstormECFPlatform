namespace ZynstormECFPlatform.Tests.Certification;

/// <summary>
/// Guarda contra el rechazo "TotalITBIS no es válido / TotalITBIS1 no coincide con MontoGravado1 x
/// ITBIS Tasa 1". El simulador calcula el ITBIS con TaxPercentage; una muestra gravada (indicador 1)
/// sin TaxPercentage produce ITBIS 0 y la DGII la rechaza. Cubre TODAS las muestras sembradas, así
/// que una muestra nueva con ese defecto hace fallar esta prueba antes de llegar a la DGII.
/// </summary>
public class SeedSamplesItbisTests
{
    public static IEnumerable<object[]> SampleIds() =>
        SeedSampleXml.All().Select(s => new object[] { s.Id });

    [Theory]
    [MemberData(nameof(SampleIds))]
    public void EverySeedSample_HasConsistentItbisTotals(int sampleId)
    {
        var sample = SeedSampleXml.ById(sampleId);
        var totales = SeedSampleXml.GenerateTotales(sample);

        SeedSampleXml.AssertItbisConsistent(totales);
    }
}
