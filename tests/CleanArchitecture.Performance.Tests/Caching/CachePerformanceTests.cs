using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Performance.Tests.Common;
using System.Diagnostics;
using System.Text.Json;

namespace CleanArchitecture.Performance.Tests.Caching;

public class CachePerformanceTests : PerformanceTestBase
{
    private ICacheService _cacheService = null!;
    private readonly List<string> _testKeys = new();
    private readonly List<TestCacheObject> _testObjects = new();

    protected override async Task InitializeAsync()
    {
        _cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // Prepare test data
        for (int i = 0; i < 100; i++)
        {
            _testKeys.Add($"test_key_{i}");
            _testObjects.Add(new TestCacheObject
            {
                Id = i,
                Name = $"TestObject_{i}",
                Data = $"Some test data for object {i}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Pre-populate cache with some data
        for (int i = 0; i < 50; i++)
        {
            await _cacheService.SetAsync(_testKeys[i], _testObjects[i], TimeSpan.FromMinutes(10));
        }
    }

    [Benchmark]
    public async Task<TestCacheObject?> GetFromCache_Performance()
    {
        var randomKey = _testKeys[Random.Shared.Next(50)]; // Use pre-populated keys
        return await _cacheService.GetAsync<TestCacheObject>(randomKey);
    }

    [Benchmark]
    public async Task SetToCache_Performance()
    {
        var randomIndex = Random.Shared.Next(_testObjects.Count);
        var key = $"benchmark_key_{randomIndex}_{Guid.NewGuid()}";
        await _cacheService.SetAsync(key, _testObjects[randomIndex], TimeSpan.FromMinutes(5));
    }

    [Benchmark]
    public async Task RemoveFromCache_Performance()
    {
        var key = $"remove_key_{Guid.NewGuid()}";
        // First set a value
        await _cacheService.SetAsync(key, _testObjects[0], TimeSpan.FromMinutes(1));
        // Then remove it
        await _cacheService.RemoveAsync(key);
    }

    [Benchmark]
    public async Task<bool> ExistsInCache_Performance()
    {
        var randomKey = _testKeys[Random.Shared.Next(50)]; // Use pre-populated keys
        return await _cacheService.ExistsAsync(randomKey);
    }

    [Fact]
    public async Task CacheGet_Performance_ShouldMeetThresholds()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var testObject = new TestCacheObject
        {
            Id = 999,
            Name = "PerformanceTestObject",
            Data = "Performance test data",
            CreatedAt = DateTime.UtcNow
        };
        
        const string testKey = "performance_test_key";
        await cacheService.SetAsync(testKey, testObject, TimeSpan.FromMinutes(5));

        var stopwatch = new Stopwatch();

        // Test: Cache get should complete within 50ms
        stopwatch.Start();
        var cachedObject = await cacheService.GetAsync<TestCacheObject>(testKey);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 50, 
            $"Cache get took {stopwatch.ElapsedMilliseconds}ms, exceeds 50ms threshold");
        Assert.NotNull(cachedObject);
        Assert.Equal(testObject.Id, cachedObject.Id);

        // Test: Multiple concurrent gets should complete within 200ms total
        stopwatch.Restart();
        var tasks = new List<Task<TestCacheObject?>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(cacheService.GetAsync<TestCacheObject>(testKey));
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 200, 
            $"10 concurrent cache gets took {stopwatch.ElapsedMilliseconds}ms, exceeds 200ms threshold");
        Assert.True(tasks.All(t => t.Result != null), "Some concurrent cache gets returned null");
    }

    [Fact]
    public async Task CacheSet_Performance_ShouldMeetThresholds()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var stopwatch = new Stopwatch();

        // Test: Cache set should complete within 100ms
        var testObject = new TestCacheObject
        {
            Id = 1000,
            Name = "SetPerformanceTest",
            Data = "Set performance test data",
            CreatedAt = DateTime.UtcNow
        };

        stopwatch.Start();
        await cacheService.SetAsync("set_performance_test", testObject, TimeSpan.FromMinutes(5));
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Cache set took {stopwatch.ElapsedMilliseconds}ms, exceeds 100ms threshold");

        // Verify the object was actually cached
        var cachedObject = await cacheService.GetAsync<TestCacheObject>("set_performance_test");
        Assert.NotNull(cachedObject);
        Assert.Equal(testObject.Id, cachedObject.Id);

