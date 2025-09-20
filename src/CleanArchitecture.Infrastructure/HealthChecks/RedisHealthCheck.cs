using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace CleanArchitecture.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis connection is not established.");
            }

            var database = _connectionMultiplexer.GetDatabase();
            
            // Test basic Redis operations
            var testKey = "health_check_test";
            var testValue = DateTime.UtcNow.ToString();
            
            // Set a test value
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
            
            // Get the test value
            var retrievedValue = await database.StringGetAsync(testKey);
            
            if (retrievedValue == testValue)
            {
                // Clean up test key
                await database.KeyDeleteAsync(testKey);
                
                var serverInfo = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var info = await serverInfo.InfoAsync();
                
                var versionInfo = info.FirstOrDefault(x => x.Key == "redis_version");
                var version = versionInfo.Any() ? versionInfo.First().Value : "Unknown";
                
                return HealthCheckResult.Healthy($"Redis is healthy. Server version: {version}");
            }
            else
            {
                return HealthCheckResult.Degraded("Redis is accessible but data operations are not working correctly.");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", ex);
        }
    }
}