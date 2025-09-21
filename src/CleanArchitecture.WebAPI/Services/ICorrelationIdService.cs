namespace CleanArchitecture.WebAPI.Services;

/// <summary>
/// Service for managing correlation IDs across requests
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current request
    /// </summary>
    /// <param name="correlationId">The correlation ID to set</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    /// <returns>A new correlation ID</returns>
    string GenerateCorrelationId();
}