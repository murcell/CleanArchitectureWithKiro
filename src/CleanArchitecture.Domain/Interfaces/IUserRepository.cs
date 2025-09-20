using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity with specific user-related operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username (in this case, we'll use Name as username)
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use
    /// </summary>
    Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username is already in use
    /// </summary>
    Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active users only
    /// </summary>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with their products
    /// </summary>
    Task<IEnumerable<User>> GetUsersWithProductsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by refresh token
    /// </summary>
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by email confirmation token
    /// </summary>
    Task<User?> GetByEmailConfirmationTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by password reset token
    /// </summary>
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets users by role
    /// </summary>
    Task<IEnumerable<User>> GetByRoleAsync(Domain.Enums.UserRole role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets locked out users
    /// </summary>
    Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default);
}