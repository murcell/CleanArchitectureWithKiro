using Microsoft.Extensions.Options;

namespace CleanArchitecture.Application.Configuration;

/// <summary>
/// Validator for ApplicationOptions configuration
/// </summary>
public class ApplicationOptionsValidator : IValidateOptions<ApplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationOptions options)
    {
        var failures = new List<string>();

        // Validate performance threshold
        if (options.PerformanceThresholdMs <= 0)
        {
            failures.Add("PerformanceThresholdMs must be greater than 0");
        }

        // Validate validation cache expiration
        if (options.ValidationCacheExpirationMinutes <= 0)
        {
            failures.Add("ValidationCacheExpirationMinutes must be greater than 0");
        }

        // Validate file upload size
        if (options.MaxFileUploadSizeMB <= 0)
        {
            failures.Add("MaxFileUploadSizeMB must be greater than 0");
        }

        // Validate page sizes
        if (options.DefaultPageSize <= 0)
        {
            failures.Add("DefaultPageSize must be greater than 0");
        }

        if (options.MaxPageSize <= 0)
        {
            failures.Add("MaxPageSize must be greater than 0");
        }

        if (options.DefaultPageSize > options.MaxPageSize)
        {
            failures.Add("DefaultPageSize cannot be greater than MaxPageSize");
        }

        // Validate file extensions
        if (options.AllowedFileExtensions?.Any() == true)
        {
            foreach (var extension in options.AllowedFileExtensions)
            {
                if (string.IsNullOrWhiteSpace(extension) || !extension.StartsWith('.'))
                {
                    failures.Add($"Invalid file extension: {extension}. Extensions must start with '.'");
                }
            }
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}