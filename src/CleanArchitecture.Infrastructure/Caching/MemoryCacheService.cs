using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly object _lockObject = new();

    public MemoryCacheService(
        IMemoryCache memoryCache,
        IOptions<CacheOptions> options,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = _memoryCache.Get<T>(key);
            if (value != null)
            {
                _logger.LogDebug("Memory cache hit for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Memory cache miss for key: {Key}", key);
            }
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value) where T : class
    {
        return SetAsync(key, value, _options.DefaultExpiration);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        try
        {
            if (value == null)
            {
                return RemoveAsync(key);
            }

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            _memoryCache.Set(key, value, options);
            
            lock (_lockObject)
            {
                _cacheKeys.Add(key);
            }

            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            
            lock (_lockObject)
            {
                _cacheKeys.Remove(key);
            }

            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            List<string> keysToRemove;
            
            lock (_lockObject)
            {
                // Simple pattern matching - replace * with regex equivalent
                var regexPattern = pattern.Replace("*", ".*");
                var regex = new System.Text.RegularExpressions.Regex(regexPattern);
                
                keysToRemove = _cacheKeys.Where(key => regex.IsMatch(key)).ToList();
            }

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                lock (_lockObject)
                {
                    _cacheKeys.Remove(key);
                }
            }

            _logger.LogDebug("Removed {Count} cached values matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        try
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            _logger.LogDebug("Memory cache miss for key: {Key}, executing factory", key);
            var value = await factory();
            
            if (value != null)
            {
                var exp = expiration ?? _options.DefaultExpiration;
                await SetAsync(key, value, exp);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            // If cache fails, still try to get the value from factory
            return await factory();
        }
    }
}