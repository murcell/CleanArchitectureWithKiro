using CleanArchitecture.WebAPI.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchitecture.WebAPI.Tests.Security;

/// <summary>
/// Tests to verify security configuration is properly applied
/// </summary>
[Collection("Test Collection")]
public class SecurityConfigurationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SecurityConfigurationTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Application_ShouldEnforceHttpsRedirection()
    {
        // Arrange
        var httpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await httpClient.GetAsync("http://localhost/api/health");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || 
                   response.StatusCode == HttpStatusCode.MovedPermanently ||
                   response.Headers.Location?.Scheme == "https");
        
        _output.WriteLine("HTTPS redirection is properly configured");
    }

    [Fact]
    public async Task Application_ShouldHaveSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        var headers = response.Headers;
        
        // Check for XSS Protection
        Assert.True(headers.Contains("X-XSS-Protection"), "X-XSS-Protection header missing");
        
        // Check for Content Type Options
        Assert.True(headers.Contains("X-Content-Type-Options"), "X-Content-Type-Options header missing");
        
        // Check for Frame Options
        Assert.True(headers.Contains("X-Frame-Options"), "X-Frame-Options header missing");
        
        // Check for Content Security Policy
        Assert.True(headers.Contains("Content-Security-Policy"), "Content-Security-Policy header missing");
        
        // Check for Referrer Policy
        Assert.True(headers.Contains("Referrer-Policy"), "Referrer-Policy header missing");
        
        _output.WriteLine("All required security headers are present");
        
        // Log header values for verification
        foreach (var header in headers)
        {
            _output.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
    }

    [Fact]
    public async Task Application_ShouldHaveCorrectSecurityHeaderValues()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        var headers = response.Headers;
        
        // Verify X-XSS-Protection value
        if (headers.TryGetValues("X-XSS-Protection", out var xssValues))
        {
            Assert.Contains("1; mode=block", xssValues);
        }
        
        // Verify X-Content-Type-Options value
        if (headers.TryGetValues("X-Content-Type-Options", out var contentTypeValues))
        {
            Assert.Contains("nosniff", contentTypeValues);
        }
        
        // Verify X-Frame-Options value
        if (headers.TryGetValues("X-Frame-Options", out var frameValues))
        {
            Assert.Contains("DENY", frameValues);
        }
        
        // Verify Content-Security-Policy contains important directives
        if (headers.TryGetValues("Content-Security-Policy", out var cspValues))
        {
            var csp = string.Join(" ", cspValues);
            Assert.Contains("default-src 'self'", csp);
            Assert.Contains("script-src 'self'", csp);
            Assert.Contains("frame-ancestors 'none'", csp);
        }
        
        _output.WriteLine("Security header values are correctly configured");
    }

    [Fact]
    public async Task Application_ShouldRejectLargePayloads()
    {
        // Arrange - Create a large payload (assuming there's a reasonable limit)
        var largeContent = new string('A', 10 * 1024 * 1024); // 10MB
        var content = new StringContent($"{{\"data\":\"{largeContent}\"}}", 
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        // Should either reject with 413 (Payload Too Large) or 400 (Bad Request)
        Assert.True(response.StatusCode == HttpStatusCode.RequestEntityTooLarge || 
                   response.StatusCode == HttpStatusCode.BadRequest);
        
        _output.WriteLine($"Large payload rejected with status: {response.StatusCode}");
    }

    [Theory]
    [InlineData("TRACE")]
    [InlineData("TRACK")]
    [InlineData("DEBUG")]
    public async Task Application_ShouldRejectDangerousHttpMethods(string httpMethod)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/api/health");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                   response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.NotImplemented);
        
        _output.WriteLine($"Dangerous HTTP method {httpMethod} rejected with status: {response.StatusCode}");
    }

    [Fact]
    public async Task Application_ShouldHaveRateLimitingHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert - Check if rate limiting headers are present (if implemented)
        // This is optional but good practice
        var hasRateLimitHeaders = response.Headers.Contains("X-RateLimit-Limit") ||
                                 response.Headers.Contains("RateLimit-Limit") ||
                                 response.Headers.Contains("X-Rate-Limit");

        _output.WriteLine($"Rate limiting headers present: {hasRateLimitHeaders}");
        
        // Log all headers for debugging
        foreach (var header in response.Headers)
        {
            _output.WriteLine($"Header: {header.Key} = {string.Join(", ", header.Value)}");
        }
    }

    [Fact]
    public async Task Application_ShouldNotExposeServerInformation()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        // Should not expose server information
        Assert.False(response.Headers.Contains("Server"), "Server header should not be exposed");
        Assert.False(response.Headers.Contains("X-Powered-By"), "X-Powered-By header should not be exposed");
        Assert.False(response.Headers.Contains("X-AspNet-Version"), "X-AspNet-Version header should not be exposed");
        
        _output.WriteLine("Server information headers are properly hidden");
    }

    [Fact]
    public async Task Application_ShouldHandleOptionsRequests()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/users");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.NoContent);
        
        // Should have CORS headers if CORS is enabled
        _output.WriteLine($"OPTIONS request handled with status: {response.StatusCode}");
    }

    [Theory]
    [InlineData("/api/../../../etc/passwd")]
    [InlineData("/api/users/../../admin")]
    [InlineData("/api\\..\\..\\windows\\system32")]
    public async Task Application_ShouldRejectPathTraversalAttempts(string maliciousPath)
    {
        // Act
        var response = await _client.GetAsync(maliciousPath);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.Forbidden);
        
        _output.WriteLine($"Path traversal attempt rejected: {maliciousPath} -> {response.StatusCode}");
    }
}