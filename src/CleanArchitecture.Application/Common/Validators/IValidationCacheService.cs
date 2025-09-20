using FluentValidation.Results;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Service interface for caching validation results to improve performance
/// </summary>
public interface IValidationCacheService
{
    /// <summary>
    /// Gets cached validation result if available
    /// </summary>
    Task<ValidationResult?> GetCachedResultAsync<T>(T request, string validatorName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches validation result for future use
    /// </summary>
    Task SetCachedResultAsync<T>(T request, string validatorName, ValidationResult result, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached validation results for a specific type
    /// </summary>
    Task InvalidateCacheAsync<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached validation results
    /// </summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of validation cache service
/// In production, this would typically use Redis or another distributed cache
/// </summary>
public class InMemoryValidationCacheService : IValidationCacheService
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new();

    private class CacheEntry
    {
        public ValidationResult Result { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public Task<ValidationResult?> GetCachedResultAsync<T>(T request, string validatorName, CancellationToken cancellationToken = default)
    {
        var key = GenerateCacheKey<T>(request, validatorName);
        
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.ExpiresAt)
                {
                    return Task.FromResult<ValidationResult?>(entry.Result);
                }
                
                // Remove expired entry
                _cache.Remove(key);
            }
        }

        return Task.FromResult<ValidationResult?>(null);
    }

    public Task SetCachedResultAsync<T>(T request, string validatorName, ValidationResult result, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var key = GenerateCacheKey<T>(request, validatorName);
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(5));

        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                Result = result,
                ExpiresAt = expiresAt
            };
        }

        return Task.CompletedTask;
    }

    public Task InvalidateCacheAsync<T>(CancellationToken cancellationToken = default)
    {
        var typePrefix = typeof(T).Name;
        
        lock (_lock)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(typePrefix)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cache.Clear();
        }

        return Task.CompletedTask;
    }

    private static string GenerateCacheKey<T>(T request, string validatorName)
    {
        // In a real implementation, you might want to use a more sophisticated
        // hashing mechanism to generate cache keys
        var requestHash = request?.GetHashCode() ?? 0;
        return $"{typeof(T).Name}_{validatorName}_{requestHash}";
    }
}