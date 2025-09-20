using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for API key operations
/// </summary>
public class ApiKeyRepository : Repository<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);
    }

    public async Task<ApiKey?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.KeyPrefix == keyPrefix, cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .Include(x => x.User)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .Include(x => x.User)
            .Where(x => x.IsActive && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > DateTime.UtcNow))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApiKey>()
            .Include(x => x.User)
            .Where(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value <= DateTime.UtcNow)
            .OrderByDescending(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
    }
}