        // Test: Batch cache sets should complete within 500ms
        var batchObjects = new List<(string key, TestCacheObject obj)>();
        for (int i = 0; i < 10; i++)
        {
            batchObjects.Add(($"batch_key_{i}", new TestCacheObject
            {
                Id = 2000 + i,
                Name = $"BatchObject_{i}",
                Data = $"Batch data {i}",
                CreatedAt = DateTime.UtcNow
            }));
        }

        stopwatch.Restart();
        var batchTasks = batchObjects.Select(item => 
            cacheService.SetAsync(item.key, item.obj, TimeSpan.FromMinutes(5)));
        await Task.WhenAll(batchTasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Batch cache set of 10 items took {stopwatch.ElapsedMilliseconds}ms, exceeds 500ms threshold");
    }

    [Fact]
    public async Task CacheHitRatio_Performance_ShouldMeetThresholds()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var hitCount = 0;
        var totalRequests = 100;

        // Pre-populate cache with 70% of the keys we'll request
        var cacheKeys = new List<string>();
        for (int i = 0; i < 70; i++)
        {
            var key = $"hit_ratio_key_{i}";
            cacheKeys.Add(key);
            await cacheService.SetAsync(key, new TestCacheObject
            {
                Id = i,
                Name = $"HitRatioObject_{i}",
                Data = $"Hit ratio data {i}",
                CreatedAt = DateTime.UtcNow
            }, TimeSpan.FromMinutes(10));
        }

        // Add keys that won't be in cache
        for (int i = 70; i < totalRequests; i++)
        {
            cacheKeys.Add($"hit_ratio_key_{i}");
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Test cache hit ratio
        for (int i = 0; i < totalRequests; i++)
        {
            var result = await cacheService.GetAsync<TestCacheObject>(cacheKeys[i]);
            if (result != null)
            {
                hitCount++;
            }
        }

        stopwatch.Stop();

        var hitRatio = (double)hitCount / totalRequests;
        
        // Assert: Hit ratio should be at least 70% (since we pre-populated 70% of keys)
        Assert.True(hitRatio >= 0.70, 
            $"Cache hit ratio {hitRatio:P2} is below 70% threshold");
        
        // Assert: Total time for 100 cache operations should be under 2 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"100 cache operations took {stopwatch.ElapsedMilliseconds}ms, exceeds 2000ms threshold");
    }

    [Fact]
    public async Task CacheExpiration_Performance_ShouldWork()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var testObject = new TestCacheObject
        {
            Id = 3000,
            Name = "ExpirationTest",
            Data = "Expiration test data",
            CreatedAt = DateTime.UtcNow
        };

        const string testKey = "expiration_test_key";

        // Test: Set with short expiration
        await cacheService.SetAsync(testKey, testObject, TimeSpan.FromMilliseconds(500));

        // Verify it exists immediately
        var immediateResult = await cacheService.GetAsync<TestCacheObject>(testKey);
        Assert.NotNull(immediateResult);

        // Wait for expiration
        await Task.Delay(600);

