using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Tests.Caching;

public class CacheKeyServiceTests
{
    private readonly CacheKeyService _cacheKeyService;
    private readonly CacheOptions _options;

    public CacheKeyServiceTests()
    {
        _options = new CacheOptions
        {
            KeyPrefix = "TestApp"
        };
        
        var optionsWrapper = Options.Create(_options);
        _cacheKeyService = new CacheKeyService(optionsWrapper);
    }

    [Fact]
    public void GenerateKey_ShouldCreateCorrectFormat()
    {
        // Arrange
        var entityName = "User";
        var id = 123;

        // Act
        var result = _cacheKeyService.GenerateKey(entityName, id);

        // Assert
        Assert.Equal("TestApp:User:123", result);
    }

    [Fact]
    public void GenerateListKey_WithoutParameters_ShouldCreateCorrectFormat()
    {
        // Arrange
        var entityName = "User";

        // Act
        var result = _cacheKeyService.GenerateListKey(entityName);

        // Assert
        Assert.Equal("TestApp:User:list", result);
    }

    [Fact]
    public void GenerateListKey_WithParameters_ShouldCreateCorrectFormat()
    {
        // Arrange
        var entityName = "User";
        var parameters = new object[] { "active", "page1" };

        // Act
        var result = _cacheKeyService.GenerateListKey(entityName, parameters);

        // Assert
        Assert.Equal("TestApp:User:list:active:page1", result);
    }

    [Fact]
    public void GenerateUserKey_WithoutParameters_ShouldCreateCorrectFormat()
    {
        // Arrange
        var userId = 456;
        var dataType = "profile";

        // Act
        var result = _cacheKeyService.GenerateUserKey(userId, dataType);

        // Assert
        Assert.Equal("TestApp:user:456:profile", result);
    }

    [Fact]
    public void GenerateUserKey_WithParameters_ShouldCreateCorrectFormat()
    {
        // Arrange
        var userId = 456;
        var dataType = "orders";
        var parameters = new object[] { "2024", "pending" };

        // Act
        var result = _cacheKeyService.GenerateUserKey(userId, dataType, parameters);

        // Assert
        Assert.Equal("TestApp:user:456:orders:2024:pending", result);
    }

    [Fact]
    public void GetEntityKeys_ShouldReturnAllRelatedKeys()
    {
        // Arrange
        var entityName = "Product";
        var id = 789;

        // Act
        var result = _cacheKeyService.GetEntityKeys(entityName, id).ToList();

        // Assert
        Assert.Contains("TestApp:Product:789", result);
        Assert.Contains("TestApp:Product:list", result);
        Assert.Contains("TestApp:Product:count", result);
        Assert.Contains("TestApp:Product:search:*", result);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void GetEntityPattern_ShouldReturnCorrectPattern()
    {
        // Arrange
        var entityName = "Order";

        // Act
        var result = _cacheKeyService.GetEntityPattern(entityName);

        // Assert
        Assert.Equal("TestApp:Order:*", result);
    }
}