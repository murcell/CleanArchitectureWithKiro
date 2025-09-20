using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Application.Common.Interfaces;
using Moq;

namespace CleanArchitecture.WebAPI.Tests.Common;

/// <summary>
/// Lightweight WebApplicationFactory using in-memory services for fast unit tests
/// </summary>
public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName;

    public InMemoryWebApplicationFactory()
    {
        _databaseName = Guid.NewGuid().ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddJsonFile("appsettings.json", optional: false)
                  .AddJsonFile("appsettings.Testing.json", optional: false);

            // Override with in-memory configurations
            var testConfig = new Dictionary<string, string?>
            {
                ["Cache:EnableDistributedCache"] = "false",
                ["RabbitMQ:EnableAutomaticRecovery"] = "false",
                ["Database:EnableAutomaticMigrations"] = "false"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextServiceDescriptor != null)
            {
                services.Remove(dbContextServiceDescriptor);
            }

            // Remove cache and message queue services
            RemoveService<ICacheService>(services);
            RemoveService<IMessageQueueService>(services);

            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Add mock cache service
            var mockCacheService = new Mock<ICacheService>();
            services.AddSingleton(mockCacheService.Object);

            // Add mock message queue service
            var mockMessageQueueService = new Mock<IMessageQueueService>();
            mockMessageQueueService.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                  .Returns(Task.CompletedTask);
            services.AddSingleton(mockMessageQueueService.Object);

            // Configure logging for tests
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Creates a new database context for direct database operations in tests
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Seeds the test database with initial data
    /// </summary>
    public async Task SeedDatabaseAsync(Func<ApplicationDbContext, Task> seedAction)
    {
        using var context = CreateDbContext();
        await seedAction(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Cleans the test database by removing all data
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        using var context = CreateDbContext();
        
        // Remove all data from tables
        context.Users.RemoveRange(context.Users);
        context.Products.RemoveRange(context.Products);
        
        await context.SaveChangesAsync();
    }

    public Task InitializeAsync()
    {
        // No initialization needed for in-memory factory
        return Task.CompletedTask;
    }

    public new Task DisposeAsync()
    {
        // Call base dispose
        return base.DisposeAsync().AsTask();
    }
}