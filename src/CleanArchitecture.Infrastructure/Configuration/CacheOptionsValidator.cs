using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Configuration;

/// <summary>
/// Validator for CacheOptions configuration
/// </summary>
public class CacheOptionsValidator : IValidateOptions<CacheOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        var failures = new List<string>();

        // Validate default expiration
        if (options.DefaultExpiration <= TimeSpan.Zero)
        {
            failures.Add("DefaultExpiration must be greater than zero");
        }

        // Validate sliding expiration
        if (options.SlidingExpiration <= TimeSpan.Zero)
        {
            failures.Add("SlidingExpiration must be greater than zero");
        }

        // Validate key prefix
        if (string.IsNullOrWhiteSpace(options.KeyPrefix))
        {
            failures.Add("KeyPrefix cannot be null or empty");
        }

        // Validate instance name
        if (string.IsNullOrWhiteSpace(options.InstanceName))
        {
            failures.Add("InstanceName cannot be null or empty");
        }

        // Validate max cache size
        if (options.MaxCacheSizeMB <= 0)
        {
            failures.Add("MaxCacheSizeMB must be greater than 0");
        }

        // Validate key separator
        if (string.IsNullOrEmpty(options.KeySeparator))
        {
            failures.Add("KeySeparator cannot be null or empty");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}