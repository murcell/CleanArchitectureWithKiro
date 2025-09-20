using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Integration;

/// <summary>
/// Integration tests for API documentation and versioning
/// </summary>
public class ApiDocumentationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiDocumentationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_V1_Should_Be_Available()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        var swaggerDoc = JsonDocument.Parse(content);
        
        // Verify basic OpenAPI structure
        Assert.True(swaggerDoc.RootElement.TryGetProperty("openapi", out _));
        Assert.True(swaggerDoc.RootElement.TryGetProperty("info", out var info));
        Assert.True(swaggerDoc.RootElement.TryGetProperty("paths", out _));
        
        // Verify API info
        Assert.True(info.TryGetProperty("title", out var title));
        Assert.Equal("Clean Architecture API", title.GetString());
        Assert.True(info.TryGetProperty("version", out var version));
        Assert.Equal("1.0", version.GetString());
    }

    [Fact]
    public async Task SwaggerJson_V2_Should_Be_Available()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v2/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var swaggerDoc = JsonDocument.Parse(content);
        
        // Verify API info for V2
        Assert.True(swaggerDoc.RootElement.TryGetProperty("info", out var info));
        Assert.True(info.TryGetProperty("version", out var version));
        Assert.Equal("2.0", version.GetString());
    }

    [Fact]
    public async Task SwaggerJson_Should_Include_Security_Definitions()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var swaggerDoc = JsonDocument.Parse(content);
        
        // Verify security definitions
        Assert.True(swaggerDoc.RootElement.TryGetProperty("components", out var components));
        Assert.True(components.TryGetProperty("securitySchemes", out var securitySchemes));
        Assert.True(securitySchemes.TryGetProperty("Bearer", out var bearerScheme));
        
        Assert.True(bearerScheme.TryGetProperty("type", out var type));
        Assert.Equal("apiKey", type.GetString());
    }

    [Fact]
    public async Task API_Should_Support_URL_Segment_Versioning()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Test V1 and V2 endpoints
        var responseV1 = await client.GetAsync("/api/v1/users/1");
        var responseV2 = await client.GetAsync("/api/v2/users/1");

        // Assert - Both should be accessible (404 is acceptable for non-existent user)
        Assert.True(responseV1.StatusCode == HttpStatusCode.OK || responseV1.StatusCode == HttpStatusCode.NotFound);
        Assert.True(responseV2.StatusCode == HttpStatusCode.OK || responseV2.StatusCode == HttpStatusCode.NotFound);
        
        // Verify API version headers are present
        Assert.True(responseV1.Headers.Contains("api-supported-versions"));
        Assert.True(responseV2.Headers.Contains("api-supported-versions"));
    }

    [Fact]
    public async Task API_Should_Support_Query_String_Versioning()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/users/1?version=2.0");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        Assert.True(response.Headers.Contains("api-supported-versions"));
    }

    [Fact]
    public async Task API_Should_Support_Header_Versioning()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Version", "2.0");

        // Act
        var response = await client.GetAsync("/api/users/1");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        Assert.True(response.Headers.Contains("api-supported-versions"));
    }

    [Fact]
    public async Task API_Should_Use_Default_Version_When_Not_Specified()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/users/1");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        Assert.True(response.Headers.Contains("api-supported-versions"));
        
        // Should default to version 1.0
        var apiVersionHeader = response.Headers.GetValues("api-supported-versions").FirstOrDefault();
        Assert.NotNull(apiVersionHeader);
        Assert.Contains("1.0", apiVersionHeader);
    }

    [Fact]
    public async Task API_Should_Report_Supported_Versions_In_Headers()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/users/1");

        // Assert
        Assert.True(response.Headers.Contains("api-supported-versions"));
        
        var supportedVersions = response.Headers.GetValues("api-supported-versions").FirstOrDefault();
        Assert.NotNull(supportedVersions);
        
        // Should contain both 1.0 and 2.0
        Assert.Contains("1.0", supportedVersions);
        Assert.Contains("2.0", supportedVersions);
    }

    [Fact]
    public async Task SwaggerJson_Should_Include_API_Endpoints()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var swaggerDoc = JsonDocument.Parse(content);
        
        // Verify paths exist
        Assert.True(swaggerDoc.RootElement.TryGetProperty("paths", out var paths));
        
        // Check for Users endpoints
        var pathsEnumerator = paths.EnumerateObject();
        var pathNames = pathsEnumerator.Select(p => p.Name).ToList();
        
        Assert.Contains(pathNames, path => path.Contains("/api/v{version}/users"));
    }

    [Fact]
    public async Task SwaggerJson_V2_Should_Include_Enhanced_Endpoints()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v2/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var swaggerDoc = JsonDocument.Parse(content);
        
        // Verify paths exist
        Assert.True(swaggerDoc.RootElement.TryGetProperty("paths", out var paths));
        
        // Check for V2 Users endpoints
        var pathsEnumerator = paths.EnumerateObject();
        var pathNames = pathsEnumerator.Select(p => p.Name).ToList();
        
        Assert.Contains(pathNames, path => path.Contains("/api/v{version}/users"));
    }
}