using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Caching;

public class CacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cacheService,
        ICacheKeyService cacheKeyService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _cacheKeyService = cacheKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Invalidates all cache entries related to a specific entity
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">ID of the entity</param>
    /// <returns>Task</returns>
    public async Task InvalidateEntityAsync(string entityName, object entityId)
    {
        try
        {
            _logger.LogDebug("Invalidating cache for entity: {EntityName} with ID: {EntityId}", entityName, entityId);

            // Get all related keys for this entity
            var keysToInvalidate = _cacheKeyService.GetEntityKeys(entityName, entityId);

            // Remove specific keys
            var tasks = keysToInvalidate.Select(key => _cacheService.RemoveAsync(key));
            await Task.WhenAll(tasks);

            // Remove by pattern for any dynamic keys
            var pattern = _cacheKeyService.GetEntityPattern(entityName);
            await _cacheService.RemoveByPatternAsync(pattern);

            _logger.LogDebug("Successfully invalidated cache for entity: {EntityName} with ID: {EntityId}", entityName, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for entity: {EntityName} with ID: {EntityId}", entityName, entityId);
        }
    }

    /// <summary>
    /// Invalidates all cache entries for a specific entity type
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <returns>Task</returns>
    public async Task InvalidateEntityTypeAsync(string entityName)
    {
        try
        {
            _logger.LogDebug("Invalidating all cache entries for entity type: {EntityName}", entityName);

            var pattern = _cacheKeyService.GetEntityPattern(entityName);
            await _cacheService.RemoveByPatternAsync(pattern);

            _logger.LogDebug("Successfully invalidated all cache entries for entity type: {EntityName}", entityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for entity type: {EntityName}", entityName);
        }
    }

    /// <summary>
    /// Invalidates user-specific cache entries
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dataType">Optional specific data type to invalidate</param>
    /// <returns>Task</returns>
    public async Task InvalidateUserCacheAsync(int userId, string? dataType = null)
    {
        try
        {
            _logger.LogDebug("Invalidating user cache for user: {UserId}, dataType: {DataType}", userId, dataType ?? "all");

            string pattern;
            if (!string.IsNullOrEmpty(dataType))
            {
                // Invalidate specific user data type
                var key = _cacheKeyService.GenerateUserKey(userId, dataType);
                await _cacheService.RemoveAsync(key);
                
                // Also remove any related patterns
                pattern = $"{key}*";
                await _cacheService.RemoveByPatternAsync(pattern);
            }
            else
            {
                // Invalidate all user cache
                pattern = _cacheKeyService.GenerateUserKey(userId, "*");
                await _cacheService.RemoveByPatternAsync(pattern);
            }

            _logger.LogDebug("Successfully invalidated user cache for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Invalidates multiple entities at once
    /// </summary>
    /// <param name="entityInvalidations">Dictionary of entity names and their IDs to invalidate</param>
    /// <returns>Task</returns>
    public async Task InvalidateMultipleEntitiesAsync(Dictionary<string, List<object>> entityInvalidations)
    {
        try
        {
            _logger.LogDebug("Invalidating multiple entities: {EntityCount}", entityInvalidations.Count);

            var tasks = new List<Task>();

            foreach (var (entityName, entityIds) in entityInvalidations)
            {
                foreach (var entityId in entityIds)
                {
                    tasks.Add(InvalidateEntityAsync(entityName, entityId));
                }
            }

            await Task.WhenAll(tasks);

            _logger.LogDebug("Successfully invalidated multiple entities");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating multiple entities");
        }
    }
}