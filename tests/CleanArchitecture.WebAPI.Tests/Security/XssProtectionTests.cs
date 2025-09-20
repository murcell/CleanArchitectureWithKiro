using CleanArchitecture.WebAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchitecture.WebAPI.Tests.Security;

/// <summary>
/// Tests for XSS protection middleware
/// </summary>
public class XssProtectionTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<XssProtectionMiddleware>> _mockLogger;

    public XssProtectionTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<XssProtectionMiddleware>>();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<iframe src='javascript:alert(1)'></iframe>")]
    [InlineData("<img onerror='alert(1)' src='x'>")]
    [InlineData("<svg onload='alert(1)'>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("<object data='data:text/html,<script>alert(1)</script>'></object>")]
    public async Task XssProtectionMiddleware_ShouldBlockMaliciousQueryParameters(string maliciousInput)
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.QueryString = new QueryString($"?search={Uri.EscapeDataString(maliciousInput)}");
        
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        _output.WriteLine($"Successfully blocked XSS attempt: {maliciousInput}");
    }

    [Theory]
    [InlineData("{\"name\":\"<script>alert('xss')</script>\"}")]
    [InlineData("{\"description\":\"javascript:alert('xss')\"}")]
    [InlineData("{\"content\":\"<img onerror='alert(1)' src='x'>\"}")]
    public async Task XssProtectionMiddleware_ShouldBlockMaliciousRequestBody(string maliciousJson)
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        
        var bodyBytes = Encoding.UTF8.GetBytes(maliciousJson);
        context.Request.Body = new MemoryStream(bodyBytes);
        
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        _output.WriteLine($"Successfully blocked XSS in request body: {maliciousJson}");
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("user@example.com")]
    [InlineData("This is a normal description with numbers 123")]
    [InlineData("Special chars: !@#$%^&*()")]
    public async Task XssProtectionMiddleware_ShouldAllowSafeContent(string safeInput)
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.QueryString = new QueryString($"?search={Uri.EscapeDataString(safeInput)}");
        
        var nextCalled = false;
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => 
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.NotEqual(400, context.Response.StatusCode);
        _output.WriteLine($"Successfully allowed safe content: {safeInput}");
    }

    [Fact]
    public async Task XssProtectionMiddleware_ShouldAddSecurityHeaders()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-XSS-Protection"));
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));
        Assert.True(context.Response.Headers.ContainsKey("Permissions-Policy"));
        
        _output.WriteLine("Security headers added successfully");
    }

    [Theory]
    [InlineData("X-Forwarded-Host")]
    [InlineData("X-Original-URL")]
    [InlineData("X-Rewrite-URL")]
    public async Task XssProtectionMiddleware_ShouldBlockDangerousHeaders(string dangerousHeader)
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers[dangerousHeader] = "malicious-value";
        
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        _output.WriteLine($"Successfully blocked dangerous header: {dangerousHeader}");
    }

    [Fact]
    public async Task XssProtectionMiddleware_ShouldHandleEmptyRequestBody()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream();
        
        var nextCalled = false;
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => 
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _output.WriteLine("Empty request body handled successfully");
    }

    [Fact]
    public async Task XssProtectionMiddleware_ShouldHandleGetRequests()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Method = "GET";
        
        var nextCalled = false;
        var middleware = new XssProtectionMiddleware(
            next: (innerHttpContext) => 
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            logger: _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _output.WriteLine("GET request handled successfully");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}