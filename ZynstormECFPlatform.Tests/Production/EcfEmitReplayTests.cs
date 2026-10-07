using System.Text.Json;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfEmitReplayTests
{
    private static EcfLookupResponse Lookup(EcfLookupState state) => new()
    {
        ENcf = "E320000000098",
        State = state,
        IsUsable = true,
        TrackId = "TRACK-1",
        SecurityCode = "ABCDEF",
        SignatureDate = "07-10-2026 09:01:23",
        QrUrl = "https://fc.dgii.gov.do/ecf/ConsultaTimbreFC?RncEmisor=132293894&ENCF=E320000000098&MontoTotal=140&CodigoSeguridad=ABCDEF",
        EcfDocumentId = 123
    };

    [Fact]
    public void Accepted_IsASuccessfulReplayWithTheStoredData()
    {
        var result = EcfEmitReplay.ToResult(Lookup(EcfLookupState.Accepted), attempt: 2);

        Assert.True(result.Replayed);
        Assert.Equal(2, result.Attempt);
        Assert.True(result.Success);
        Assert.False(result.IsPending);
        Assert.False(result.IsAcceptedConditional);
        Assert.Equal("TRACK-1", result.TrackId);
        Assert.Equal("ABCDEF", result.SecurityCode);
        Assert.Equal("07-10-2026 09:01:23", result.SignatureDate);
        Assert.Contains("ConsultaTimbreFC", result.QrUrl);
        Assert.Equal(123, result.EcfDocumentId);
        Assert.Equal("E320000000098", result.ENcf);
        Assert.Equal(32, result.EcfType);
    }

    [Fact]
    public void AcceptedConditional_IsFlaggedAndNotASuccess()
    {
        var result = EcfEmitReplay.ToResult(Lookup(EcfLookupState.AcceptedConditional), attempt: 1);

        Assert.False(result.Success);
        Assert.True(result.IsAcceptedConditional);
        Assert.False(result.IsPending);
    }

    [Fact]
    public void Pending_IsPendingAndNotASuccess()
    {
        var result = EcfEmitReplay.ToResult(Lookup(EcfLookupState.Pending), attempt: 1);

        Assert.False(result.Success);
        Assert.True(result.IsPending);
        Assert.True(result.Replayed);
    }

    [Theory]
    [InlineData(EcfLookupState.Accepted)]
    [InlineData(EcfLookupState.AcceptedConditional)]
    [InlineData(EcfLookupState.Pending)]
    public void Message_NeverContainsRejectionWords(EcfLookupState state)
    {
        // La librería de e-CF busca "Rechaz" en el mensaje para clasificar un rechazo.
        var message = EcfEmitReplay.ToResult(Lookup(state), attempt: 1).Message;

        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.DoesNotContain("Rechaz", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingSecurityData_BecomesEmptyStrings()
    {
        var lookup = Lookup(EcfLookupState.Accepted) with { SecurityCode = null, SignatureDate = null, QrUrl = null, TrackId = null };

        var result = EcfEmitReplay.ToResult(lookup, attempt: 1);

        Assert.Equal(string.Empty, result.SecurityCode);
        Assert.Equal(string.Empty, result.SignatureDate);
        Assert.Equal(string.Empty, result.QrUrl);
        Assert.Equal(string.Empty, result.TrackId);
    }

    [Fact]
    public void Serializes_ReplayedAndAttempt()
    {
        var json = JsonSerializer.Serialize(
            EcfEmitReplay.ToResult(Lookup(EcfLookupState.Accepted), attempt: 3),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"replayed\":true", json);
        Assert.Contains("\"attempt\":3", json);
    }
}
