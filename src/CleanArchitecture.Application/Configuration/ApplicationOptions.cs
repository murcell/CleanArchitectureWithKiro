namespace CleanArchitecture.Application.Configuration;

/// <summary>
/// Configuration options for Application layer settings
/// </summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>
    /// Enable performance monitoring for MediatR pipeline
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Performance threshold in milliseconds for logging slow operations
    /// </summary>
    public int PerformanceThresholdMs { get; set; } = 500;

    /// <summary>
    /// Enable validation caching
    /// </summary>
    public bool EnableValidationCaching { get; set; } = true;

    /// <summary>
    /// Validation cache expiration in minutes
    /// </summary>
    public int ValidationCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Enable detailed validation errors
    /// </summary>
    public bool EnableDetailedValidationErrors { get; set; } = true;

    /// <summary>
    /// Maximum allowed file upload size in MB
    /// </summary>
    public int MaxFileUploadSizeMB { get; set; } = 10;

    /// <summary>
    /// Allowed file extensions for uploads
    /// </summary>
    public string[] AllowedFileExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };

    /// <summary>
    /// Default page size for paginated queries
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Maximum page size for paginated queries
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
}