using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Infrastructure.Data.SeedData;

namespace CleanArchitecture.Infrastructure.Data.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending migrations and seeds the database
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            // Apply pending migrations
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
            
            // Seed the database
            await ApplicationDbContextSeed.SeedAsync(context, logger);
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }
    
    /// <summary>
    /// Ensures the database is created (for development/testing)
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            var created = await context.Database.EnsureCreatedAsync();
            if (created)
            {
                logger.LogInformation("Database created successfully");
                await ApplicationDbContextSeed.SeedAsync(context, logger);
            }
            else
            {
                logger.LogInformation("Database already exists");
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "An error occurred while ensuring database creation");
            throw;
        }
    }
}