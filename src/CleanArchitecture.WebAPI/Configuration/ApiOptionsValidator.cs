using Microsoft.Extensions.Options;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Validator for ApiOptions configuration
/// </summary>
public class ApiOptionsValidator : IValidateOptions<ApiOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiOptions options)
    {
        var failures = new List<string>();

        // Validate title
        if (string.IsNullOrWhiteSpace(options.Title))
        {
            failures.Add("Title cannot be null or empty");
        }

        // Validate description
        if (string.IsNullOrWhiteSpace(options.Description))
        {
            failures.Add("Description cannot be null or empty");
        }

        // Validate default version
        if (string.IsNullOrWhiteSpace(options.DefaultVersion))
        {
            failures.Add("DefaultVersion cannot be null or empty");
        }

        // Validate version format
        if (!string.IsNullOrWhiteSpace(options.DefaultVersion) && 
            !System.Text.RegularExpressions.Regex.IsMatch(options.DefaultVersion, @"^\d+\.\d+$"))
        {
            failures.Add("DefaultVersion must be in format 'major.minor' (e.g., '1.0')");
        }

        // Validate rate limiting settings
        if (options.EnableRateLimiting && options.RateLimitRequestsPerMinute <= 0)
        {
            failures.Add("RateLimitRequestsPerMinute must be greater than 0 when rate limiting is enabled");
        }

        // Validate contact information
        if (options.Contact != null)
        {
            if (!string.IsNullOrWhiteSpace(options.Contact.Email) && 
                !System.Text.RegularExpressions.Regex.IsMatch(options.Contact.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                failures.Add("Contact.Email must be a valid email address");
            }

            if (!string.IsNullOrWhiteSpace(options.Contact.Url) && 
                !Uri.TryCreate(options.Contact.Url, UriKind.Absolute, out _))
            {
                failures.Add("Contact.Url must be a valid URL");
            }
        }

        // Validate license information
        if (options.License != null && 
            !string.IsNullOrWhiteSpace(options.License.Url) && 
            !Uri.TryCreate(options.License.Url, UriKind.Absolute, out _))
        {
            failures.Add("License.Url must be a valid URL");
        }

        // Validate CORS origins
        if (options.AllowedOrigins?.Any() == true)
        {
            foreach (var origin in options.AllowedOrigins)
            {
                if (string.IsNullOrWhiteSpace(origin))
                {
                    failures.Add("AllowedOrigins cannot contain null or empty values");
                    break;
                }

                if (origin != "*" && !Uri.TryCreate(origin, UriKind.Absolute, out _))
                {
                    failures.Add($"Invalid CORS origin: {origin}. Must be '*' or a valid URL");
                }
            }
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}