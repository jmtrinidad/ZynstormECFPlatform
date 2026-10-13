using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTrackIdsServiceTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(respond(request));
        }
    }

    private static DgiiTransmissionService Service(HttpMessageHandler handler, Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["EcfXmlValidation:UseInternalValidator"] = "false",
                ["DgiiUrls:Test:ConsultaTrackIds"] = "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds"
            })
            .Build();

        return new DgiiTransmissionService(new HttpClient(handler), configuration, NullLogger<DgiiTransmissionService>.Instance);
    }

    [Fact]
    public async Task BuildsTheRequestAndParsesTheEntries()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"trackId":"T1","estado":"Aceptado","fechaRecepcion":"x"}]""")
        });

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.True(result.Available);
        Assert.Null(result.Error);
        Assert.Equal("T1", Assert.Single(result.Entries).TrackId);
        Assert.Equal(
            "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds/api/trackids/consulta?rncemisor=132293894&encf=E320000000098",
            handler.Last!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Last.Headers.Authorization!.Scheme);
        Assert.Equal("tok", handler.Last.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task WithoutConfiguredUrl_IsNotAvailable()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("no debe llamar a la DGII"));

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.CerteCF, "tok", "132293894", "E320000000098");

        Assert.False(result.Available);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task WithInternalValidator_IsNotAvailable()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("no debe llamar a la DGII"));
        var settings = new Dictionary<string, string?>
        {
            ["EcfXmlValidation:UseInternalValidator"] = "true",
            ["DgiiUrls:Test:ConsultaTrackIds"] = "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds"
        };

        var result = await Service(handler, settings).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.False(result.Available);
    }

    [Fact]
    public async Task HttpFailure_IsReportedAsErrorButStillAvailable()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.True(result.Available);
        Assert.NotNull(result.Error);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task NetworkFailure_IsReportedAsError()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("sin ruta"));

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.NotNull(result.Error);
        Assert.Empty(result.Entries);
    }
}
