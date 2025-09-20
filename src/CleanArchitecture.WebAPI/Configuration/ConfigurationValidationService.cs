using Microsoft.Extensions.Options;
using CleanArchitecture.Application.Configuration;
using CleanArchitecture.Infrastructure.Configuration;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Service for validating all configuration options at startup
/// </summary>
public class ConfigurationValidationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationValidationService> _logger;

    public ConfigurationValidationService(
        IServiceProvider serviceProvider,
        ILogger<ConfigurationValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting configuration validation...");

        try
        {
            // Validate all options by forcing their creation
            ValidateOptions<ApplicationOptions>();
            ValidateOptions<DatabaseOptions>();
            ValidateOptions<CacheOptions>();
            ValidateOptions<MessageQueueOptions>();
            ValidateOptions<ApiOptions>();
            ValidateOptions<LoggingOptions>();

            _logger.LogInformation("Configuration validation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void ValidateOptions<T>() where T : class
    {
        try
        {
            var options = _serviceProvider.GetRequiredService<IOptions<T>>();
            var value = options.Value; // This will trigger validation
            _logger.LogDebug("Configuration validation passed for {OptionsType}", typeof(T).Name);
        }
        catch (OptionsValidationException ex)
        {
            _logger.LogError("Configuration validation failed for {OptionsType}: {Failures}", 
                typeof(T).Name, string.Join(", ", ex.Failures));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during configuration validation for {OptionsType}", typeof(T).Name);
            throw;
        }
    }
}