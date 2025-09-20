using CleanArchitecture.WebAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Middleware;

/// <summary>
/// Unit tests for LoggingMiddleware
/// </summary>
public class LoggingMiddlewareTests
{
    private readonly Mock<ILogger<LoggingMiddleware>> _mockLogger;
    private readonly DefaultHttpContext _context;

    public LoggingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<LoggingMiddleware>>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Request_And_Response()
    {
        // Arrange
        _context.Request.Method = "GET";
        _context.Request.Path = "/api/users";
        _context.Request.QueryString = new QueryString("?page=1");
        _context.Response.StatusCode = 200;

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);

        // Verify request logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify response logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Request_Body_For_POST_Request()
    {
        // Arrange
        var requestBody = "{\"name\":\"John\",\"email\":\"john@example.com\"}";
        var bodyBytes = Encoding.UTF8.GetBytes(requestBody);

        _context.Request.Method = "POST";
        _context.Request.Path = "/api/users";
        _context.Request.ContentLength = bodyBytes.Length;
        _context.Request.Body = new MemoryStream(bodyBytes);

        RequestDelegate next = (HttpContext hc) => Task.CompletedTask;
        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(requestBody)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Hide_Sensitive_Data_In_Request_Body()
    {
        // Arrange
        var requestBody = "{\"password\":\"secret123\"}";
        var bodyBytes = Encoding.UTF8.GetBytes(requestBody);

        _context.Request.Method = "POST";
        _context.Request.Path = "/api/auth/login";
        _context.Request.ContentLength = bodyBytes.Length;
        _context.Request.Body = new MemoryStream(bodyBytes);

        RequestDelegate next = (HttpContext hc) => Task.CompletedTask;
        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SENSITIVE DATA HIDDEN]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify sensitive data is not logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("secret123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Error_Response_With_Warning_Level()
    {
        // Arrange
        _context.Request.Method = "GET";
        _context.Request.Path = "/api/users/999";
        _context.Response.StatusCode = 404;

        RequestDelegate next = (HttpContext hc) =>
        {
            hc.Response.StatusCode = 404;
            return Task.CompletedTask;
        };

        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Slow_Request_Warning()
    {
        // Arrange
        _context.Request.Method = "GET";
        _context.Request.Path = "/api/slow-endpoint";

        RequestDelegate next = async (HttpContext hc) =>
        {
            // Simulate slow request
            await Task.Delay(1100); // > 1 second
        };

        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Slow request detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Exception_And_Rethrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _context.Request.Method = "GET";
        _context.Request.Path = "/api/error";

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(_context));

        Assert.Same(exception, thrownException);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Request failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Use_Correlation_Id_From_Context()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        _context.Items["CorrelationId"] = correlationId;
        _context.Request.Method = "GET";
        _context.Request.Path = "/api/test";

        RequestDelegate next = (HttpContext hc) => Task.CompletedTask;
        var middleware = new LoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(correlationId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}