using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Logging;
using Xunit;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CleanArchitecture.WebAPI.Tests.Logging;

public class StructuredLoggingTests
{
    [Fact]
    public void ApplicationLogger_Should_Be_Creatable()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StructuredLoggingTests>>();

        // Act
        var applicationLogger = new ApplicationLogger<StructuredLoggingTests>(mockLogger.Object);

        // Assert
        Assert.NotNull(applicationLogger);
        Assert.IsAssignableFrom<IApplicationLogger<StructuredLoggingTests>>(applicationLogger);
    }

    [Fact]
    public void ApplicationLogger_Should_Log_Information()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StructuredLoggingTests>>();
        var applicationLogger = new ApplicationLogger<StructuredLoggingTests>(mockLogger.Object);

        // Act & Assert (should not throw)
        applicationLogger.LogInformation("Test information message");
        applicationLogger.LogWarning("Test warning message");
        applicationLogger.LogError("Test error message");

        // Verify that the underlying logger was called
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Test information message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ApplicationLogger_Should_Log_Performance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StructuredLoggingTests>>();
        var applicationLogger = new ApplicationLogger<StructuredLoggingTests>(mockLogger.Object);

        // Act & Assert (should not throw)
        applicationLogger.LogPerformance("TestOperation", 150);
        applicationLogger.LogSlowOperation("SlowOperation", 1000, 500);

        // Verify performance logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TestOperation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify slow operation logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SlowOperation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ApplicationLogger_Should_Log_Business_Events()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StructuredLoggingTests>>();
        var applicationLogger = new ApplicationLogger<StructuredLoggingTests>(mockLogger.Object);
        var eventData = new { UserId = 123, Action = "UserCreated" };

        // Act & Assert (should not throw)
        applicationLogger.LogBusinessEvent("UserCreated", eventData);
        applicationLogger.LogUserAction("123", "Login");

        // Verify business event logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserCreated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify user action logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ApplicationLogger_Should_Log_Security_Events()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StructuredLoggingTests>>();
        var applicationLogger = new ApplicationLogger<StructuredLoggingTests>(mockLogger.Object);

        // Act & Assert (should not throw)
        applicationLogger.LogSecurityEvent("UnauthorizedAccess", "Attempted access to restricted resource", "123");
        applicationLogger.LogAuthenticationEvent("Login", "123", true, "Successful login");
        applicationLogger.LogAuthenticationEvent("Login", "456", false, "Invalid credentials");

        // Verify security event logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Security event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify authentication event logging (success)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Authentication event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify authentication event logging (failure)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Authentication event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ApplicationLogger_Should_Handle_Null_Data()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<StructuredLoggingTests>>();
        var applicationLogger = new ApplicationLogger<StructuredLoggingTests>(mockLogger.Object);

        // Act & Assert (should not throw)
        applicationLogger.LogUserAction("123", "Login", null);
        applicationLogger.LogSecurityEvent("UnauthorizedAccess", "Details", null);

        // Verify logging was called
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2));
    }
}