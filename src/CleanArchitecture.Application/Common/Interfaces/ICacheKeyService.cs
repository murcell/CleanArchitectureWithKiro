namespace CleanArchitecture.Application.Common.Interfaces;

public interface ICacheKeyService
{
    /// <summary>
    /// Generates a cache key for an entity by ID
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="id">Entity ID</param>
    /// <returns>Generated cache key</returns>
    string GenerateKey(string entityName, object id);

    /// <summary>
    /// Generates a cache key for a list of entities
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="parameters">Additional parameters for the key</param>
    /// <returns>Generated cache key</returns>
    string GenerateListKey(string entityName, params object[] parameters);

    /// <summary>
    /// Generates a cache key for user-specific data
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dataType">Type of data</param>
    /// <param name="parameters">Additional parameters</param>
    /// <returns>Generated cache key</returns>
    string GenerateUserKey(int userId, string dataType, params object[] parameters);

    /// <summary>
    /// Gets all possible keys for an entity (for invalidation)
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="id">Entity ID</param>
    /// <returns>List of cache keys to invalidate</returns>
    IEnumerable<string> GetEntityKeys(string entityName, object id);

    /// <summary>
    /// Gets pattern for invalidating all keys related to an entity
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <returns>Pattern for key invalidation</returns>
    string GetEntityPattern(string entityName);
}