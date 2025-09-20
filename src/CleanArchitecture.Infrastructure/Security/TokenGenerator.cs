using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CleanArchitecture.Infrastructure.Security;

/// <summary>
/// Implementation of token generation for JWT and other tokens
/// </summary>
public class TokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _configuration;

    public TokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Authentication");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? "CleanArchitecture";
        var audience = jwtSettings["Audience"] ?? "CleanArchitecture";
        var expiryMinutes = jwtSettings.GetValue<int>("AccessTokenExpiryMinutes", 15);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("user_id", user.Id.ToString()),
            new("email_confirmed", user.IsEmailConfirmed.ToString().ToLower()),
            new("is_active", user.IsActive.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string GenerateEmailConfirmationToken()
    {
        return GenerateSecureToken(32);
    }

    public string GeneratePasswordResetToken()
    {
        return GenerateSecureToken(32);
    }

    public (string apiKey, string keyHash, string keyPrefix) GenerateApiKey()
    {
        // Generate a random API key
        var keyBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        
        var apiKey = Convert.ToBase64String(keyBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        
        // Create prefix (first 8 characters)
        var keyPrefix = apiKey.Substring(0, 8);
        
        // Hash the full key for storage
        var keyHash = HashApiKey(apiKey);
        
        // Format: ca_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        var formattedApiKey = $"ca_{apiKey}";
        
        return (formattedApiKey, keyHash, keyPrefix);
    }

    private string GenerateSecureToken(int length = 32)
    {
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashedBytes);
    }
}