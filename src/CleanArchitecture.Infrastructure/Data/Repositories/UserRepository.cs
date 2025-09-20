using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Data;

namespace CleanArchitecture.Infrastructure.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Name == username, cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await _dbSet
            .AnyAsync(u => u.Email.Value == email, cancellationToken);
    }

    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        return await _dbSet
            .AnyAsync(u => u.Name == username, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersWithProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Products)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<User?> GetByEmailConfirmationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.EmailConfirmationToken == token, cancellationToken);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Role == role)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}