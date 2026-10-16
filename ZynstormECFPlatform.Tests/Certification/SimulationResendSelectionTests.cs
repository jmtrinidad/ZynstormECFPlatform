using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Certification.OldSimulation;

namespace ZynstormECFPlatform.Tests.Certification;

public class SimulationResendSelectionTests
{
    [Theory]
    [InlineData(31, false, false, "31")]
    [InlineData(32, false, false, "32-250K")]
    [InlineData(32, true, false, "32-RFCE")]
    [InlineData(32, false, true, "32-MANUAL")]
    [InlineData(47, false, false, "47")]
    public void GroupOfRow_MapsMatrixRows(int type, bool isSummary, bool isManual, string expected)
    {
        Assert.Equal(expected, SimulationResendSelection.GroupOfRow(type, isSummary, isManual));
    }

    [Fact]
    public void GroupOfDocument_ClassifiesThe32Variants()
    {
        Assert.Equal("32-MANUAL", SimulationResendSelection.GroupOfDocument("32", "MANUAL", "<ECF/>"));
        Assert.Equal("32-RFCE", SimulationResendSelection.GroupOfDocument("32", "abc", "<RFCE><Encabezado/></RFCE>"));
        Assert.Equal("32-250K", SimulationResendSelection.GroupOfDocument("32", "abc", "<ECF><Encabezado/></ECF>"));
        Assert.Equal("34", SimulationResendSelection.GroupOfDocument("34", "abc", "<ECF/>"));
    }

    [Fact]
    public void RunsRow_FullSimulationRunsEverything_RfceAlsoRunsManual()
    {
        Assert.True(SimulationResendSelection.RunsRow(null, "41"));
        Assert.True(SimulationResendSelection.RunsRow("34", "34"));
        Assert.False(SimulationResendSelection.RunsRow("34", "31"));
        Assert.True(SimulationResendSelection.RunsRow("32-RFCE", "32-MANUAL"));
        Assert.False(SimulationResendSelection.RunsRow("32-250K", "32-RFCE"));
    }

    [Fact]
    public void IsResendableGroup_ExcludesManualAndUnknown()
    {
        Assert.True(SimulationResendSelection.IsResendableGroup("32-RFCE"));
        Assert.True(SimulationResendSelection.IsResendableGroup("45"));
        Assert.False(SimulationResendSelection.IsResendableGroup("32-MANUAL"));
        Assert.False(SimulationResendSelection.IsResendableGroup("99"));
        Assert.False(SimulationResendSelection.IsResendableGroup(null));
    }

    [Fact]
    public void AddCount_IncrementsTheMatchingCounter()
    {
        var stats = new SimulationStatsDto();
        SimulationResendSelection.AddCount(stats, "31");
        SimulationResendSelection.AddCount(stats, "31");
        SimulationResendSelection.AddCount(stats, "32-RFCE");
        SimulationResendSelection.AddCount(stats, "32-MANUAL");
        SimulationResendSelection.AddCount(stats, "32-250K");

        Assert.Equal(2, stats.Type31);
        Assert.Equal(1, stats.Type32Rfce);
        Assert.Equal(1, stats.Type32Manual);
        Assert.Equal(1, stats.Type32Greater250k);
    }

    [Fact]
    public void ParseAccepted31_ReadsReferenceFieldsFromSignedXml()
    {
        const string xml = """
            <ECF>
              <Encabezado>
                <IdDoc><TipoeCF>31</TipoeCF><eNCF>E310000000007</eNCF></IdDoc>
                <Emisor><FechaEmision>08-10-2026</FechaEmision></Emisor>
                <Comprador><RNCComprador>131880681</RNCComprador><RazonSocialComprador>DOCUMENTOS ELECTRONICOS DE 03</RazonSocialComprador></Comprador>
              </Encabezado>
              <DetallesItems>
                <Item><NumeroLinea>1</NumeroLinea><PrecioUnitarioItem>245.0000</PrecioUnitarioItem></Item>
                <Item><NumeroLinea>2</NumeroLinea><PrecioUnitarioItem>10.00</PrecioUnitarioItem></Item>
              </DetallesItems>
            </ECF>
            """;

        var r = SimulationResendSelection.ParseAccepted31(xml);

        Assert.NotNull(r);
        Assert.Equal("E310000000007", r!.Ncf);
        Assert.Equal(new DateTime(2026, 10, 8), r.IssueDate);
        Assert.Equal("131880681", r.CustomerRnc);
        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 03", r.CustomerName);
        Assert.Equal(245.0000m, r.FirstItemUnitPrice);
    }

    [Fact]
    public void ParseAccepted31_ReturnsNullForInvalidXml()
    {
        Assert.Null(SimulationResendSelection.ParseAccepted31("no es xml"));
        Assert.Null(SimulationResendSelection.ParseAccepted31(null));
    }
}
