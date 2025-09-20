using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Configuration;

/// <summary>
/// Validator for MessageQueueOptions configuration
/// </summary>
public class MessageQueueOptionsValidator : IValidateOptions<MessageQueueOptions>
{
    public ValidateOptionsResult Validate(string? name, MessageQueueOptions options)
    {
        var failures = new List<string>();

        // Validate hostname
        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            failures.Add("HostName cannot be null or empty");
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            failures.Add("UserName cannot be null or empty");
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add("Password cannot be null or empty");
        }

        // Validate port
        if (options.Port <= 0 || options.Port > 65535)
        {
            failures.Add("Port must be between 1 and 65535");
        }

        // Validate virtual host
        if (string.IsNullOrWhiteSpace(options.VirtualHost))
        {
            failures.Add("VirtualHost cannot be null or empty");
        }

        // Validate retry settings
        if (options.MaxRetryAttempts < 0)
        {
            failures.Add("MaxRetryAttempts cannot be negative");
        }

        if (options.RetryDelay <= TimeSpan.Zero)
        {
            failures.Add("RetryDelay must be greater than zero");
        }

        // Validate connection timeout
        if (options.ConnectionTimeoutSeconds <= 0)
        {
            failures.Add("ConnectionTimeoutSeconds must be greater than 0");
        }

        // Validate network recovery interval
        if (options.NetworkRecoveryIntervalSeconds <= 0)
        {
            failures.Add("NetworkRecoveryIntervalSeconds must be greater than 0");
        }

        // Validate prefetch count
        if (options.PrefetchCount == 0)
        {
            failures.Add("PrefetchCount must be greater than 0");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}