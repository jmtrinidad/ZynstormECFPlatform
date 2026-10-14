using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri.Received;

namespace ZynstormECFPlatform.Tests.Ri.Received;

public class ReceivedEcfMapperTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void Map_Type31_FillsIssuerBuyerAndTotals()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.Production);

        Assert.Equal(31, model.EcfType);
        Assert.Equal("E310000000402", model.ENcf);
        Assert.Equal("FACTURA DE CRÉDITO FISCAL ELECTRÓNICA", model.TypeName);
        Assert.Equal("133009889", model.Issuer.Document);
        Assert.Equal("102620717", model.Buyer.Document);
        Assert.Equal(6001.00m, model.Total);
        Assert.NotEmpty(model.Items);
        Assert.Contains("ConsultaTimbre", model.Qr);
        Assert.False(string.IsNullOrWhiteSpace(model.SecurityCode));
    }

    [Fact]
    public void Map_Type34_FillsReferenceAndTaxRate()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E340000000002.xml"), DgiiEnvironment.Production);

        Assert.Equal(34, model.EcfType);
        Assert.Equal("NOTA DE CRÉDITO ELECTRÓNICA", model.TypeName);
        Assert.Equal("E310000000034", model.AffectedNcf);
        Assert.Equal("3 - Corrige montos del NCF modificado", model.ModificationCode);
        Assert.Equal("Error en monto", model.ModificationReason);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(18m, model.ItbisRate);
        Assert.Equal(86445.00m, model.Itbis);
        Assert.Equal(566695.00m, model.Total);
        Assert.Equal("Lb", model.Items[0].Unit);
    }

    [Fact]
    public void Map_Type43_WithoutBuyer_LeavesBuyerEmpty()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E430000000008.xml"), DgiiEnvironment.Production);

        Assert.Equal(43, model.EcfType);
        Assert.Equal(string.Empty, model.Buyer.Name);
        Assert.Equal(string.Empty, model.Buyer.Document);
    }

    [Fact]
    public void Map_InvalidXml_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ReceivedEcfMapper.Map("no es xml", DgiiEnvironment.Production));
    }
}
