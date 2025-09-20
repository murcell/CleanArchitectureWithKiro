namespace CleanArchitecture.Application.DTOs.Authentication;

/// <summary>
/// Response model for successful login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Token expiry time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// User information
    /// </summary>
    public UserDto User { get; set; } = null!;
}