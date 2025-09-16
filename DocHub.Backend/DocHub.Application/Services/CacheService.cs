using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocHub.Application.Services;

public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
    void RemovePattern(string pattern);
    void Clear();
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly Dictionary<string, HashSet<string>> _keyPatterns = new();

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("📦 [CACHE] Cache hit for key: {Key}", key);
            return value;
        }
        
        _logger.LogDebug("📦 [CACHE] Cache miss for key: {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            SlidingExpiration = TimeSpan.FromMinutes(1),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _logger.LogDebug("📦 [CACHE] Cached key: {Key} with expiration: {Expiration}", key, expiration);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("📦 [CACHE] Removed key: {Key}", key);
    }

    public void RemovePattern(string pattern)
    {
        // Since MemoryCache doesn't support pattern removal, we'll track patterns manually
        if (_keyPatterns.TryGetValue(pattern, out var keys))
        {
            foreach (var key in keys.ToList())
            {
                _cache.Remove(key);
            }
            _keyPatterns.Remove(pattern);
            _logger.LogDebug("📦 [CACHE] Removed pattern: {Pattern} with {Count} keys", pattern, keys.Count);
        }
    }

    public void Clear()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        _keyPatterns.Clear();
        _logger.LogDebug("📦 [CACHE] Cache cleared");
    }

    public void TrackKey(string key, string pattern)
    {
        if (!_keyPatterns.ContainsKey(pattern))
        {
            _keyPatterns[pattern] = new HashSet<string>();
        }
        _keyPatterns[pattern].Add(key);
    }
}
