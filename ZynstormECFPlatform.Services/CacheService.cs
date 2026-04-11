using System;
using Microsoft.Extensions.Caching.Memory;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T? Get<T>(string key)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return value;
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _memoryCache.Set(key, value, expiration);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}
