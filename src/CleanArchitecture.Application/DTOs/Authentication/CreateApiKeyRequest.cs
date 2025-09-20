using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Application.DTOs.Authentication;

/// <summary>
/// Request model for creating an API key
/// </summary>
public class CreateApiKeyRequest
{
    /// <summary>
    /// API key name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// API key description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    /// <summary>
    /// API key scopes/permissions
    /// </summary>
    [Required(ErrorMessage = "At least one scope is required")]
    [MinLength(1, ErrorMessage = "At least one scope is required")]
    public string[] Scopes { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// API key expiry date (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Associated user ID (optional, for user-specific API keys)
    /// </summary>
    public int? UserId { get; set; }
}