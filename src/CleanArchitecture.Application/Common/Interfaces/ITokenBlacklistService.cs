namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Service for managing blacklisted tokens
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist
    /// </summary>
    /// <param name="token">Token to blacklist</param>
    /// <param name="expiry">Token expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BlacklistTokenAsync(string token, DateTime expiry, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a token is blacklisted
    /// </summary>
    /// <param name="token">Token to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token is blacklisted</returns>
    Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes expired tokens from blacklist
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}