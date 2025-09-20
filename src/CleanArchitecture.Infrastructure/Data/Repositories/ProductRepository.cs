using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Infrastructure.Data.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsAvailable)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Stock <= threshold)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, string currency, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Price.Currency == currency && 
                       p.Price.Amount >= minPrice && 
                       p.Price.Amount <= maxPrice)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetProductsWithUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(cancellationToken);

        var lowerSearchTerm = searchTerm.ToLower();
        return await _dbSet
            .Where(p => p.Name.ToLower().Contains(lowerSearchTerm) || 
                       p.Description.ToLower().Contains(lowerSearchTerm))
            .ToListAsync(cancellationToken);
    }
}