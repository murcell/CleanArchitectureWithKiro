using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.WebAPI.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Integration.Authentication;

[Collection("Test Collection")]
public class ApiKeyIntegrationTests : IntegrationTestBase<TestWebApplicationFactory>
{
    public ApiKeyIntegrationTests()
    {
    }

    [Fact]
    public async Task TestApiKey_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Clear();

        // Act
        var response = await Client.GetAsync("/api/v1.0/api-keys/test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}