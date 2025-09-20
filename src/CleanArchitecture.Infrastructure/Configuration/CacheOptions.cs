namespace CleanArchitecture.Infrastructure.Configuration;

/// <summary>
/// Configuration options for caching services
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Default cache expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Cache key prefix
    /// </summary>
    public string KeyPrefix { get; set; } = "CleanArch";

    /// <summary>
    /// Enable cache compression
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Cache instance name
    /// </summary>
    public string InstanceName { get; set; } = "CleanArchitecture";

    /// <summary>
    /// Enable distributed caching (Redis)
    /// </summary>
    public bool EnableDistributedCache { get; set; } = true;

    /// <summary>
    /// Sliding expiration time for frequently accessed items
    /// </summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum cache size in MB (for in-memory cache)
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 100;

    /// <summary>
    /// Enable cache statistics
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Cache key separator
    /// </summary>
    public string KeySeparator { get; set; } = ":";
}