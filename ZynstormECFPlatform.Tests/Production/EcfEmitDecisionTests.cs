using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfEmitDecisionTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Stale = TimeSpan.FromMinutes(5);

    private static EcfEmitExisting Doc(
        int id, int statusId, int minutesAgo = 0, bool track = false, EcfLookupState? state = null) =>
        new(id, statusId, state ?? EcfLookupLogic.ResolveState(statusId, null), Now.AddMinutes(-minutesAgo), track);

    private static EcfEmitDecisionResult Decide(params EcfEmitExisting[] existing) =>
        EcfEmitDecision.Decide(existing, Now, Stale);

    [Fact]
    public void NoDocuments_Creates()
    {
        var result = Decide();

        Assert.Equal(EcfEmitAction.Create, result.Action);
        Assert.Null(result.ReplayDocumentId);
    }

    [Fact]
    public void Accepted_Replays()
    {
        var result = Decide(Doc(1, 10));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(1, result.ReplayDocumentId);
        Assert.Equal(EcfLookupState.Accepted, result.ReplayState);
    }

    [Fact]
    public void AcceptedConditional_Replays()
    {
        var result = Decide(Doc(1, 11, state: EcfLookupState.AcceptedConditional));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(EcfLookupState.AcceptedConditional, result.ReplayState);
    }

    [Fact]
    public void Invoice118_AcceptedFirstThenRejectedDuplicate_ReplaysTheAcceptedOne()
    {
        var result = Decide(Doc(1, 10, minutesAgo: 1), Doc(2, 11, minutesAgo: 0));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(1, result.ReplayDocumentId);
    }

    [Fact]
    public void Accepted_BeatsAnyInFlightDocument()
    {
        var result = Decide(Doc(1, 7, minutesAgo: 1), Doc(2, 10, minutesAgo: 30));

        Assert.Equal(2, result.ReplayDocumentId);
        Assert.Equal(EcfLookupState.Accepted, result.ReplayState);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void RecentInFlight_ReplaysAsPending(int statusId)
    {
        var result = Decide(Doc(1, statusId, minutesAgo: 1));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(EcfLookupState.Pending, result.ReplayState);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(8)]
    public void StaleInFlightWithoutTrackId_Retries(int statusId)
    {
        var result = Decide(Doc(1, statusId, minutesAgo: 6));

        Assert.Equal(EcfEmitAction.Retry, result.Action);
    }

    [Fact]
    public void StaleThreshold_IsExclusive()
    {
        Assert.Equal(EcfEmitAction.Replay, Decide(Doc(1, 8, minutesAgo: 4)).Action);
        Assert.Equal(EcfEmitAction.Retry, Decide(Doc(1, 8, minutesAgo: 5)).Action);
    }

    [Fact]
    public void StalePendingWithTrackId_StillReplays()
    {
        // La DGII ya tiene el documento: el job de seguimiento lo resolverá.
        var result = Decide(Doc(1, 9, minutesAgo: 120, track: true));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(EcfLookupState.Pending, result.ReplayState);
    }

    [Theory]
    [InlineData(3)]    // ValidationFailed
    [InlineData(11)]   // Rejected
    [InlineData(12)]   // Error
    [InlineData(13)]   // Cancelled
    public void FailedDocuments_Retry(int statusId)
    {
        var result = Decide(Doc(1, statusId, minutesAgo: 0));

        Assert.Equal(EcfEmitAction.Retry, result.Action);
        Assert.Null(result.ReplayDocumentId);
    }

    [Fact]
    public void InFlight_BeatsFailedDocuments()
    {
        var result = Decide(Doc(1, 11, minutesAgo: 10), Doc(2, 7, minutesAgo: 1));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(2, result.ReplayDocumentId);
    }

    [Fact]
    public void SeveralInFlight_ReplaysTheMostRecent()
    {
        var result = Decide(Doc(1, 8, minutesAgo: 3), Doc(2, 8, minutesAgo: 1));

        Assert.Equal(2, result.ReplayDocumentId);
    }

    // ─── LockKey ───────────────────────────────────────────────────

    [Fact]
    public void LockKey_IsStableForTheSameClientAndNcf()
    {
        Assert.Equal(
            EcfEmitDecision.LockKey(7, "E320000000098"),
            EcfEmitDecision.LockKey(7, "E320000000098"));
    }

    [Fact]
    public void LockKey_DiffersByNcfAndByClient()
    {
        var baseKey = EcfEmitDecision.LockKey(7, "E320000000098");

        Assert.NotEqual(baseKey, EcfEmitDecision.LockKey(7, "E320000000099"));
        Assert.NotEqual(baseKey, EcfEmitDecision.LockKey(8, "E320000000098"));
    }
}
