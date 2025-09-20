using System;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default if not found</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in cache with default expiration
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <returns>Task</returns>
    Task SetAsync<T>(string key, T value) where T : class;

    /// <summary>
    /// Sets a value in cache with specific expiration
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Cache expiration time</param>
    /// <returns>Task</returns>
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Task</returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes multiple cached values by pattern
    /// </summary>
    /// <param name="pattern">Key pattern (supports wildcards)</param>
    /// <returns>Task</returns>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets or sets a cached value
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create value if not cached</param>
    /// <param name="expiration">Cache expiration time</param>
    /// <returns>Cached or newly created value</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
}