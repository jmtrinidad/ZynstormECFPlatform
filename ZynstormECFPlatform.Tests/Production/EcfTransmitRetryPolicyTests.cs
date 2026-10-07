using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfTransmitRetryPolicyTests
{
    // ─── Normalize ─────────────────────────────────────────────────

    [Fact]
    public void Normalize_WithoutConfiguration_UsesTheDefaults()
    {
        var delays = EcfTransmitRetryPolicy.Normalize(null);

        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120)], delays);
    }

    [Fact]
    public void Normalize_EmptyConfiguration_UsesTheDefaults()
    {
        Assert.Equal(3, EcfTransmitRetryPolicy.Normalize([]).Count);
    }

    [Fact]
    public void Normalize_KeepsTheConfiguredValuesInOrder()
    {
        var delays = EcfTransmitRetryPolicy.Normalize([2, 10]);

        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)], delays);
    }

    [Fact]
    public void Normalize_DropsNonPositiveValuesAndCapsTheMaximum()
    {
        var delays = EcfTransmitRetryPolicy.Normalize([0, -5, 7200]);

        Assert.Equal([TimeSpan.FromSeconds(3600)], delays);
    }

    [Fact]
    public void Normalize_OnlyNonPositiveValues_MeansNoRetries()
    {
        Assert.Empty(EcfTransmitRetryPolicy.Normalize([0]));
    }

    // ─── NextDelay ─────────────────────────────────────────────────

    private static readonly IReadOnlyList<TimeSpan> Delays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120)];

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 30)]
    [InlineData(3, 120)]
    public void NextDelay_AfterEachFailedAttempt_IsTheNextConfiguredDelay(int attemptsMade, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), EcfTransmitRetryPolicy.NextDelay(attemptsMade, Delays));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(10)]
    public void NextDelay_OutOfRange_IsNull(int attemptsMade)
    {
        Assert.Null(EcfTransmitRetryPolicy.NextDelay(attemptsMade, Delays));
    }

    // ─── IsOwnDuplicate ────────────────────────────────────────────

    private static DgiiStatusResponse Rejected(object code, string value = "") => new()
    {
        Estado = "Rechazado",
        Codigo = "2",
        Mensajes = [new DgiiStatusMensaje { Codigo = code, Valor = value }]
    };

    [Theory]
    [InlineData(1209)]
    [InlineData(75)]
    [InlineData("1209")]
    [InlineData("75")]
    public void IsOwnDuplicate_SequenceAlreadyUsedCodes_AfterAnUnconfirmedAttempt(object code)
    {
        Assert.True(EcfTransmitRetryPolicy.IsOwnDuplicate(Rejected(code), earlierAttemptsWithoutTrackId: true));
    }

    [Fact]
    public void IsOwnDuplicate_DetectsTheTextEvenWithoutAKnownCode()
    {
        var status = Rejected(999, "El e-NCF de este Resumen de Factura de Consumo ya ha sido utilizado previamente.");

        Assert.True(EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId: true));
    }

    [Fact]
    public void IsOwnDuplicate_WithoutAnUnconfirmedEarlierAttempt_IsAGenuineBurnedSequence()
    {
        Assert.False(EcfTransmitRetryPolicy.IsOwnDuplicate(Rejected(1209), earlierAttemptsWithoutTrackId: false));
    }

    [Fact]
    public void IsOwnDuplicate_OtherRejections_AreNotDuplicates()
    {
        var status = Rejected(1940, "Monto gravado inválido.");

        Assert.False(EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId: true));
    }
}
