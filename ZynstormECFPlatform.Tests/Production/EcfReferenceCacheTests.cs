using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfReferenceCacheTests
{
    private sealed class FakeCache : ICacheService
    {
        public Dictionary<string, object> Items { get; } = [];

        public TimeSpan? LastExpiration { get; private set; }

        public T? Get<T>(string key) => Items.TryGetValue(key, out var value) ? (T)value : default;

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            Items[key] = value!;
            LastExpiration = expiration;
        }

        public void Remove(string key) => Items.Remove(key);
    }

    private sealed record Currency(string Code);

    [Fact]
    public async Task SecondCall_ComesFromTheCache()
    {
        var cache = new EcfReferenceCache(new FakeCache());
        var loads = 0;

        Task<Currency?> Load() { loads++; return Task.FromResult<Currency?>(new Currency("DOP")); }

        var first = await cache.GetOrLoadAsync("ecf-ref:currency", Load);
        var second = await cache.GetOrLoadAsync("ecf-ref:currency", Load);

        Assert.Equal(1, loads);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Absence_IsNotCached()
    {
        var cache = new EcfReferenceCache(new FakeCache());
        var loads = 0;

        Task<Currency?> Load() { loads++; return Task.FromResult<Currency?>(null); }

        Assert.Null(await cache.GetOrLoadAsync("ecf-ref:currency", Load));
        Assert.Null(await cache.GetOrLoadAsync("ecf-ref:currency", Load));

        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task DifferentKeys_AreIndependent()
    {
        var cache = new EcfReferenceCache(new FakeCache());

        var one = await cache.GetOrLoadAsync("ecf-ref:type:31", () => Task.FromResult<Currency?>(new Currency("31")));
        var two = await cache.GetOrLoadAsync("ecf-ref:type:32", () => Task.FromResult<Currency?>(new Currency("32")));

        Assert.Equal("31", one!.Code);
        Assert.Equal("32", two!.Code);
    }

    [Fact]
    public async Task Entries_ExpireAfterFiveMinutes()
    {
        var fake = new FakeCache();
        var cache = new EcfReferenceCache(fake);

        await cache.GetOrLoadAsync("ecf-ref:currency", () => Task.FromResult<Currency?>(new Currency("DOP")));

        Assert.Equal(TimeSpan.FromMinutes(5), fake.LastExpiration);
    }
}
