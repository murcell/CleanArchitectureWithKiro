using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.WebAPI.Tests.Common;

/// <summary>
/// Helper class for managing test configuration
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Creates a test configuration with default test settings
    /// </summary>
    public static IConfiguration CreateTestConfiguration(Dictionary<string, string?>? additionalSettings = null)
    {
        var defaultSettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=CleanArchitectureDB_Test;Trusted_Connection=true;TrustServerCertificate=true;",
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["Database:EnableAutomaticMigrations"] = "true",
            ["Database:EnableSensitiveDataLogging"] = "true",
            ["Database:EnableDetailedErrors"] = "true",
            ["Cache:EnableDistributedCache"] = "false",
            ["Cache:DefaultExpiration"] = "00:05:00",
            ["RabbitMQ:HostName"] = "localhost",
            ["RabbitMQ:UserName"] = "guest",
            ["RabbitMQ:Password"] = "guest",
            ["RabbitMQ:Port"] = "5672",
            ["RabbitMQ:EnableAutomaticRecovery"] = "false",
            ["Application:EnablePerformanceMonitoring"] = "false",
            ["Api:EnableSwagger"] = "false",
            ["Api:EnableCors"] = "false",
            ["Logging:EnableStructuredLogging"] = "false",
            ["Logging:EnablePerformanceLogging"] = "false",
            ["Logging:LogLevel"] = "Warning"
        };

        if (additionalSettings != null)
        {
            foreach (var setting in additionalSettings)
            {
                defaultSettings[setting.Key] = setting.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaultSettings)
            .Build();
    }

    /// <summary>
    /// Creates configuration for container-based tests
    /// </summary>
    public static IConfiguration CreateContainerTestConfiguration(
        string sqlConnectionString,
        string redisConnectionString,
        string rabbitMqHost,
        int rabbitMqPort,
        string rabbitMqUsername,
        string rabbitMqPassword)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = sqlConnectionString,
            ["ConnectionStrings:Redis"] = redisConnectionString,
            ["RabbitMQ:HostName"] = rabbitMqHost,
            ["RabbitMQ:Port"] = rabbitMqPort.ToString(),
            ["RabbitMQ:UserName"] = rabbitMqUsername,
            ["RabbitMQ:Password"] = rabbitMqPassword,
            ["Database:EnableAutomaticMigrations"] = "true",
            ["Cache:EnableDistributedCache"] = "true"
        };

        return CreateTestConfiguration(settings);
    }

    /// <summary>
    /// Gets test environment variables
    /// </summary>
    public static class Environment
    {
        public static bool UseRealContainers => 
            System.Environment.GetEnvironmentVariable("USE_REAL_CONTAINERS")?.ToLowerInvariant() == "true";

        public static bool RunSlowTests => 
            System.Environment.GetEnvironmentVariable("RUN_SLOW_TESTS")?.ToLowerInvariant() == "true";

        public static string TestDatabaseName => 
            System.Environment.GetEnvironmentVariable("TEST_DATABASE_NAME") ?? "CleanArchitectureDB_Test";

        public static int TestTimeoutSeconds => 
            int.TryParse(System.Environment.GetEnvironmentVariable("TEST_TIMEOUT_SECONDS"), out var timeout) 
                ? timeout : 30;
    }
}