using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTransmissionResultTests
{
    [Theory]
    [InlineData("Aceptado Condicional", 4)]
    [InlineData("Aceptado Condicionalmente", null)]
    public void ConditionalWithoutTrackId_IsAReceivedRfce(string estado, int? codigo)
    {
        var result = new DgiiTransmissionResult { Estado = estado, Codigo = codigo, Error = "Observación DGII" };

        Assert.True(result.IsAcceptedConditional);
        Assert.True(result.Success);
    }

    [Fact]
    public void RejectedWithoutTrackId_IsNotSuccessful()
    {
        var result = new DgiiTransmissionResult { Estado = "Rechazado", Codigo = 2 };

        Assert.False(result.IsAcceptedConditional);
        Assert.False(result.Success);
    }

    [Fact]
    public void AcceptedRfceCodeWithoutTrackId_IsSuccessful()
    {
        Assert.True(new DgiiTransmissionResult { Codigo = 1 }.Success);
    }
}
