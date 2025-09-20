using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Infrastructure.Data;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;
using DotNet.Testcontainers.Builders;

namespace CleanArchitecture.WebAPI.Tests.Common;

/// <summary>
/// Custom WebApplicationFactory for integration tests with test containers
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private readonly RedisContainer _redisContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public TestWebApplicationFactory()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd123")
            .WithCleanUp(true)
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration
            config.Sources.Clear();

            // Add base configuration
            config.AddJsonFile("appsettings.json", optional: false)
                  .AddJsonFile("appsettings.Testing.json", optional: false);

            // Override with test container connection strings
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["RabbitMQ:HostName"] = _rabbitMqContainer.Hostname,
                ["RabbitMQ:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMQ:UserName"] = "testuser",
                ["RabbitMQ:Password"] = "testpass",
                ["RabbitMQ:VirtualHost"] = "/",
                ["Cache:EnableDistributedCache"] = "true",
                ["Database:EnableAutomaticMigrations"] = "true"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_sqlContainer.GetConnectionString(), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Configure logging for tests
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        });
    }

    public async Task InitializeAsync()
    {
        // Start all containers in parallel
        var tasks = new[]
        {
            _sqlContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        };

        await Task.WhenAll(tasks);

        // Ensure database is created and migrated
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        // Stop all containers
        var tasks = new[]
        {
            _sqlContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask(),
            _rabbitMqContainer.DisposeAsync().AsTask()
        };

        await Task.WhenAll(tasks);
        await base.DisposeAsync();
    }

    /// <summary>
    /// Gets the SQL Server connection string for direct database access in tests
    /// </summary>
    public string GetSqlConnectionString() => _sqlContainer.GetConnectionString();

    /// <summary>
    /// Gets the Redis connection string for direct cache access in tests
    /// </summary>
    public string GetRedisConnectionString() => _redisContainer.GetConnectionString();

    /// <summary>
    /// Gets the RabbitMQ connection details for direct message queue access in tests
    /// </summary>
    public (string Host, int Port, string Username, string Password) GetRabbitMqConnection()
    {
        return (_rabbitMqContainer.Hostname, 
                _rabbitMqContainer.GetMappedPublicPort(5672), 
                "testuser", 
                "testpass");
    }

    /// <summary>
    /// Creates a new database context for direct database operations in tests
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
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
        
        // Remove all data from tables (in reverse dependency order)
        context.Users.RemoveRange(context.Users);
        context.Products.RemoveRange(context.Products);
        
        await context.SaveChangesAsync();
    }
}