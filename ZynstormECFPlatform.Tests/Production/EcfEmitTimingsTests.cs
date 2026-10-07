using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfEmitTimingsTests
{
    [Fact]
    public void Mark_RecordsTheTimeSinceThePreviousMark()
    {
        var elapsed = 0L;
        var timings = new EcfEmitTimings(() => elapsed);

        elapsed = 40;
        timings.Mark("consultas_previas");
        elapsed = 100;
        timings.Mark("firma");

        Assert.Equal("consultas_previas=40ms, firma=60ms, total=100ms", timings.ToString());
    }

    [Fact]
    public void WithoutMarks_OnlyTheTotalIsReported()
    {
        var timings = new EcfEmitTimings(() => 7);

        Assert.Equal("total=7ms", timings.ToString());
    }

    [Fact]
    public void RealClock_NeverGoesNegative()
    {
        var timings = new EcfEmitTimings();
        timings.Mark("fase");

        Assert.DoesNotContain("-", timings.ToString());
    }
}
