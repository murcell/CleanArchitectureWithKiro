using CleanArchitecture.Application.Common.Behaviors;
using CleanArchitecture.Application.Common.Validators;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Tests.Common.Behaviors;

public class CachedValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    private readonly Mock<IValidationCacheService> _mockCacheService;
    private readonly Mock<ILogger<CachedValidationBehavior<TestRequest, string>>> _mockLogger;

    public CachedValidationBehaviorTests()
    {
        _mockCacheService = new Mock<IValidationCacheService>();
        _mockLogger = new Mock<ILogger<CachedValidationBehavior<TestRequest, string>>>();
    }

    [Fact]
    public async Task Handle_Should_Use_Cached_Result_When_Available()
    {
        // Arrange
        var cachedResult = new ValidationResult();
        _mockCacheService.Setup(x => x.GetCachedResultAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var mockValidator = new Mock<IValidator<TestRequest>>();
        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new CachedValidationBehavior<TestRequest, string>(validators, _mockCacheService.Object, _mockLogger.Object);
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

        // Verify validator was not called (cache hit)
        mockValidator.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Never);

        // Verify cache was checked
        _mockCacheService.Verify(x => x.GetCachedResultAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Execute_Validation_When_Cache_Miss()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedResultAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationResult?)null);

        var validationResult = new ValidationResult();
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new CachedValidationBehavior<TestRequest, string>(validators, _mockCacheService.Object, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        // Verify validator was called (cache miss)
        mockValidator.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify cache was checked
        _mockCacheService.Verify(x => x.GetCachedResultAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Cache_Result_For_Cacheable_Validators()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedResultAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationResult?)null);

        var validationResult = new ValidationResult();
        
        // Use the actual AsyncTestRequestValidator instead of mocking GetType()
        var asyncValidator = new AsyncTestRequestValidator();

        var validators = new List<IValidator<TestRequest>> { asyncValidator };
        var behavior = new CachedValidationBehavior<TestRequest, string>(validators, _mockCacheService.Object, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        // Verify result was cached (AsyncTestRequestValidator should be considered cacheable)
        _mockCacheService.Verify(x => x.SetCachedResultAsync(
            request, 
            It.IsAny<string>(), 
            It.IsAny<ValidationResult>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Aggregate_Cached_And_Fresh_Results()
    {
        // Arrange
        var cachedFailure = new ValidationFailure("CachedField", "Cached error");
        var cachedResult = new ValidationResult(new[] { cachedFailure });
        
        var freshFailure = new ValidationFailure("FreshField", "Fresh error");
        var freshResult = new ValidationResult(new[] { freshFailure });

        var mockCachedValidator = new Mock<IValidator<TestRequest>>();
        var mockFreshValidator = new Mock<IValidator<TestRequest>>();
        mockFreshValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshResult);

        _mockCacheService.SetupSequence(x => x.GetCachedResultAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult)  // First validator has cached result
            .ReturnsAsync((ValidationResult?)null); // Second validator has no cached result

        var validators = new List<IValidator<TestRequest>> { mockCachedValidator.Object, mockFreshValidator.Object };
        var behavior = new CachedValidationBehavior<TestRequest, string>(validators, _mockCacheService.Object, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains("CachedField", exception.Errors.Keys);
        Assert.Contains("FreshField", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Log_Cache_Hit_And_Miss_Statistics()
    {
        // Arrange
        _mockCacheService.SetupSequence(x => x.GetCachedResultAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()) // Cache hit
            .ReturnsAsync((ValidationResult?)null); // Cache miss

        var mockValidator1 = new Mock<IValidator<TestRequest>>();
        var mockValidator2 = new Mock<IValidator<TestRequest>>();
        mockValidator2.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { mockValidator1.Object, mockValidator2.Object };
        var behavior = new CachedValidationBehavior<TestRequest, string>(validators, _mockCacheService.Object, _mockLogger.Object);
        var request = new TestRequest("Test");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1 cache hits, 1 cache misses")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helper class for testing cacheable validator type detection
    public class AsyncTestRequestValidator : IValidator<TestRequest>
    {
        public ValidationResult Validate(IValidationContext context) => new();
        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) => Task.FromResult(new ValidationResult());
        public ValidationResult Validate(TestRequest instance) => new();
        public Task<ValidationResult> ValidateAsync(TestRequest instance, CancellationToken cancellation = default) => Task.FromResult(new ValidationResult());
        public IValidatorDescriptor CreateDescriptor() => throw new NotImplementedException();
        public bool CanValidateInstancesOfType(Type type) => type == typeof(TestRequest);
    }
}