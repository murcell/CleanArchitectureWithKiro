using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.WebAPI.Middleware;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionMiddleware
/// </summary>
public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_Should_Call_Next_When_No_Exception_Occurs()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_ValidationException()
    {
        // Arrange
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Email is invalid")
        };
        var validationException = new ValidationException(validationFailures);

        RequestDelegate next = (HttpContext hc) => throw validationException;
        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(400, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Validation failed", response.Message);
        Assert.NotNull(response.Errors);
        Assert.Contains("Name is required", response.Errors);
        Assert.Contains("Email is invalid", response.Errors);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_ArgumentException()
    {
        // Arrange
        var argumentException = new ArgumentException("Invalid argument provided");

        RequestDelegate next = (HttpContext hc) => throw argumentException;
        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(400, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Invalid argument provided", response.Message);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_UnauthorizedAccessException()
    {
        // Arrange
        var unauthorizedException = new UnauthorizedAccessException();

        RequestDelegate next = (HttpContext hc) => throw unauthorizedException;
        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Unauthorized access", response.Message);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Generic_Exception()
    {
        // Arrange
        var genericException = new InvalidOperationException("Something went wrong");

        RequestDelegate next = (HttpContext hc) => throw genericException;
        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(500, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("An internal server error occurred", response.Message);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Exception()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}