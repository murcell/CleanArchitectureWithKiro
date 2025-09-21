using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Simple caching strategies for common use cases
/// </summary>
public class SimpleCacheStrategies
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<SimpleCacheStrategies> _logger;

    public SimpleCacheStrategies(ICacheService cacheService, ILogger<SimpleCacheStrategies> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Cache-aside pattern: Check cache first, then database
    /// </summary>
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? expiry = null) where T : class
    {
        // Try to get from cache first
        var cached = await _cacheService.GetAsync<T>(key);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);

        // Get from source
        var value = await factory();
        if (value != null)
        {
            // Set in cache
            await _cacheService.SetAsync(key, value, expiry ?? TimeSpan.FromMinutes(30));
            _logger.LogDebug("Value cached for key: {Key}", key);
        }

        return value;
    }
}