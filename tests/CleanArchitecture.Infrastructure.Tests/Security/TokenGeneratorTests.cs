using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Security;

public class TokenGeneratorTests
{
    private readonly TokenGenerator _tokenGenerator;
    private readonly IConfiguration _configuration;

    public TokenGeneratorTests()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:SecretKey"] = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
            ["Authentication:Issuer"] = "TestIssuer",
            ["Authentication:Audience"] = "TestAudience",
            ["Authentication:AccessTokenExpiryMinutes"] = "15"
        });
        _configuration = configurationBuilder.Build();
        _tokenGenerator = new TokenGenerator(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ReturnsValidJwtToken()
    {
        // Arrange
        var user = User.Create("Test User", "test@example.com");
        user.SetRole(UserRole.User);

        // Act
        var token = _tokenGenerator.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token structure
        var tokenHandler = new JwtSecurityTokenHandler();
        Assert.True(tokenHandler.CanReadToken(token));

        var jwtToken = tokenHandler.ReadJwtToken(token);
        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);
        
        // Verify claims
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Test User");
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _tokenGenerator.GenerateRefreshToken();
        var token2 = _tokenGenerator.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateEmailConfirmationToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _tokenGenerator.GenerateEmailConfirmationToken();
        var token2 = _tokenGenerator.GenerateEmailConfirmationToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GeneratePasswordResetToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _tokenGenerator.GeneratePasswordResetToken();
        var token2 = _tokenGenerator.GeneratePasswordResetToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateApiKey_ReturnsValidApiKeyComponents()
    {
        // Act
        var (apiKey, keyHash, keyPrefix) = _tokenGenerator.GenerateApiKey();

        // Assert
        Assert.NotNull(apiKey);
        Assert.NotNull(keyHash);
        Assert.NotNull(keyPrefix);
        Assert.NotEmpty(apiKey);
        Assert.NotEmpty(keyHash);
        Assert.NotEmpty(keyPrefix);

        // Verify API key format
        Assert.StartsWith("ca_", apiKey);
        Assert.Equal(8, keyPrefix.Length);
        
        // Verify key prefix matches the beginning of the actual key (after ca_ prefix)
        var actualKey = apiKey.Substring(3);
        Assert.StartsWith(keyPrefix, actualKey);
    }

    [Fact]
    public void GenerateApiKey_ReturnsUniqueKeys()
    {
        // Act
        var (apiKey1, keyHash1, keyPrefix1) = _tokenGenerator.GenerateApiKey();
        var (apiKey2, keyHash2, keyPrefix2) = _tokenGenerator.GenerateApiKey();

        // Assert
        Assert.NotEqual(apiKey1, apiKey2);
        Assert.NotEqual(keyHash1, keyHash2);
        Assert.NotEqual(keyPrefix1, keyPrefix2);
    }

    [Fact]
    public void GenerateAccessToken_WithMissingSecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = "TestIssuer",
            ["Authentication:Audience"] = "TestAudience"
            // Missing SecretKey
        });
        var configuration = configurationBuilder.Build();
        var tokenGenerator = new TokenGenerator(configuration);
        var user = User.Create("Test User", "test@example.com");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenGenerator.GenerateAccessToken(user));
    }
}