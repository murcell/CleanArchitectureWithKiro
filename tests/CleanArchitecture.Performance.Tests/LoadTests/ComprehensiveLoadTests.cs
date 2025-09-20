using CleanArchitecture.WebAPI.Tests.Common;
using System.Text.Json;
using System.Text;
using System.Diagnostics;

namespace CleanArchitecture.Performance.Tests.LoadTests;

/// <summary>
/// Comprehensive load tests covering various performance scenarios
/// </summary>
[Trait("Category", "LoadTest")]
public class ComprehensiveLoadTests : IDisposable
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _httpClient = null!;

    public ComprehensiveLoadTests()
    {
        _factory = new TestWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public async Task LoadTest_HealthCheck_ShouldMeetPerformanceThresholds()
    {
        const int numberOfRequests = 100;
        const int concurrentRequests = 10;
        var responseTimes = new List<long>();
        var successCount = 0;
        var failureCount = 0;

        var semaphore = new SemaphoreSlim(concurrentRequests);
        var tasks = new List<Task>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await _httpClient.GetAsync("/health");
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
        Assert.True(successCount > 0, "No successful requests recorded");
        Assert.True(failureCount == 0, $"Health check should not fail, but {failureCount} failures recorded");
        
        var averageResponseTime = responseTimes.Average();
        Assert.True(averageResponseTime < 100, 
            $"Health check average response time {averageResponseTime}ms exceeds 100ms threshold");
        
        var p95ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(responseTimes.Count * 0.95)).First();
        Assert.True(p95ResponseTime < 200, 
            $"Health check 95th percentile {p95ResponseTime}ms exceeds 200ms threshold");
    }

    [Fact]
    public async Task LoadTest_ConcurrentUsers_ShouldScaleWell()
    {
        const int concurrentUsers = 20;
        const int requestsPerUser = 10;
        var allResponseTimes = new List<long>();
        var successCount = 0;
        var failureCount = 0;

        var userTasks = new List<Task>();
        
        for (int user = 0; user < concurrentUsers; user++)
        {
            userTasks.Add(Task.Run(async () =>
            {
                var userResponseTimes = new List<long>();
                
                for (int request = 0; request < requestsPerUser; request++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await _httpClient.GetAsync("/health");
                    stopwatch.Stop();
                    
                    userResponseTimes.Add(stopwatch.ElapsedMilliseconds);
                    
                    if (response.IsSuccessStatusCode)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref failureCount);
                    
                    // Small delay between requests from same user
                    await Task.Delay(10);
                }
                
                lock (allResponseTimes)
                {
                    allResponseTimes.AddRange(userResponseTimes);
                }
            }));
        }

        await Task.WhenAll(userTasks);

        // Assert scalability
        var totalRequests = concurrentUsers * requestsPerUser;
        Assert.Equal(totalRequests, successCount + failureCount);
        Assert.True(successCount > 0, "No successful requests recorded");
        Assert.True(failureCount == 0, $"Health check should not fail, but {failureCount} failures recorded");
        
        var averageResponseTime = allResponseTimes.Average();
        var maxResponseTime = allResponseTimes.Max();
        var p95ResponseTime = allResponseTimes.OrderBy(x => x).Skip((int)(allResponseTimes.Count * 0.95)).First();
        
        Assert.True(averageResponseTime < 150, 
            $"Concurrent users test average response time {averageResponseTime}ms exceeds 150ms threshold");
        
        Assert.True(p95ResponseTime < 300, 
            $"Concurrent users test 95th percentile response time {p95ResponseTime}ms exceeds 300ms threshold");
        
        Assert.True(maxResponseTime < 1000, 
            $"Concurrent users test maximum response time {maxResponseTime}ms exceeds 1000ms threshold");
    }

    [Fact]
    public async Task LoadTest_SustainedLoad_ShouldMaintainStability()
    {
        const int durationSeconds = 30;
        const int requestsPerSecond = 10;
        var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
        var responseTimes = new List<long>();
        var successCount = 0;
        var failureCount = 0;

        var tasks = new List<Task>();
        
        while (DateTime.UtcNow < endTime)
        {
            var batchTasks = new List<Task>();
            
            for (int i = 0; i < requestsPerSecond; i++)
            {
                batchTasks.Add(Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await _httpClient.GetAsync("/health");
                    stopwatch.Stop();
                    
                    lock (responseTimes)
                    {
                        responseTimes.Add(stopwatch.ElapsedMilliseconds);
                        if (response.IsSuccessStatusCode)
                            successCount++;
                        else
                            failureCount++;
                    }
                }));
            }
            
            tasks.AddRange(batchTasks);
            await Task.Delay(1000); // Wait 1 second before next batch
        }

        await Task.WhenAll(tasks);

        // Assert sustained performance
        Assert.True(successCount > 0, "No successful requests recorded");
        Assert.True(failureCount == 0, $"Health check should not fail, but {failureCount} failures recorded");
        
        var averageResponseTime = responseTimes.Average();
        var actualRequestsPerSecond = (double)responseTimes.Count / durationSeconds;
        
        Assert.True(averageResponseTime < 200, 
            $"Sustained load average response time {averageResponseTime}ms exceeds 200ms threshold");
        
        Assert.True(actualRequestsPerSecond >= requestsPerSecond * 0.9, 
            $"Actual throughput {actualRequestsPerSecond:F2} req/s is below 90% of target {requestsPerSecond} req/s");
        
        // Check for performance degradation over time
        var firstHalfAvg = responseTimes.Take(responseTimes.Count / 2).Average();
        var secondHalfAvg = responseTimes.Skip(responseTimes.Count / 2).Average();
        var degradationRatio = secondHalfAvg / firstHalfAvg;
        
        Assert.True(degradationRatio < 2.0, 
            $"Performance degraded significantly over time: {degradationRatio:F2}x slower in second half");
    }

    [Fact]
    public async Task LoadTest_MemoryPressure_ShouldHandleMultipleRequests()
    {
        const int numberOfRequests = 500;
        const int concurrentRequests = 25;
        var responseTimes = new List<long>();
        var successCount = 0;
        var failureCount = 0;

        var semaphore = new SemaphoreSlim(concurrentRequests);
        var tasks = new List<Task>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await _httpClient.GetAsync("/health");
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

        // Assert memory pressure handling
        Assert.True(successCount > 0, "No successful requests recorded");
        
        var averageResponseTime = responseTimes.Average();
        Assert.True(averageResponseTime < 300, 
            $"Memory pressure test average response time {averageResponseTime}ms exceeds 300ms threshold");
        
        // Error rate should be low even under memory pressure
        var errorRate = (double)failureCount / (successCount + failureCount);
        Assert.True(errorRate < 0.05, 
            $"Memory pressure test error rate {errorRate:P2} exceeds 5% threshold");
        
        // Check response time consistency
        var responseTimeStdDev = Math.Sqrt(responseTimes.Select(x => Math.Pow(x - averageResponseTime, 2)).Average());
        Assert.True(responseTimeStdDev < averageResponseTime * 2, 
            $"Response time standard deviation {responseTimeStdDev}ms indicates high variability");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
    }
}