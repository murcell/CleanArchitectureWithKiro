using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Application.DTOs.Authentication;

/// <summary>
/// Request model for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}