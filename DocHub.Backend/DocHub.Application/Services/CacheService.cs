using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DocHub.Application.Services;

public interface ICacheService
{
    T? Get<T>(string key);
    Task<T?> GetAsync<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
    void RemovePattern(string pattern);
    void Clear();
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
    Task<T> GetOrSetAsync<T>(string key, Func<T> factory, TimeSpan expiration);
    bool TryGet<T>(string key, out T? value);
    void SetWithSlidingExpiration<T>(string key, T value, TimeSpan slidingExpiration);
    void SetWithAbsoluteExpiration<T>(string key, T value, DateTime absoluteExpiration);
    void SetWithPriority<T>(string key, T value, TimeSpan expiration, CacheItemPriority priority);
    Dictionary<string, object> GetCacheStats();
    void InvalidateUserCache(string userId);
    void InvalidateModuleCache(string module);
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _keyPatterns = new();
    private readonly ConcurrentDictionary<string, DateTime> _keyTimestamps = new();
    private readonly object _statsLock = new object();
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private long _cacheSets = 0;
    private long _cacheRemovals = 0;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.LogDebug("📦 [CACHE] Cache hit for key: {Key}", key);
            return value;
        }
        
        Interlocked.Increment(ref _cacheMisses);
        _logger.LogDebug("📦 [CACHE] Cache miss for key: {Key}", key);
        return default;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return await Task.FromResult(Get<T>(key));
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            SlidingExpiration = TimeSpan.FromMinutes(5), // Default sliding expiration
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _keyTimestamps[key] = DateTime.UtcNow;
        Interlocked.Increment(ref _cacheSets);
        _logger.LogDebug("📦 [CACHE] Cached key: {Key} with expiration: {Expiration}", key, expiration);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        await Task.Run(() => Set(key, value, expiration));
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keyTimestamps.TryRemove(key, out _);
        Interlocked.Increment(ref _cacheRemovals);
        _logger.LogDebug("📦 [CACHE] Removed key: {Key}", key);
    }

    public void RemovePattern(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var keysToRemove = new List<string>();

        foreach (var kvp in _keyTimestamps)
        {
            if (regex.IsMatch(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        _logger.LogDebug("📦 [CACHE] Removed pattern: {Pattern} with {Count} keys", pattern, keysToRemove.Count);
    }

    public void Clear()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        _keyPatterns.Clear();
        _keyTimestamps.Clear();
        _logger.LogDebug("📦 [CACHE] Cache cleared");
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        var cached = Get<T>(key);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        Set(key, value, expiration);
        return value;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<T> factory, TimeSpan expiration)
    {
        var cached = Get<T>(key);
        if (cached != null)
        {
            return cached;
        }

        var value = await Task.Run(factory);
        Set(key, value, expiration);
        return value;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        value = Get<T>(key);
        return value != null;
    }

    public void SetWithSlidingExpiration<T>(string key, T value, TimeSpan slidingExpiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _keyTimestamps[key] = DateTime.UtcNow;
        Interlocked.Increment(ref _cacheSets);
        _logger.LogDebug("📦 [CACHE] Cached key: {Key} with sliding expiration: {Expiration}", key, slidingExpiration);
    }

    public void SetWithAbsoluteExpiration<T>(string key, T value, DateTime absoluteExpiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _keyTimestamps[key] = DateTime.UtcNow;
        Interlocked.Increment(ref _cacheSets);
        _logger.LogDebug("📦 [CACHE] Cached key: {Key} with absolute expiration: {Expiration}", key, absoluteExpiration);
    }

    public void SetWithPriority<T>(string key, T value, TimeSpan expiration, CacheItemPriority priority)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Priority = priority
        };

        _cache.Set(key, value, options);
        _keyTimestamps[key] = DateTime.UtcNow;
        Interlocked.Increment(ref _cacheSets);
        _logger.LogDebug("📦 [CACHE] Cached key: {Key} with priority: {Priority}", key, priority);
    }

    public Dictionary<string, object> GetCacheStats()
    {
        lock (_statsLock)
        {
            return new Dictionary<string, object>
            {
                ["TotalKeys"] = _keyTimestamps.Count,
                ["CacheHits"] = _cacheHits,
                ["CacheMisses"] = _cacheMisses,
                ["CacheSets"] = _cacheSets,
                ["CacheRemovals"] = _cacheRemovals,
                ["HitRate"] = _cacheHits + _cacheMisses > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0,
                ["MemoryUsage"] = GC.GetTotalMemory(false)
            };
        }
    }

    public void InvalidateUserCache(string userId)
    {
        RemovePattern($"user:{userId}:*");
        RemovePattern($"session:{userId}:*");
        _logger.LogDebug("📦 [CACHE] Invalidated cache for user: {UserId}", userId);
    }

    public void InvalidateModuleCache(string module)
    {
        RemovePattern($"module:{module}:*");
        RemovePattern($"dashboard:{module}:*");
        _logger.LogDebug("📦 [CACHE] Invalidated cache for module: {Module}", module);
    }

    public void TrackKey(string key, string pattern)
    {
        _keyPatterns.AddOrUpdate(pattern, 
            new HashSet<string> { key },
            (_, existing) => { existing.Add(key); return existing; });
    }
}
