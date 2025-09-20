using Microsoft.Extensions.Options;
using CleanArchitecture.Application.Configuration;
using CleanArchitecture.Infrastructure.Configuration;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Extension methods for configuration validation
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates all configuration sections at startup
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ValidateConfigurationOnStartup(this IServiceCollection services, IConfiguration configuration)
    {
        // Validate required connection strings
        ValidateConnectionStrings(configuration);

        // Validate required configuration sections exist
        ValidateRequiredSections(configuration);

        // Validate environment-specific settings
        ValidateEnvironmentSettings(configuration);

        return services;
    }

    /// <summary>
    /// Validates that required connection strings are present
    /// </summary>
    /// <param name="configuration">The configuration</param>
    private static void ValidateConnectionStrings(IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        
        var defaultConnection = connectionStrings["DefaultConnection"];
        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection connection string is required");
        }

        var redisConnection = connectionStrings["Redis"];
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException("Redis connection string is required");
        }
    }

    /// <summary>
    /// Validates that required configuration sections exist
    /// </summary>
    /// <param name="configuration">The configuration</param>
    private static void ValidateRequiredSections(IConfiguration configuration)
    {
        var requiredSections = new[]
        {
            ApplicationOptions.SectionName,
            DatabaseOptions.SectionName,
            CacheOptions.SectionName,
            MessageQueueOptions.SectionName,
            ApiOptions.SectionName,
            LoggingOptions.SectionName
        };

        foreach (var sectionName in requiredSections)
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                throw new InvalidOperationException($"Required configuration section '{sectionName}' is missing");
            }
        }
    }

    /// <summary>
    /// Validates environment-specific settings
    /// </summary>
    /// <param name="configuration">The configuration</param>
    private static void ValidateEnvironmentSettings(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // Validate production-specific requirements
        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            ValidateProductionSettings(configuration);
        }

        // Validate development-specific requirements
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            ValidateDevelopmentSettings(configuration);
        }

        // Validate staging-specific requirements
        if (string.Equals(environment, "Staging", StringComparison.OrdinalIgnoreCase))
        {
            ValidateStagingSettings(configuration);
        }
    }

    /// <summary>
    /// Validates production-specific configuration requirements
    /// </summary>
    /// <param name="configuration">The configuration</param>
    private static void ValidateProductionSettings(IConfiguration configuration)
    {
        // Ensure sensitive data logging is disabled in production
        var databaseSection = configuration.GetSection(DatabaseOptions.SectionName);
        if (databaseSection.GetValue<bool>("EnableSensitiveDataLogging"))
        {
            throw new InvalidOperationException("EnableSensitiveDataLogging must be false in production");
        }

        if (databaseSection.GetValue<bool>("EnableDetailedErrors"))
        {
            throw new InvalidOperationException("EnableDetailedErrors must be false in production");
        }

        // Ensure Swagger is disabled in production
        var apiSection = configuration.GetSection(ApiOptions.SectionName);
        if (apiSection.GetValue<bool>("EnableSwagger"))
        {
            throw new InvalidOperationException("EnableSwagger should be false in production");
        }

        // Ensure rate limiting is enabled in production
        if (!apiSection.GetValue<bool>("EnableRateLimiting"))
        {
            throw new InvalidOperationException("EnableRateLimiting should be true in production");
        }
    }

    /// <summary>
    /// Validates development-specific configuration requirements
    /// </summary>
    /// <param name="configuration">The configuration</param>
    private static void ValidateDevelopmentSettings(IConfiguration configuration)
    {
        // Ensure development database is used
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!connectionString?.Contains("_Dev", StringComparison.OrdinalIgnoreCase) == true)
        {
            Console.WriteLine("Warning: Development environment should use a development database");
        }
    }

    /// <summary>
    /// Validates staging-specific configuration requirements
    /// </summary>
    /// <param name="configuration">The configuration</param>
    private static void ValidateStagingSettings(IConfiguration configuration)
    {
        // Ensure staging database is used
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!connectionString?.Contains("Staging", StringComparison.OrdinalIgnoreCase) == true)
        {
            Console.WriteLine("Warning: Staging environment should use a staging database");
        }

        // Ensure appropriate logging levels for staging
        var loggingSection = configuration.GetSection("Logging:LogLevel");
        var defaultLogLevel = loggingSection["Default"];
        if (string.Equals(defaultLogLevel, "Debug", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Warning: Debug logging level may not be appropriate for staging environment");
        }
    }

    /// <summary>
    /// Validates configuration values at runtime
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication ValidateConfigurationAtRuntime(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Force validation of all options by accessing them
            var configHelper = scope.ServiceProvider.GetRequiredService<IConfigurationHelper>();
            
            logger.LogInformation("Configuration validation completed successfully");
            logger.LogInformation("Environment: {Environment}", configHelper.Environment);
            logger.LogInformation("Database AutoMigrations: {AutoMigrations}", configHelper.Database.EnableAutomaticMigrations);
            logger.LogInformation("Cache Distributed: {DistributedCache}", configHelper.Cache.EnableDistributedCache);
            logger.LogInformation("API Swagger: {Swagger}", configHelper.Api.EnableSwagger);
            logger.LogInformation("API Rate Limiting: {RateLimit}", configHelper.Api.EnableRateLimiting);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Configuration validation failed at runtime");
            throw;
        }

        return app;
    }
}