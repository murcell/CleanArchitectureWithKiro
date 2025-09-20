using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Configuration;

/// <summary>
/// Validator for DatabaseOptions configuration
/// </summary>
public class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        // Validate command timeout
        if (options.CommandTimeout <= 0)
        {
            failures.Add("CommandTimeout must be greater than 0");
        }

        // Validate retry settings
        if (options.MaxRetryCount < 0)
        {
            failures.Add("MaxRetryCount cannot be negative");
        }

        if (options.MaxRetryDelay <= 0)
        {
            failures.Add("MaxRetryDelay must be greater than 0");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}