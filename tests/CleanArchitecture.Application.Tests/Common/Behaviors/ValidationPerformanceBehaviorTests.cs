using CleanArchitecture.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Tests.Common.Behaviors;

public class ValidationPerformanceBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    private readonly Mock<ILogger<ValidationPerformanceBehavior<TestRequest, string>>> _mockLogger;

    public ValidationPerformanceBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<ValidationPerformanceBehavior<TestRequest, string>>>();
    }

    [Fact]
    public async Task Handle_Should_Log_Performance_Metrics()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationPerformanceBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("Test");
        var nextCalled = false;

        RequestHandlerDelegate<string> next = (ct) => 
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Success", result);

        // Verify performance logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting validation for request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation passed for request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Individual_Validator_Performance()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(10); // Simulate some processing time
                return new ValidationResult();
            });

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationPerformanceBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validator") && v.ToString()!.Contains("completed in")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Warning_For_Slow_Validation()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1100); // Simulate slow validation (over 1 second)
                return new ValidationResult();
            });

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationPerformanceBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("exceeds the recommended threshold")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Validator_Errors()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Validator error"));

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationPerformanceBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validator") && v.ToString()!.Contains("failed after")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_With_Performance_Logging()
    {
        // Arrange
        var validationFailure = new ValidationFailure("Name", "Name is required");
        var validationResult = new ValidationResult(new[] { validationFailure });

        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationPerformanceBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.Contains("Name", exception.Errors.Keys);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed for request") && v.ToString()!.Contains("in") && v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}