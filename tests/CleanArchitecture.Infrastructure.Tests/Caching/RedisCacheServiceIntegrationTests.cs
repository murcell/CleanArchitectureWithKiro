using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace CleanArchitecture.Infrastructure.Tests.Caching;

public class RedisCacheServiceIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IConnectionMultiplexer? _connectionMultiplexer;
    private IDistributedCache? _distributedCache;
    private RedisCacheService? _cacheService;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;

    public RedisCacheServiceIntegrationTests()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
        
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        
        var connectionString = _redisContainer.GetConnectionString();
        _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        
        // Create distributed cache
        _distributedCache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(
            Options.Create(new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
            {
                Configuration = connectionString,
                InstanceName = "TestInstance"
            }));

        var cacheOptions = Options.Create(new CacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(30),
            KeyPrefix = "TestApp"
        });

        _cacheService = new RedisCacheService(
            _distributedCache,
            _connectionMultiplexer,
            cacheOptions,
            _loggerMock.Object);
    }

    public async Task DisposeAsync()
    {
        _connectionMultiplexer?.Dispose();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Id = 1, Name = "Test" };
        await _cacheService!.SetAsync(key, value);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _cacheService!.GetAsync<TestObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValue()
    {
        // Arrange
        var key = "test-key-2";
        var value = new TestObject { Id = 2, Name = "Test2" };

        // Act
        await _cacheService!.SetAsync(key, value);

        // Assert
        var result = await _cacheService.GetAsync<TestObject>(key);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpireAfterTime()
    {
        // Arrange
        var key = "expiring-key";
        var value = new TestObject { Id = 3, Name = "Test3" };
        var expiration = TimeSpan.FromSeconds(1);

        // Act
        await _cacheService!.SetAsync(key, value, expiration);

        // Assert - Value should exist initially
        var result1 = await _cacheService.GetAsync<TestObject>(key);
        Assert.NotNull(result1);

        // Wait for expiration
        await Task.Delay(1100);

        // Value should be expired
        var result2 = await _cacheService.GetAsync<TestObject>(key);
        Assert.Null(result2);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValue()
    {
        // Arrange
        var key = "test-key-3";
        var value = new TestObject { Id = 4, Name = "Test4" };
        await _cacheService!.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        var result = await _cacheService.GetAsync<TestObject>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveByPatternAsync_ShouldRemoveMatchingKeys()
    {
        // Arrange
        var key1 = "user:123:profile";
        var key2 = "user:123:settings";
        var key3 = "user:456:profile";
        var value = new TestObject { Id = 5, Name = "Test5" };

        await _cacheService!.SetAsync(key1, value);
        await _cacheService.SetAsync(key2, value);
        await _cacheService.SetAsync(key3, value);

        // Act
        await _cacheService.RemoveByPatternAsync("user:123:*");

        // Assert
        var result1 = await _cacheService.GetAsync<TestObject>(key1);
        var result2 = await _cacheService.GetAsync<TestObject>(key2);
        var result3 = await _cacheService.GetAsync<TestObject>(key3);

        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3); // This should still exist
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "test-key-4";
        var value = new TestObject { Id = 6, Name = "Test6" };
        await _cacheService!.SetAsync(key, value);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var key = "non-existent-key-2";

        // Act
        var result = await _cacheService!.ExistsAsync(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyExists_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test-key-5";
        var cachedValue = new TestObject { Id = 7, Name = "Cached" };
        var factoryValue = new TestObject { Id = 8, Name = "Factory" };
        
        await _cacheService!.SetAsync(key, cachedValue);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedValue.Id, result.Id);
        Assert.Equal(cachedValue.Name, result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyDoesNotExist_ShouldCallFactoryAndCache()
    {
        // Arrange
        var key = "test-key-6";
        var factoryValue = new TestObject { Id = 9, Name = "Factory" };

        // Act
        var result = await _cacheService!.GetOrSetAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(factoryValue.Id, result.Id);
        Assert.Equal(factoryValue.Name, result.Name);

        // Verify it was cached
        var cachedResult = await _cacheService.GetAsync<TestObject>(key);
        Assert.NotNull(cachedResult);
        Assert.Equal(factoryValue.Id, cachedResult.Id);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldHandleMultipleOperations()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "concurrent-test";

        // Act - Perform multiple concurrent operations
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"{keyPrefix}:{index}";
                var value = new TestObject { Id = index, Name = $"Test{index}" };
                
                await _cacheService!.SetAsync(key, value);
                var result = await _cacheService.GetAsync<TestObject>(key);
                
                Assert.NotNull(result);
                Assert.Equal(index, result.Id);
            }));
        }

        // Assert
        await Task.WhenAll(tasks);
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}