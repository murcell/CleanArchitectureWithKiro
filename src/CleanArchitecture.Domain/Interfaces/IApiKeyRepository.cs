using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Repository interface for API key operations
/// </summary>
public interface IApiKeyRepository : IRepository<ApiKey>
{
    /// <summary>
    /// Gets an API key by its hash
    /// </summary>
    /// <param name="keyHash">The API key hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The API key if found, null otherwise</returns>
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an API key by its prefix
    /// </summary>
    /// <param name="keyPrefix">The API key prefix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The API key if found, null otherwise</returns>
    Task<ApiKey?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all API keys for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of API keys for the user</returns>
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active API keys
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active API keys</returns>
    Task<IEnumerable<ApiKey>> GetActiveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all expired API keys
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired API keys</returns>
    Task<IEnumerable<ApiKey>> GetExpiredAsync(CancellationToken cancellationToken = default);
}