        // Verify it's expired
        var expiredResult = await cacheService.GetAsync<TestCacheObject>(testKey);
        Assert.Null(expiredResult);
    }

    [Fact]
    public async Task CacheConcurrency_Performance_ShouldHandleMultipleClients()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var concurrentOperations = 50;
        var stopwatch = new Stopwatch();

        // Test: Concurrent cache sets
        var setTasks = new List<Task>();
        stopwatch.Start();
        
        for (int i = 0; i < concurrentOperations; i++)
        {
            var index = i;
            setTasks.Add(Task.Run(async () =>
            {
                var obj = new TestCacheObject
                {
                    Id = 4000 + index,
                    Name = $"ConcurrentObject_{index}",
                    Data = $"Concurrent data {index}",
                    CreatedAt = DateTime.UtcNow
                };
                await cacheService.SetAsync($"concurrent_set_key_{index}", obj, TimeSpan.FromMinutes(5));
            }));
        }

        await Task.WhenAll(setTasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Concurrent cache sets took {stopwatch.ElapsedMilliseconds}ms, exceeds 2000ms threshold");

        // Test: Concurrent cache gets
        var getTasks = new List<Task<TestCacheObject?>>();
        stopwatch.Restart();
        
        for (int i = 0; i < concurrentOperations; i++)
        {
            var index = i;
            getTasks.Add(Task.Run(async () =>
            {
                return await cacheService.GetAsync<TestCacheObject>($"concurrent_set_key_{index}");
            }));
        }

        var results = await Task.WhenAll(getTasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Concurrent cache gets took {stopwatch.ElapsedMilliseconds}ms, exceeds 1000ms threshold");
        Assert.True(results.All(r => r != null), "Some concurrent cache gets returned null");
    }

    [Fact]
    public async Task CacheMemoryUsage_Performance_ShouldBeEfficient()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var largeObjectCount = 1000;
        var stopwatch = new Stopwatch();

        // Test: Cache large number of objects
        stopwatch.Start();
        for (int i = 0; i < largeObjectCount; i++)
        {
            var largeObject = new TestCacheObject
            {
                Id = 5000 + i,
                Name = $"LargeObject_{i}",
                Data = new string('X', 1000), // 1KB of data
                CreatedAt = DateTime.UtcNow
            };
            
            await cacheService.SetAsync($"large_object_key_{i}", largeObject, TimeSpan.FromMinutes(10));
        }
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"Caching {largeObjectCount} large objects took {stopwatch.ElapsedMilliseconds}ms, exceeds 10000ms threshold");

        // Test: Retrieve random objects efficiently
        var randomRetrievals = 100;
        stopwatch.Restart();
        
        for (int i = 0; i < randomRetrievals; i++)
        {
            var randomIndex = Random.Shared.Next(largeObjectCount);
            var result = await cacheService.GetAsync<TestCacheObject>($"large_object_key_{randomIndex}");
            Assert.NotNull(result);
        }
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Random retrieval of {randomRetrievals} objects took {stopwatch.ElapsedMilliseconds}ms, exceeds 1000ms threshold");
    }

    [Fact]
    public async Task CacheInvalidation_Performance_ShouldBeEfficient()
    {
        // Arrange
        var cacheService = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        var objectCount = 100;
        var stopwatch = new Stopwatch();

        // Setup: Cache multiple objects
        for (int i = 0; i < objectCount; i++)
        {
            var obj = new TestCacheObject
            {
                Id = 6000 + i,
                Name = $"InvalidationObject_{i}",
                Data = $"Invalidation data {i}",
                CreatedAt = DateTime.UtcNow
            };
            await cacheService.SetAsync($"invalidation_key_{i}", obj, TimeSpan.FromMinutes(10));
        }

        // Test: Bulk invalidation performance
        stopwatch.Start();
        var invalidationTasks = new List<Task>();
        
        for (int i = 0; i < objectCount; i++)
        {
            var index = i;
            invalidationTasks.Add(Task.Run(async () =>
            {
                await cacheService.RemoveAsync($"invalidation_key_{index}");
            }));
        }

        await Task.WhenAll(invalidationTasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Bulk cache invalidation took {stopwatch.ElapsedMilliseconds}ms, exceeds 2000ms threshold");

        // Verify all objects are invalidated
        for (int i = 0; i < objectCount; i++)
        {
            var result = await cacheService.GetAsync<TestCacheObject>($"invalidation_key_{i}");
            Assert.Null(result);
        }
    }

    protected override async Task CleanupAsync()
    {
        if (_cacheService != null)
        {
            // Clean up test keys
            var cleanupTasks = new List<Task>();
            
            foreach (var key in _testKeys)
            {
                cleanupTasks.Add(_cacheService.RemoveAsync(key));
            }

            // Clean up other test keys
            var additionalKeys = new[]
            {
                "performance_test_key",
                "set_performance_test",
                "expiration_test_key"
            };

            foreach (var key in additionalKeys)
            {
                cleanupTasks.Add(_cacheService.RemoveAsync(key));
            }

            // Clean up batch keys
            for (int i = 0; i < 10; i++)
            {
                cleanupTasks.Add(_cacheService.RemoveAsync($"batch_key_{i}"));
            }

            // Clean up hit ratio keys
            for (int i = 0; i < 100; i++)
            {
                cleanupTasks.Add(_cacheService.RemoveAsync($"hit_ratio_key_{i}"));
            }

            await Task.WhenAll(cleanupTasks);
        }
    }

    public class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}