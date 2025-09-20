namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Configuration options for API settings
/// </summary>
public class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>
    /// API title for documentation
    /// </summary>
    public string Title { get; set; } = "Clean Architecture API";

    /// <summary>
    /// API description for documentation
    /// </summary>
    public string Description { get; set; } = "A comprehensive Clean Architecture implementation with .NET 9";

    /// <summary>
    /// Contact information for API documentation
    /// </summary>
    public ContactInfo Contact { get; set; } = new();

    /// <summary>
    /// License information for API documentation
    /// </summary>
    public LicenseInfo License { get; set; } = new();

    /// <summary>
    /// Enable API versioning
    /// </summary>
    public bool EnableVersioning { get; set; } = true;

    /// <summary>
    /// Default API version
    /// </summary>
    public string DefaultVersion { get; set; } = "1.0";

    /// <summary>
    /// Enable Swagger documentation
    /// </summary>
    public bool EnableSwagger { get; set; } = true;

    /// <summary>
    /// Enable CORS
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Allowed CORS origins
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = false;

    /// <summary>
    /// Rate limit requests per minute
    /// </summary>
    public int RateLimitRequestsPerMinute { get; set; } = 100;
}

/// <summary>
/// Contact information for API documentation
/// </summary>
public class ContactInfo
{
    public string Name { get; set; } = "Clean Architecture Team";
    public string Email { get; set; } = "support@cleanarchitecture.com";
    public string Url { get; set; } = "https://github.com/cleanarchitecture/api";
}

/// <summary>
/// License information for API documentation
/// </summary>
public class LicenseInfo
{
    public string Name { get; set; } = "MIT License";
    public string Url { get; set; } = "https://opensource.org/licenses/MIT";
}