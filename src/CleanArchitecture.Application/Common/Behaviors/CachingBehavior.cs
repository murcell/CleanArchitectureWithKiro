using CleanArchitecture.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchitecture.Application.Common.Behaviors;

/// <summary>
/// Caching behavior for MediatR requests
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache queries (requests that implement ICacheableQuery)
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(request, cacheableQuery.CacheKey);
        
        // Try to get from cache
        var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

        // Execute the request
        var response = await next();

        // Cache the response if it's not null
        if (response != null)
        {
            var expiration = cacheableQuery.CacheExpiration ?? TimeSpan.FromMinutes(30);
            await _cacheService.SetAsync(cacheKey, response, expiration);
            
            _logger.LogDebug("Response cached for key: {CacheKey}, Expiration: {Expiration}", 
                cacheKey, expiration);
        }

        return response;
    }

    private static string GenerateCacheKey(TRequest request, string? customKey = null)
    {
        if (!string.IsNullOrEmpty(customKey))
        {
            return customKey;
        }

        // Generate cache key based on request type and properties
        var requestType = typeof(TRequest).Name;
        var requestJson = JsonSerializer.Serialize(request);
        var requestHash = requestJson.GetHashCode();

        return $"{requestType}_{requestHash}";
    }
}

/// <summary>
/// Interface for cacheable queries
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Custom cache key (optional)
    /// </summary>
    string? CacheKey { get; }
    
    /// <summary>
    /// Cache expiration time (optional, defaults to 30 minutes)
    /// </summary>
    TimeSpan? CacheExpiration { get; }
}

/// <summary>
/// Interface for cache invalidation
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Cache keys or patterns to invalidate
    /// </summary>
    IEnumerable<string> CacheKeysToInvalidate { get; }
}