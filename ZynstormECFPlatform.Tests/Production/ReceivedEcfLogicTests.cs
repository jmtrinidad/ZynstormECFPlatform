using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class ReceivedEcfLogicTests
{
    private static ReceivedB2BMessage Message(string rnc, DateTime receivedAt, string encf = "E310000000402", string xml = "") =>
        new()
        {
            ReceivedB2BMessageId = 1,
            ClientId = 7,
            MessageType = MessageType.Ecf,
            RncEmisor = rnc,
            RncComprador = "101010101",
            ENcf = encf,
            RawXml = xml,
            ReceivedAtUtc = receivedAt
        };

    [Theory]
    [InlineData("1-33-00988-9", "133009889")]
    [InlineData(" 133009889 ", "133009889")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeRnc_KeepsOnlyDigits(string? raw, string expected)
    {
        Assert.Equal(expected, ReceivedEcfLogic.NormalizeRnc(raw));
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(0, 100)]
    [InlineData(-5, 100)]
    [InlineData(25, 25)]
    [InlineData(200, 200)]
    [InlineData(5000, 200)]
    public void ClampLimit_AppliesDefaultAndMaximum(int? raw, int expected)
    {
        Assert.Equal(expected, ReceivedEcfLogic.ClampLimit(raw));
    }

    [Fact]
    public void Pick_NoCandidates_IsNotFound()
    {
        Assert.Equal(ReceivedEcfPickKind.NotFound, ReceivedEcfLogic.Pick([], null).Kind);
    }

    [Fact]
    public void Pick_SingleEmisor_ReturnsLatestResend()
    {
        var older = Message("133009889", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = Message("133009889", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));

        var pick = ReceivedEcfLogic.Pick([older, newer], null);

        Assert.Equal(ReceivedEcfPickKind.Found, pick.Kind);
        Assert.Same(newer, pick.Message);
    }

    [Fact]
    public void Pick_TwoEmisores_WithoutRnc_IsAmbiguous()
    {
        var a = Message("133009889", DateTime.UtcNow);
        var b = Message("102620717", DateTime.UtcNow);

        var pick = ReceivedEcfLogic.Pick([a, b], null);

        Assert.Equal(ReceivedEcfPickKind.Ambiguous, pick.Kind);
        Assert.Equal(new[] { "102620717", "133009889" }, pick.Emisores.ToArray());
    }

    [Fact]
    public void Pick_TwoEmisores_WithRnc_NarrowsToThatEmisor()
    {
        var a = Message("133009889", DateTime.UtcNow);
        var b = Message("102620717", DateTime.UtcNow);

        var pick = ReceivedEcfLogic.Pick([a, b], "1-02-62071-7");

        Assert.Equal(ReceivedEcfPickKind.Found, pick.Kind);
        Assert.Same(b, pick.Message);
    }

    [Fact]
    public void Pick_RncThatDoesNotMatch_IsNotFound()
    {
        var a = Message("133009889", DateTime.UtcNow);

        Assert.Equal(ReceivedEcfPickKind.NotFound, ReceivedEcfLogic.Pick([a], "999999999").Kind);
    }

    [Fact]
    public void ReadTotal_ReadsMontoTotal_AndIsNullWhenUnreadable()
    {
        Assert.Equal(1180.50m,
            ReceivedEcfLogic.ReadTotal("<ECF><Encabezado><Totales><MontoTotal>1180.50</MontoTotal></Totales></Encabezado></ECF>"));
        Assert.Null(ReceivedEcfLogic.ReadTotal("no es xml"));
        Assert.Null(ReceivedEcfLogic.ReadTotal("<ECF/>"));
    }

    [Fact]
    public void ToListItem_TakesTypeFromNcf()
    {
        var item = ReceivedEcfLogic.ToListItem(Message("133009889", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), "E340000000002"));

        Assert.Equal("E340000000002", item.ENcf);
        Assert.Equal(34, item.EcfType);
        Assert.Equal("133009889", item.RncEmisor);
        Assert.Null(item.Total);
    }

    [Theory]
    [InlineData(null, DgiiEnvironment.Production)]
    [InlineData("", DgiiEnvironment.Production)]
    [InlineData("basura", DgiiEnvironment.Production)]
    [InlineData("Test", DgiiEnvironment.Test)]
    [InlineData("certecf", DgiiEnvironment.CerteCF)]
    public void ResolveEnvironment_FallsBackToProduction(string? configured, DgiiEnvironment expected)
    {
        Assert.Equal(expected, ReceivedEcfLogic.ResolveEnvironment(configured));
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData("", true, null)]
    [InlineData("2026-10-14T12:30:00Z", true, "2026-10-14T12:30:00.0000000Z")]
    [InlineData("2026-10-14T08:30:00-04:00", true, "2026-10-14T12:30:00.0000000Z")]
    [InlineData("2026-10-14T12:30:00", true, "2026-10-14T12:30:00.0000000Z")]
    [InlineData("ayer", false, null)]
    public void TryParseSince_ReadsIsoAsUtc(string? raw, bool ok, string? expected)
    {
        var result = ReceivedEcfLogic.TryParseSince(raw, out var since);

        Assert.Equal(ok, result);
        Assert.Equal(expected, since?.ToString("O"));
        if (since is not null)
        {
            Assert.Equal(DateTimeKind.Utc, since.Value.Kind);
        }
    }
}
