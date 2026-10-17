using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Tests.Dgii;

public class DgiiRefreshPolicyTests
{
    private static readonly DateTime Ahora = new(2026, 9, 1, 8, 0, 0);
    private const long PadronCompleto = 800_000;

    [Fact]
    public void SinRegistros_Descarga()
    {
        Assert.True(DgiiRefreshPolicy.ShouldRefreshOnStartup(0, Ahora, Ahora, 2));
    }

    [Fact]
    public void ConRegistrosPeroSinFecha_Descarga()
    {
        Assert.True(DgiiRefreshPolicy.ShouldRefreshOnStartup(PadronCompleto, null, Ahora, 2));
    }

    // Un arranque a las 00:05, minutos despues de que el job corrio, no puede disparar una
    // segunda descarga de 60 MB.
    [Fact]
    public void PadronReciente_NoDescarga()
    {
        Assert.False(DgiiRefreshPolicy.ShouldRefreshOnStartup(PadronCompleto, Ahora.AddMinutes(-5), Ahora, 2));
    }

    [Fact]
    public void PadronDeAyer_NoDescarga()
    {
        Assert.False(DgiiRefreshPolicy.ShouldRefreshOnStartup(PadronCompleto, Ahora.AddDays(-1), Ahora, 2));
    }

    [Fact]
    public void PadronJustoEnElLimite_Descarga()
    {
        Assert.True(DgiiRefreshPolicy.ShouldRefreshOnStartup(PadronCompleto, Ahora.AddDays(-2), Ahora, 2));
    }

    [Fact]
    public void PadronViejo_Descarga()
    {
        Assert.True(DgiiRefreshPolicy.ShouldRefreshOnStartup(PadronCompleto, Ahora.AddDays(-30), Ahora, 2));
    }

    [Fact]
    public void MaxAgeEnCero_ApagaElVencimientoPeroNoElPadronAusente()
    {
        Assert.False(DgiiRefreshPolicy.ShouldRefreshOnStartup(PadronCompleto, Ahora.AddDays(-30), Ahora, 0));
        Assert.True(DgiiRefreshPolicy.ShouldRefreshOnStartup(0, Ahora.AddDays(-30), Ahora, 0));
    }
}
