using CleanArchitecture.WebAPI.Tests.Common;
using CleanArchitecture.Application.Features.Users.Commands.CreateUser;
using System.Text.Json;
using System.Text;
using System.Diagnostics;

namespace CleanArchitecture.Performance.Tests.LoadTests;

/// <summary>
/// Load tests for API endpoints using basic HTTP client testing
/// </summary>
[Trait("Category", "LoadTest")]
public class ApiLoadTests : IDisposable
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _httpClient = null!;

    private void Initialize()
    {
        _factory = new TestWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    /// <summary>
    /// Load test for health check endpoint
    /// </summary>
    [Fact]
    public async Task LoadTest_HealthCheck_ShouldHandleHighLoad()
    {
        Initialize();

        const int numberOfRequests = 100;
        const int concurrentRequests = 10;
        var stopwatch = new Stopwatch();
        var successCount = 0;
        var failureCount = 0;
        var responseTimes = new List<long>();

        stopwatch.Start();
        
        var semaphore = new SemaphoreSlim(concurrentRequests);
        var tasks = new List<Task>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    var response = await _httpClient.GetAsync("/health");
                    requestStopwatch.Stop();
                    
                    lock (responseTimes)
                    {
                        responseTimes.Add(requestStopwatch.ElapsedMilliseconds);
                        if (response.IsSuccessStatusCode)
                            successCount++;
                        else
                            failureCount++;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert performance thresholds
        Assert.True(successCount > 0, "No successful requests recorded");
        Assert.True(failureCount == 0, $"Health check should not fail, but {failureCount} failures recorded");
        
        var averageResponseTime = responseTimes.Average();
        Assert.True(averageResponseTime < 100, 
            $"Average response time {averageResponseTime}ms exceeds 100ms threshold");
        
        var totalTime = stopwatch.ElapsedMilliseconds;
        var requestsPerSecond = (double)numberOfRequests / (totalTime / 1000.0);
        Assert.True(requestsPerSecond > 50, 
            $"Throughput {requestsPerSecond:F2} requests/second is below 50 req/s threshold");
    }

    /// <summary>
    /// Load test for user operations
    /// </summary>
    [Fact]
    public async Task LoadTest_UserOperations_ShouldMeetPerformanceThresholds()
    {
        Initialize();

        const int numberOfRequests = 50;
        const int concurrentRequests = 5;
        var responseTimes = new List<long>();
        var successCount = 0;
        var failureCount = 0;

        var semaphore = new SemaphoreSlim(concurrentRequests);
        var tasks = new List<Task>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            var requestIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    // Test creating a user
                    var command = new CreateUserCommand($"LoadTestUser_{requestIndex}", $"loadtest_{requestIndex}@example.com");
                    var json = JsonSerializer.Serialize(command);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await _httpClient.PostAsync("/api/users", content);
                    stopwatch.Stop();
                    
                    lock (responseTimes)
                    {
                        responseTimes.Add(stopwatch.ElapsedMilliseconds);
                        if (response.IsSuccessStatusCode)
                            successCount++;
                        else
                            failureCount++;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert performance thresholds
        Assert.True(successCount > 0, "No successful user creation requests recorded");
        
        var averageResponseTime = responseTimes.Average();
        Assert.True(averageResponseTime < 500, 
            $"User operations average response time {averageResponseTime}ms exceeds 500ms threshold");
        
        // Allow some failures but not more than 10%
        var errorRate = (double)failureCount / (successCount + failureCount);
        Assert.True(errorRate < 0.1, 
            $"User operations error rate {errorRate:P2} exceeds 10% threshold");
    }

    /// <summary>
    /// Load test for mixed workload
    /// </summary>
    [Fact]
    public async Task LoadTest_MixedWorkload_ShouldMaintainPerformance()
    {
        Initialize();

        const int totalRequests = 100;
        const int concurrentRequests = 10;
        var responseTimes = new Dictionary<string, List<long>>
        {
            ["health"] = new List<long>(),
            ["users"] = new List<long>()
        };
        var successCounts = new Dictionary<string, int>
        {
            ["health"] = 0,
            ["users"] = 0
        };

        var semaphore = new SemaphoreSlim(concurrentRequests);
        var tasks = new List<Task>();

        for (int i = 0; i < totalRequests; i++)
        {
            var requestIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    if (requestIndex % 2 == 0)
                    {
                        // Health check request
                        var response = await _httpClient.GetAsync("/health");
                        stopwatch.Stop();
                        
                        lock (responseTimes)
                        {
                            responseTimes["health"].Add(stopwatch.ElapsedMilliseconds);
                            if (response.IsSuccessStatusCode)
                                successCounts["health"]++;
                        }
                    }
                    else
                    {
                        // User creation request
                        var command = new CreateUserCommand($"MixedLoadUser_{requestIndex}", $"mixed_{requestIndex}@example.com");
                        var json = JsonSerializer.Serialize(command);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        
                        var response = await _httpClient.PostAsync("/api/users", content);
                        stopwatch.Stop();
                        
                        lock (responseTimes)
                        {
                            responseTimes["users"].Add(stopwatch.ElapsedMilliseconds);
                            if (response.IsSuccessStatusCode)
                                successCounts["users"]++;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert performance thresholds for each operation type
        if (responseTimes["health"].Any())
        {
            var healthAvg = responseTimes["health"].Average();
            Assert.True(healthAvg < 100, 
                $"Health check average response time {healthAvg}ms exceeds 100ms threshold");
        }

        if (responseTimes["users"].Any())
        {
            var usersAvg = responseTimes["users"].Average();
            Assert.True(usersAvg < 500, 
                $"User operations average response time {usersAvg}ms exceeds 500ms threshold");
        }

        Assert.True(successCounts["health"] > 0 || successCounts["users"] > 0, 
            "No successful requests recorded in mixed workload test");
    }

    /// <summary>
    /// Stress test to evaluate system under high concurrent load
    /// </summary>
    [Fact]
    public async Task LoadTest_StressTest_ShouldHandleHighConcurrency()
    {
        Initialize();

        const int numberOfRequests = 200;
        const int concurrentRequests = 20;
        var responseTimes = new List<long>();
        var successCount = 0;
        var failureCount = 0;

        var semaphore = new SemaphoreSlim(concurrentRequests);
        var tasks = new List<Task>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            var requestIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    // Alternate between different endpoints
                    var endpointChoice = requestIndex % 3;
                    var endpoint = endpointChoice switch
                    {
                        0 => "/health",
                        1 => $"/api/users/{(requestIndex % 100) + 1}",
                        _ => "/health"
                    };
                    
                    var response = await _httpClient.GetAsync(endpoint);
                    stopwatch.Stop();
                    
                    lock (responseTimes)
                    {
                        responseTimes.Add(stopwatch.ElapsedMilliseconds);
                        if (response.IsSuccessStatusCode)
                            successCount++;
                        else
                            failureCount++;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert stress test thresholds
        Assert.True(successCount > 0, "No successful requests recorded in stress test");
        
        var averageResponseTime = responseTimes.Average();
        Assert.True(averageResponseTime < 1000, 
            $"Stress test average response time {averageResponseTime}ms exceeds 1000ms threshold");
        
        var p95ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(responseTimes.Count * 0.95)).First();
        Assert.True(p95ResponseTime < 2000, 
            $"Stress test 95th percentile response time {p95ResponseTime}ms exceeds 2000ms threshold");
        
        // Error rate should be reasonable even under stress
        var errorRate = (double)failureCount / (successCount + failureCount);
        Assert.True(errorRate < 0.2, 
            $"Stress test error rate {errorRate:P2} exceeds 20% threshold");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
    }
}