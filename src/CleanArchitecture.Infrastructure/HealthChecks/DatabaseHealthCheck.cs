using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Infrastructure.Data;

namespace CleanArchitecture.Infrastructure.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to connect to the database and execute a simple query
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Check if we can execute a simple query
            var canQuery = await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1", cancellationToken) >= 0;

            if (canQuery)
            {
                return HealthCheckResult.Healthy("Database is accessible and responsive.");
            }
            else
            {
                return HealthCheckResult.Degraded("Database is accessible but query execution failed.");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible.", ex);
        }
    }
}