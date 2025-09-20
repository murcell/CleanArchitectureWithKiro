using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.WebAPI.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Integration.Authentication;

[Collection("Test Collection")]
public class AuthenticationIntegrationTests : IntegrationTestBase<TestWebApplicationFactory>
{
    public AuthenticationIntegrationTests()
    {
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccessAndTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1.0/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.NotEmpty(loginResponse.RefreshToken);
        Assert.Equal("Bearer", loginResponse.TokenType);
        Assert.True(loginResponse.ExpiresIn > 0);
        Assert.NotNull(loginResponse.User);
        Assert.Equal("Test User", loginResponse.User.Name);
        Assert.Equal("test@example.com", loginResponse.User.Email);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndTokens()
    {
        // Arrange
        await RegisterTestUserAsync("login@example.com", "TestPassword123!");

        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.NotEmpty(loginResponse.RefreshToken);
        Assert.Equal("Bearer", loginResponse.TokenType);
        Assert.True(loginResponse.ExpiresIn > 0);
    }

    private async Task RegisterTestUserAsync(string email, string password)
    {
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        var response = await Client.PostAsJsonAsync("/api/v1.0/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}