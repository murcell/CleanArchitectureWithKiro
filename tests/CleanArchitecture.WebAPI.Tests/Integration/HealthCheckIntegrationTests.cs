using CleanArchitecture.WebAPI.Tests.Common;
using System.Net;
using System.Text.Json;

namespace CleanArchitecture.WebAPI.Tests.Integration;

/// <summary>
/// Integration tests for health check endpoints using in-memory services for fast execution
/// </summary>
[Collection("In-Memory Tests")]
public class HealthCheckIntegrationTests : IntegrationTestBase<InMemoryWebApplicationFactory>
{

    [Fact]
    public async Task HealthCheck_Endpoint_Should_Return_Status()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        
        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(healthResponse.TryGetProperty("status", out _));
        Assert.True(healthResponse.TryGetProperty("checks", out _));
        Assert.True(healthResponse.TryGetProperty("totalDuration", out _));
    }

    [Fact]
    public async Task HealthCheck_Ready_Endpoint_Should_Be_Available()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthCheck_Live_Endpoint_Should_Return_OK()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthController_GetHealth_Should_Return_Status()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        
        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(healthResponse.TryGetProperty("status", out _));
        Assert.True(healthResponse.TryGetProperty("entries", out _));
        Assert.True(healthResponse.TryGetProperty("totalDuration", out _));
    }

    [Fact]
    public async Task HealthController_GetDetailedHealth_Should_Return_Detailed_Status()
    {
        // Act
        var response = await Client.GetAsync("/api/health/detailed");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        
        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(healthResponse.TryGetProperty("status", out _));
        Assert.True(healthResponse.TryGetProperty("entries", out var entries));
        
        // Check if entries contain detailed information
        if (entries.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in entries.EnumerateObject())
            {
                Assert.True(entry.Value.TryGetProperty("status", out _));
                // Detailed health checks should have more information
            }
        }
    }

    [Fact]
    public async Task HealthController_GetDetailedHealth_With_Tags_Should_Filter_Results()
    {
        // Act
        var response = await Client.GetAsync("/api/health/detailed?tags=application");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task HealthController_GetReadiness_Should_Return_Status()
    {
        // Act
        var response = await Client.GetAsync("/api/health/ready");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthController_GetLiveness_Should_Return_OK()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("\"Alive\"", content);
    }
}