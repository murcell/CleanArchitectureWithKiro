using CleanArchitecture.Application.Common.Validators;
using FluentValidation.Results;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

public class ValidationCacheServiceTests
{
    private readonly InMemoryValidationCacheService _cacheService;

    public ValidationCacheServiceTests()
    {
        _cacheService = new InMemoryValidationCacheService();
    }

    [Fact]
    public async Task Should_Return_Null_When_No_Cached_Result()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };

        // Act
        var result = await _cacheService.GetCachedResultAsync(request, "TestValidator");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Return_Cached_Result_When_Available()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var validationResult = new ValidationResult();
        
        await _cacheService.SetCachedResultAsync(request, "TestValidator", validationResult);

        // Act
        var cachedResult = await _cacheService.GetCachedResultAsync(request, "TestValidator");

        // Assert
        Assert.NotNull(cachedResult);
        Assert.Equal(validationResult.Errors.Count, cachedResult.Errors.Count);
    }

    [Fact]
    public async Task Should_Return_Null_When_Cache_Expired()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var validationResult = new ValidationResult();
        var shortExpiration = TimeSpan.FromMilliseconds(10);
        
        await _cacheService.SetCachedResultAsync(request, "TestValidator", validationResult, shortExpiration);

        // Wait for expiration
        await Task.Delay(20);

        // Act
        var cachedResult = await _cacheService.GetCachedResultAsync(request, "TestValidator");

        // Assert
        Assert.Null(cachedResult);
    }

    [Fact]
    public async Task Should_Cache_Different_Requests_Separately()
    {
        // Arrange
        var request1 = new TestRequest { Name = "Test1" };
        var request2 = new TestRequest { Name = "Test2" };
        var result1 = new ValidationResult(new[] { new ValidationFailure("Name", "Error1") });
        var result2 = new ValidationResult(new[] { new ValidationFailure("Name", "Error2") });
        
        await _cacheService.SetCachedResultAsync(request1, "TestValidator", result1);
        await _cacheService.SetCachedResultAsync(request2, "TestValidator", result2);

        // Act
        var cachedResult1 = await _cacheService.GetCachedResultAsync(request1, "TestValidator");
        var cachedResult2 = await _cacheService.GetCachedResultAsync(request2, "TestValidator");

        // Assert
        Assert.NotNull(cachedResult1);
        Assert.NotNull(cachedResult2);
        Assert.Equal("Error1", cachedResult1.Errors.First().ErrorMessage);
        Assert.Equal("Error2", cachedResult2.Errors.First().ErrorMessage);
    }

    [Fact]
    public async Task Should_Cache_Different_Validators_Separately()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var result1 = new ValidationResult(new[] { new ValidationFailure("Name", "Validator1 Error") });
        var result2 = new ValidationResult(new[] { new ValidationFailure("Name", "Validator2 Error") });
        
        await _cacheService.SetCachedResultAsync(request, "Validator1", result1);
        await _cacheService.SetCachedResultAsync(request, "Validator2", result2);

        // Act
        var cachedResult1 = await _cacheService.GetCachedResultAsync(request, "Validator1");
        var cachedResult2 = await _cacheService.GetCachedResultAsync(request, "Validator2");

        // Assert
        Assert.NotNull(cachedResult1);
        Assert.NotNull(cachedResult2);
        Assert.Equal("Validator1 Error", cachedResult1.Errors.First().ErrorMessage);
        Assert.Equal("Validator2 Error", cachedResult2.Errors.First().ErrorMessage);
    }

    [Fact]
    public async Task Should_Invalidate_Cache_For_Specific_Type()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var validationResult = new ValidationResult();
        
        await _cacheService.SetCachedResultAsync(request, "TestValidator", validationResult);

        // Verify it's cached
        var cachedResult = await _cacheService.GetCachedResultAsync(request, "TestValidator");
        Assert.NotNull(cachedResult);

        // Act
        await _cacheService.InvalidateCacheAsync<TestRequest>();

        // Assert
        var invalidatedResult = await _cacheService.GetCachedResultAsync(request, "TestValidator");
        Assert.Null(invalidatedResult);
    }

    [Fact]
    public async Task Should_Invalidate_All_Cache()
    {
        // Arrange
        var request1 = new TestRequest { Name = "Test1" };
        var request2 = new AnotherTestRequest { Value = "Test2" };
        var result1 = new ValidationResult();
        var result2 = new ValidationResult();
        
        await _cacheService.SetCachedResultAsync(request1, "TestValidator", result1);
        await _cacheService.SetCachedResultAsync(request2, "AnotherValidator", result2);

        // Verify they're cached
        Assert.NotNull(await _cacheService.GetCachedResultAsync(request1, "TestValidator"));
        Assert.NotNull(await _cacheService.GetCachedResultAsync(request2, "AnotherValidator"));

        // Act
        await _cacheService.InvalidateAllAsync();

        // Assert
        Assert.Null(await _cacheService.GetCachedResultAsync(request1, "TestValidator"));
        Assert.Null(await _cacheService.GetCachedResultAsync(request2, "AnotherValidator"));
    }

    private class TestRequest
    {
        public string Name { get; set; } = string.Empty;

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    private class AnotherTestRequest
    {
        public string Value { get; set; } = string.Empty;

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}