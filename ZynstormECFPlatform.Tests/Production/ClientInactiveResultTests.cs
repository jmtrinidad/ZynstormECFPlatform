using System.Text.Json;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class ClientInactiveResultTests
{
    [Fact]
    public void BuildClientInactiveResult_SetsFlagMessageAndFailure()
    {
        var result = ReceivedEcfProductionService.BuildClientInactiveResult(new ReceivedEcfEmissionResultDto { Success = true });

        Assert.False(result.Success);
        Assert.True(result.ClientInactive);
        Assert.Equal("El cliente se encuentra desactivado. Por favor, comuníquese con soporte.", result.Message);
        Assert.Equal(0, result.EcfDocumentId);
    }

    [Fact]
    public void ResultDto_SerializesClientInactiveAndEcfDocumentId()
    {
        var json = JsonSerializer.Serialize(
            new ReceivedEcfEmissionResultDto { ClientInactive = true, EcfDocumentId = 42 },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"clientInactive\":true", json);
        Assert.Contains("\"ecfDocumentId\":42", json);
        Assert.DoesNotContain("hasUnexpectedError", json);
    }
}
