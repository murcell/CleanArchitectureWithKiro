namespace CleanArchitecture.Application.DTOs.Authentication;

/// <summary>
/// DTO for API key information
/// </summary>
public class ApiKeyDto
{
    /// <summary>
    /// API key ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// API key name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// API key prefix (for identification)
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// API key expiry date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Last time the API key was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// API key scopes/permissions
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// API key description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Associated user ID
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}