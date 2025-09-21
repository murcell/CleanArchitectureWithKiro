namespace CleanArchitecture.WebAPI.Services;

/// <summary>
/// Implementation of correlation ID service
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private string _correlationId = string.Empty;

    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    public string CorrelationId => _correlationId;

    /// <summary>
    /// Sets the correlation ID for the current request
    /// </summary>
    /// <param name="correlationId">The correlation ID to set</param>
    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
    }

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    /// <returns>A new correlation ID</returns>
    public string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }
}