namespace CleanArchitecture.Infrastructure.Configuration;

/// <summary>
/// Configuration options for database connections and settings
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Default database connection string
    /// </summary>
    public string DefaultConnection { get; set; } = string.Empty;

    /// <summary>
    /// Enable automatic migrations in development
    /// </summary>
    public bool EnableAutomaticMigrations { get; set; } = true;

    /// <summary>
    /// Enable sensitive data logging (development only)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Enable detailed errors (development only)
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum retry delay in seconds
    /// </summary>
    public int MaxRetryDelay { get; set; } = 30;
}