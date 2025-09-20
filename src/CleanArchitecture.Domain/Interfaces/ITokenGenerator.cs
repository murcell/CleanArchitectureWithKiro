using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Interface for token generation operations
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates a JWT access token for the specified user
    /// </summary>
    /// <param name="user">The user to generate the token for</param>
    /// <returns>The JWT access token</returns>
    string GenerateAccessToken(User user);
    
    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>The refresh token</returns>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Generates an email confirmation token
    /// </summary>
    /// <returns>The email confirmation token</returns>
    string GenerateEmailConfirmationToken();
    
    /// <summary>
    /// Generates a password reset token
    /// </summary>
    /// <returns>The password reset token</returns>
    string GeneratePasswordResetToken();
    
    /// <summary>
    /// Generates an API key
    /// </summary>
    /// <returns>A tuple containing the API key and its hash</returns>
    (string apiKey, string keyHash, string keyPrefix) GenerateApiKey();
}