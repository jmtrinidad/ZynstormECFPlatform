using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfRequeuePolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Stale = TimeSpan.FromMinutes(5);

    private static EcfRequeueAction? Evaluate(int status, bool xml = true, bool trackId = false, double idleMinutes = 10) =>
        EcfRequeuePolicy.Evaluate(new EcfRequeueSnapshot(status, xml, trackId, Now.AddMinutes(-idleMinutes)), Now, Stale);

    [Theory]
    [InlineData(12)]
    public void ErrorWithoutTrackId_IsTransmit(int status) =>
        Assert.Equal(EcfRequeueAction.Transmit, Evaluate(status));

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void InFlightWithoutTrackId_IsTransmitOnlyAfterTheStaleWindow(int status)
    {
        Assert.Equal(EcfRequeueAction.Transmit, Evaluate(status, idleMinutes: 5));
        Assert.Null(Evaluate(status, idleMinutes: 4.9));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(12)]
    public void WithTrackId_IsTrackAfterTwoMinutes(int status)
    {
        Assert.Equal(EcfRequeueAction.Track, Evaluate(status, trackId: true, idleMinutes: 2));
        Assert.Null(Evaluate(status, trackId: true, idleMinutes: 1.9));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(6)]
    public void ExcludedStatuses_AreNeverRequeueable(int status)
    {
        Assert.Null(Evaluate(status));
        Assert.Null(Evaluate(status, trackId: true));
    }

    [Fact]
    public void WithoutSignedXml_IsNeverRequeueable() => Assert.Null(Evaluate(12, xml: false));

    [Fact]
    public void ChangedInTheLastMinute_IsNotRequeueable()
    {
        Assert.Null(Evaluate(12, idleMinutes: 0.5));
        Assert.Equal(EcfRequeueAction.Transmit, Evaluate(12, idleMinutes: 1));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(5, 6)]
    public void NextAttemptNumber_IsAtLeastTwo(int transmissions, int expected) =>
        Assert.Equal(expected, EcfRequeuePolicy.NextAttemptNumber(transmissions));
}
