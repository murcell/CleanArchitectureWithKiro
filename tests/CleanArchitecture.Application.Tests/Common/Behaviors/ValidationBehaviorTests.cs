using CleanArchitecture.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    private readonly Mock<ILogger<ValidationBehavior<TestRequest, string>>> _mockLogger;

    public ValidationBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<ValidationBehavior<TestRequest, string>>>();
    }

    [Fact]
    public async Task Handle_Should_Call_Next_When_No_Validators()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators, _mockLogger.Object);
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
    }

    [Fact]
    public async Task Handle_Should_Call_Next_When_Validation_Passes()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, string>(validators, _mockLogger.Object);
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
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_Validation_Fails()
    {
        // Arrange
        var validationFailure = new ValidationFailure("Name", "Name is required");
        var validationResult = new ValidationResult(new[] { validationFailure });

        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.Contains("Name", exception.Errors.Keys);
        Assert.Contains("Name is required", exception.Errors["Name"]);
    }

    [Fact]
    public async Task Handle_Should_Aggregate_Multiple_Validation_Failures()
    {
        // Arrange
        var validationFailures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Email", "Email is invalid")
        };
        var validationResult = new ValidationResult(validationFailures);

        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, string>(validators, _mockLogger.Object);
        var request = new TestRequest("");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains("Name", exception.Errors.Keys);
        Assert.Contains("Email", exception.Errors.Keys);
    }
}