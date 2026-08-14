using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Performance;

public class DgiiPipelinePerformanceTests
{
    [Fact]
    public async Task ConcurrentTokenRequestsShareOneDgiiAuthentication()
    {
        var handler = new CountingAuthHandler();
        var signer = new CountingSigner();
        var cache = new TestCache();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EcfXmlValidation:UseInternalValidator"] = "false",
                ["DgiiUrls:Test:Auth"] = "https://dgii.test/auth"
            })
            .Build();
        var service = new DgiiAuthService(
            new HttpClient(handler),
            signer,
            cache,
            configuration,
            NullLogger<DgiiAuthService>.Instance);
        var issuerRnc = Random.Shared.NextInt64(100_000_000, 999_999_999).ToString();

        var requests = Enumerable.Range(0, 20)
            .Select(_ => service.GetTokenAsync(issuerRnc, DgiiEnvironment.Test, "certificate", "password"));

        var tokens = await Task.WhenAll(requests);

        Assert.All(tokens, token => Assert.Equal("shared-token", token));
        Assert.Equal(1, handler.SeedRequests);
        Assert.Equal(1, handler.TokenRequests);
        Assert.Equal(1, signer.SignRequests);
    }

    [Fact]
    public async Task StatusRequestHonorsTheEmissionCancellationToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EcfXmlValidation:UseInternalValidator"] = "false",
                ["DgiiUrls:Test:Consulta"] = "https://dgii.test/status"
            })
            .Build();
        var service = new DgiiTransmissionService(
            new HttpClient(new BlockingStatusHandler()),
            configuration,
            NullLogger<DgiiTransmissionService>.Instance);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetStatusAsync(
                DgiiEnvironment.Test,
                "token",
                "track-id",
                cancellation.Token));
    }

    [Fact]
    public async Task ConcurrentSchemaRequestsReuseTheCompiledSchema()
    {
        var loadSchema = typeof(EcfProductionGeneratorService).GetMethod(
            "LoadSchemaSetForType",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(loadSchema);

        var requests = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => loadSchema.Invoke(null, [31, false])));
        var schemas = await Task.WhenAll(requests);

        Assert.NotNull(schemas[0]);
        Assert.All(schemas, schema => Assert.Same(schemas[0], schema));
    }

    private sealed class CountingAuthHandler : HttpMessageHandler
    {
        private int _seedRequests;
        private int _tokenRequests;

        public int SeedRequests => _seedRequests;
        public int TokenRequests => _tokenRequests;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                Interlocked.Increment(ref _seedRequests);
                await Task.Delay(50, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<Semilla>123</Semilla>")
                };
            }

            Interlocked.Increment(ref _tokenRequests);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token\":\"shared-token\"}")
            };
        }
    }

    private sealed class BlockingStatusHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The cancellation token was not honored.");
        }
    }

    private sealed class CountingSigner : IXmlSignatureService
    {
        private int _signRequests;

        public int SignRequests => _signRequests;

        public string SignXml(string unsignedXml, string certificateBase64, string certificatePassword)
        {
            Interlocked.Increment(ref _signRequests);
            return unsignedXml;
        }

        public string GetSignatureValue(string signedXml) => string.Empty;
    }

    private sealed class TestCache : ICacheService
    {
        private readonly ConcurrentDictionary<string, object> _entries = new();

        public T? Get<T>(string key)
        {
            return _entries.TryGetValue(key, out var value) ? (T)value : default;
        }

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            _entries[key] = value!;
        }

        public void Remove(string key)
        {
            _entries.TryRemove(key, out _);
        }
    }
}
