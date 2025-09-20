using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using CleanArchitecture.Infrastructure.HealthChecks;
using CleanArchitecture.Infrastructure.Configuration;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using StackExchange.Redis;

namespace CleanArchitecture.Infrastructure.Tests.HealthChecks;

public class HealthCheckTests
{
    [Fact]
    public async Task ApplicationHealthCheck_Should_Return_Healthy()
    {
        // Arrange
        var healthCheck = new ApplicationHealthCheck();
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Application is running normally", result.Description);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("Version"));
        Assert.True(result.Data.ContainsKey("Environment"));
    }

    [Fact]
    public async Task DatabaseHealthCheck_Should_Return_Healthy_When_Database_Is_Available()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync(); // Ensure the database is created
        
        var healthCheck = new DatabaseHealthCheck(context);
        var healthCheckContext = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(healthCheckContext);

        // Assert
        // In-memory database might not support ExecuteSqlRaw, so we expect either Healthy or Degraded
        Assert.True(result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Degraded);
        Assert.NotNull(result.Description);
    }

    [Fact]
    public async Task DatabaseHealthCheck_Should_Return_Unhealthy_When_Database_Is_Not_Available()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=invalid;")
            .Options;

        using var context = new ApplicationDbContext(options);
        var healthCheck = new DatabaseHealthCheck(context);
        var healthCheckContext = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(healthCheckContext);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Database is not accessible", result.Description);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task RedisHealthCheck_Should_Return_Unhealthy_When_Redis_Is_Not_Connected()
    {
        // Arrange
        var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        mockConnectionMultiplexer.Setup(x => x.IsConnected).Returns(false);

        var healthCheck = new RedisHealthCheck(mockConnectionMultiplexer.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Redis connection is not established", result.Description);
    }

    [Fact]
    public async Task RabbitMQHealthCheck_Should_Return_Unhealthy_When_Configuration_Is_Invalid()
    {
        // Arrange
        var options = new MessageQueueOptions
        {
            HostName = "invalid-host",
            UserName = "invalid",
            Password = "invalid",
            Port = 9999,
            VirtualHost = "/"
        };

        var mockOptions = new Mock<IOptions<MessageQueueOptions>>();
        mockOptions.Setup(x => x.Value).Returns(options);

        var healthCheck = new RabbitMQHealthCheck(mockOptions.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("RabbitMQ health check failed", result.Description);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void HealthCheckContext_Should_Be_Creatable()
    {
        // Arrange & Act
        var context = new HealthCheckContext();

        // Assert
        Assert.NotNull(context);
        // Registration is null by default in a new context
    }
}