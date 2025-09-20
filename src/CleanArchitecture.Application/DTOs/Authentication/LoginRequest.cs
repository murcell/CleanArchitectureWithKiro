using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Application.DTOs.Authentication;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to remember the user (longer token expiry)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}