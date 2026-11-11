using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class AccountSuspendedResultTests
{
    [Fact]
    public void BuildAccountSuspendedResult_UsesTheClientInactiveFlagSoTheApiAnswers403()
    {
        var result = ReceivedEcfProductionService.BuildAccountSuspendedResult(new ReceivedEcfEmissionResultDto { Success = true });

        Assert.False(result.Success);
        Assert.True(result.ClientInactive);
        Assert.Equal(ReceivedEcfProductionService.AccountSuspendedMessage, result.Message);
        Assert.Equal(0, result.EcfDocumentId);
    }
}
