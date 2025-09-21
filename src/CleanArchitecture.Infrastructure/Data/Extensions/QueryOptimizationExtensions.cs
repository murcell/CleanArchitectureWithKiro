using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Data.Extensions;

/// <summary>
/// Extensions for optimizing database queries
/// </summary>
public static class QueryOptimizationExtensions
{
    /// <summary>
    /// Applies common query optimizations
    /// </summary>
    public static IQueryable<T> OptimizeQuery<T>(this IQueryable<T> query) where T : class
    {
        return query
            .AsNoTracking() // Disable change tracking for read-only queries
            .AsSplitQuery(); // Use split queries for complex includes
    }

    /// <summary>
    /// Applies optimizations for paginated queries
    /// </summary>
    public static IQueryable<T> OptimizeForPagination<T>(this IQueryable<T> query) where T : class
    {
        return query
            .AsNoTracking()
            .AsNoTrackingWithIdentityResolution(); // Better for queries with potential duplicates
    }

    /// <summary>
    /// Applies optimizations for single entity queries
    /// </summary>
    public static IQueryable<T> OptimizeForSingle<T>(this IQueryable<T> query) where T : class
    {
        return query
            .AsNoTracking()
            .AsSingleQuery(); // Use single query for simple includes
    }

    /// <summary>
    /// Applies optimizations for update operations
    /// </summary>
    public static IQueryable<T> OptimizeForUpdate<T>(this IQueryable<T> query) where T : class
    {
        return query
            .AsTracking() // Enable change tracking for updates
            .AsSingleQuery();
    }
}