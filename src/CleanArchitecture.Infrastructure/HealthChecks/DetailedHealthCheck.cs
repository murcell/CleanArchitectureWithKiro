using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CleanArchitecture.Infrastructure.HealthChecks;

/// <summary>
/// Detailed health check that provides comprehensive system information
/// </summary>
public class DetailedHealthCheck : IHealthCheck
{
    private readonly ILogger<DetailedHealthCheck> _logger;

    public DetailedHealthCheck(ILogger<DetailedHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                ["version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                ["machine_name"] = Environment.MachineName,
                ["os_version"] = Environment.OSVersion.ToString(),
                ["processor_count"] = Environment.ProcessorCount,
                ["working_set"] = GC.GetTotalMemory(false),
                ["uptime"] = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss")
            };

            // Add GC information
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                data[$"gc_gen_{i}_collections"] = GC.CollectionCount(i);
            }

            _logger.LogDebug("Detailed health check completed successfully");
            
            return Task.FromResult(HealthCheckResult.Healthy("Application is healthy", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Health check failed", ex));
        }
    }
}