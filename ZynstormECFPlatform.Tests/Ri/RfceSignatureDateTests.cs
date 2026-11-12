using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

public class RfceSignatureDateTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void Rfce_TakesFechaFirmaFromFullEcfSibling()
    {
        var rfce = Load("Paso_25_E320000000344.xml");
        var full = Load("SUBIR_DGII_Paso_29_E320000000344.xml");

        Assert.Equal(string.Empty, EcfRiDataMapper.Map(rfce, DgiiEnvironment.CerteCF).FechaFirma);

        var completed = RfceSignatureDate.Complete(rfce, new string?[] { null, full });
        var data = EcfRiDataMapper.Map(completed, DgiiEnvironment.CerteCF);

        Assert.Equal("24-04-2026 22:12:08", data.FechaFirma);
        Assert.Equal("QuGoOA", data.SecurityCode);
        Assert.Equal(6029.00m, data.Totals.Total);
    }

    [Fact]
    public void Rfce_WithoutSibling_IsUnchanged()
    {
        var rfce = Load("Paso_25_E320000000344.xml");
        Assert.Equal(rfce, RfceSignatureDate.Complete(rfce, Array.Empty<string?>()));
    }

    [Fact]
    public void FullEcf_IsUnchanged()
    {
        var full = Load("SUBIR_DGII_Paso_29_E320000000344.xml");
        Assert.Equal(full, RfceSignatureDate.Complete(full, new string?[] { Load("Paso_25_E320000000344.xml") }));
    }
}
