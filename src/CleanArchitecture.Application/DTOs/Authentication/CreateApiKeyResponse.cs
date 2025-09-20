namespace CleanArchitecture.Application.DTOs.Authentication;

/// <summary>
/// Response model for API key creation
/// </summary>
public class CreateApiKeyResponse
{
    /// <summary>
    /// The generated API key (only shown once)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API key information
    /// </summary>
    public ApiKeyDto ApiKeyInfo { get; set; } = null!;
}