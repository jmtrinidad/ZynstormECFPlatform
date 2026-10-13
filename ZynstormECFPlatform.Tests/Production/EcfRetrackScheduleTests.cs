using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfRetrackScheduleTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(3, 5)]
    [InlineData(4, 30)]
    [InlineData(6, 30)]
    [InlineData(7, 120)]
    [InlineData(9, 120)]
    public void NextDelay_GrowsWithTheAttempts(int attemptsMade, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), EcfRetrackSchedule.NextDelay(attemptsMade));

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(11)]
    public void NextDelay_IsNullOutsideTheRange(int attemptsMade) =>
        Assert.Null(EcfRetrackSchedule.NextDelay(attemptsMade));
}
