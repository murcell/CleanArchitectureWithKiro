using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Repository interface for Product entity with specific product-related operations
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Gets products by user ID
    /// </summary>
    Task<IEnumerable<Product>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available products only
    /// </summary>
    Task<IEnumerable<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with low stock (below specified threshold)
    /// </summary>
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by price range
    /// </summary>
    Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with their associated users
    /// </summary>
    Task<IEnumerable<Product>> GetProductsWithUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name or description
    /// </summary>
    Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}