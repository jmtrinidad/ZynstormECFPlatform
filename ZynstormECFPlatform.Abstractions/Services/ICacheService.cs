using System;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>
/// Abstraction for caching operations to allow swapping providers (Memory, Redis, etc.) in the future without friction.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache by its key.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a value in the cache with a specified expiration time.
    /// </summary>
    void Set<T>(string key, T value, TimeSpan expiration);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    void Remove(string key);
}
