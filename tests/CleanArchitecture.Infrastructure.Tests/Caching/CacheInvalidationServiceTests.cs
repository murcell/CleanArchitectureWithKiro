using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Moq;

namespace CleanArchitecture.Infrastructure.Tests.Caching;

public class CacheInvalidationServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICacheKeyService> _cacheKeyServiceMock;
    private readonly Mock<ILogger<CacheInvalidationService>> _loggerMock;
    private readonly CacheInvalidationService _invalidationService;

    public CacheInvalidationServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _cacheKeyServiceMock = new Mock<ICacheKeyService>();
        _loggerMock = new Mock<ILogger<CacheInvalidationService>>();
        
        _invalidationService = new CacheInvalidationService(
            _cacheServiceMock.Object,
            _cacheKeyServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InvalidateEntityAsync_ShouldRemoveAllRelatedKeys()
    {
        // Arrange
        var entityName = "User";
        var entityId = 123;
        var relatedKeys = new List<string>
        {
            "TestApp:User:123",
            "TestApp:User:list",
            "TestApp:User:count",
            "TestApp:User:search:*"
        };
        var entityPattern = "TestApp:User:*";

        _cacheKeyServiceMock
            .Setup(x => x.GetEntityKeys(entityName, entityId))
            .Returns(relatedKeys);
        
        _cacheKeyServiceMock
            .Setup(x => x.GetEntityPattern(entityName))
            .Returns(entityPattern);

        // Act
        await _invalidationService.InvalidateEntityAsync(entityName, entityId);

        // Assert
        foreach (var key in relatedKeys)
        {
            _cacheServiceMock.Verify(x => x.RemoveAsync(key), Times.Once);
        }
        
        _cacheServiceMock.Verify(x => x.RemoveByPatternAsync(entityPattern), Times.Once);
    }

    [Fact]
    public async Task InvalidateEntityTypeAsync_ShouldRemoveAllKeysForEntityType()
    {
        // Arrange
        var entityName = "Product";
        var entityPattern = "TestApp:Product:*";

        _cacheKeyServiceMock
            .Setup(x => x.GetEntityPattern(entityName))
            .Returns(entityPattern);

        // Act
        await _invalidationService.InvalidateEntityTypeAsync(entityName);

        // Assert
        _cacheServiceMock.Verify(x => x.RemoveByPatternAsync(entityPattern), Times.Once);
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_WithSpecificDataType_ShouldRemoveUserSpecificKeys()
    {
        // Arrange
        var userId = 456;
        var dataType = "profile";
        var userKey = "TestApp:user:456:profile";

        _cacheKeyServiceMock
            .Setup(x => x.GenerateUserKey(userId, dataType))
            .Returns(userKey);

        // Act
        await _invalidationService.InvalidateUserCacheAsync(userId, dataType);

        // Assert
        _cacheServiceMock.Verify(x => x.RemoveAsync(userKey), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveByPatternAsync($"{userKey}*"), Times.Once);
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_WithoutDataType_ShouldRemoveAllUserKeys()
    {
        // Arrange
        var userId = 456;
        var userPattern = "TestApp:user:456:*";

        _cacheKeyServiceMock
            .Setup(x => x.GenerateUserKey(userId, "*"))
            .Returns(userPattern);

        // Act
        await _invalidationService.InvalidateUserCacheAsync(userId);

        // Assert
        _cacheServiceMock.Verify(x => x.RemoveByPatternAsync(userPattern), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateMultipleEntitiesAsync_ShouldInvalidateAllSpecifiedEntities()
    {
        // Arrange
        var entityInvalidations = new Dictionary<string, List<object>>
        {
            { "User", new List<object> { 1, 2 } },
            { "Product", new List<object> { 10, 20, 30 } }
        };

        var userKeys1 = new List<string> { "TestApp:User:1", "TestApp:User:list" };
        var userKeys2 = new List<string> { "TestApp:User:2", "TestApp:User:list" };
        var productKeys1 = new List<string> { "TestApp:Product:10", "TestApp:Product:list" };
        var productKeys2 = new List<string> { "TestApp:Product:20", "TestApp:Product:list" };
        var productKeys3 = new List<string> { "TestApp:Product:30", "TestApp:Product:list" };

        _cacheKeyServiceMock.Setup(x => x.GetEntityKeys("User", 1)).Returns(userKeys1);
        _cacheKeyServiceMock.Setup(x => x.GetEntityKeys("User", 2)).Returns(userKeys2);
        _cacheKeyServiceMock.Setup(x => x.GetEntityKeys("Product", 10)).Returns(productKeys1);
        _cacheKeyServiceMock.Setup(x => x.GetEntityKeys("Product", 20)).Returns(productKeys2);
        _cacheKeyServiceMock.Setup(x => x.GetEntityKeys("Product", 30)).Returns(productKeys3);

        _cacheKeyServiceMock.Setup(x => x.GetEntityPattern("User")).Returns("TestApp:User:*");
        _cacheKeyServiceMock.Setup(x => x.GetEntityPattern("Product")).Returns("TestApp:Product:*");

        // Act
        await _invalidationService.InvalidateMultipleEntitiesAsync(entityInvalidations);

        // Assert
        // Verify that invalidation was called for each entity
        _cacheServiceMock.Verify(x => x.RemoveByPatternAsync("TestApp:User:*"), Times.Exactly(2));
        _cacheServiceMock.Verify(x => x.RemoveByPatternAsync("TestApp:Product:*"), Times.Exactly(3));
    }

    [Fact]
    public async Task InvalidateEntityAsync_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var entityName = "User";
        var entityId = 123;
        var exception = new Exception("Cache error");

        _cacheKeyServiceMock
            .Setup(x => x.GetEntityKeys(entityName, entityId))
            .Throws(exception);

        // Act
        await _invalidationService.InvalidateEntityAsync(entityName, entityId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error invalidating cache")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}