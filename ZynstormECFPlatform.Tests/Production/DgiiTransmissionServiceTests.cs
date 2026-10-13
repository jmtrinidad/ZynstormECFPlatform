using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTransmissionServiceTests
{
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    private static DgiiTransmissionService ServiceThatThrows(Exception exception)
    {
        // El validador interno usa PlatformUrl y no necesita las URLs de la DGII.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EcfXmlValidation:UseInternalValidator"] = "true",
                ["AppSettings:PlatformUrl"] = "https://ecfstaging.zynstorm.com/api"
            })
            .Build();

        return new DgiiTransmissionService(
            new HttpClient(new ThrowingHandler(exception)),
            configuration,
            NullLogger<DgiiTransmissionService>.Instance);
    }

    [Fact]
    public async Task Timeout_StillPropagatesTheException_AsInMain()
    {
        // La certificación (y cualquier otro llamador de SendEcfAsync) depende de que un timeout
        // siga lanzando la excepción. La conversión a fallo de transporte la hace DgiiSendGuard,
        // solo para el envío diferido de emit.
        var service = ServiceThatThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.SendEcfAsync(DgiiEnvironment.Test, "token", "<x/>", 32, 10m, "132293894", "E320000000098"));
    }

    [Fact]
    public async Task ConnectionFailure_IsReportedAsATransportFailure()
    {
        var service = ServiceThatThrows(new HttpRequestException("no route"));

        var result = await service.SendEcfAsync(DgiiEnvironment.Test, "token", "<x/>", 32, 10m, "132293894", "E320000000098");

        Assert.True(result.TransportFailure);
        Assert.False(result.Success);
    }
}
