using Microsoft.Extensions.Options;
using CleanArchitecture.Application.Configuration;
using CleanArchitecture.Infrastructure.Configuration;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Helper service for accessing configuration values at runtime
/// </summary>
public interface IConfigurationHelper
{
    ApplicationOptions Application { get; }
    DatabaseOptions Database { get; }
    CacheOptions Cache { get; }
    MessageQueueOptions MessageQueue { get; }
    ApiOptions Api { get; }
    LoggingOptions Logging { get; }
    bool IsProduction { get; }
    bool IsDevelopment { get; }
    bool IsStaging { get; }
    string Environment { get; }
}

/// <summary>
/// Implementation of configuration helper service
/// </summary>
public class ConfigurationHelper : IConfigurationHelper
{
    private readonly IWebHostEnvironment _environment;

    public ConfigurationHelper(
        IOptions<ApplicationOptions> applicationOptions,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<CacheOptions> cacheOptions,
        IOptions<MessageQueueOptions> messageQueueOptions,
        IOptions<ApiOptions> apiOptions,
        IOptions<LoggingOptions> loggingOptions,
        IWebHostEnvironment environment)
    {
        Application = applicationOptions.Value;
        Database = databaseOptions.Value;
        Cache = cacheOptions.Value;
        MessageQueue = messageQueueOptions.Value;
        Api = apiOptions.Value;
        Logging = loggingOptions.Value;
        _environment = environment;
    }

    public ApplicationOptions Application { get; }
    public DatabaseOptions Database { get; }
    public CacheOptions Cache { get; }
    public MessageQueueOptions MessageQueue { get; }
    public ApiOptions Api { get; }
    public LoggingOptions Logging { get; }

    public bool IsProduction => _environment.IsProduction();
    public bool IsDevelopment => _environment.IsDevelopment();
    public bool IsStaging => _environment.IsStaging();
    public string Environment => _environment.EnvironmentName;
}