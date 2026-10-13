using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiSendGuardTests
{
    [Fact]
    public async Task ATimeout_BecomesATransportFailureResult()
    {
        var result = await DgiiSendGuard.SendAsync(
            () => throw new TaskCanceledException("timeout"),
            isSummary: false);

        Assert.True(result.TransportFailure);
        Assert.False(result.Success);
        Assert.Contains("e-CF", result.Error);
        Assert.Equal("timeout", result.Mensaje);
    }

    [Fact]
    public async Task ATimeoutOnASummary_NamesTheSummaryChannel()
    {
        var result = await DgiiSendGuard.SendAsync(
            () => throw new TaskCanceledException("timeout"),
            isSummary: true);

        Assert.Contains("Resumen B2C/RFCE", result.Error);
    }

    [Fact]
    public async Task ASuccessfulSend_IsReturnedUntouched()
    {
        var expected = new DgiiTransmissionResult { TrackId = "T1" };

        var result = await DgiiSendGuard.SendAsync(() => Task.FromResult(expected), isSummary: false);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task AnyOtherException_Propagates()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => DgiiSendGuard.SendAsync(
            () => throw new InvalidOperationException("otro fallo"),
            isSummary: false));
    }
}
