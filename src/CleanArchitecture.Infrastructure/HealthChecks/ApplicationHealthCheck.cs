using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;

namespace CleanArchitecture.Infrastructure.HealthChecks;

public class ApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var buildDate = GetBuildDate(assembly);
            
            var data = new Dictionary<string, object>
            {
                ["Version"] = version,
                ["BuildDate"] = buildDate,
                ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                ["MachineName"] = Environment.MachineName,
                ["ProcessId"] = Environment.ProcessId,
                ["WorkingSet"] = GC.GetTotalMemory(false),
                ["GCCollectionCount"] = GC.CollectionCount(0)
            };

            return Task.FromResult(HealthCheckResult.Healthy("Application is running normally.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Application health check failed.", ex));
        }
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        try
        {
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute != null && DateTime.TryParse(attribute.InformationalVersion, out var buildDate))
            {
                return buildDate;
            }
            
            // Fallback to file creation time
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return File.GetCreationTime(location);
            }
        }
        catch
        {
            // Ignore errors and return default
        }
        
        return DateTime.UtcNow;
    }
}