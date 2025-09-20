using Microsoft.Extensions.Options;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Validator for LoggingOptions configuration
/// </summary>
public class LoggingOptionsValidator : IValidateOptions<LoggingOptions>
{
    public ValidateOptionsResult Validate(string? name, LoggingOptions options)
    {
        var failures = new List<string>();

        // Validate performance threshold
        if (options.PerformanceThresholdMs <= 0)
        {
            failures.Add("PerformanceThresholdMs must be greater than 0");
        }

        // Validate log level
        if (!string.IsNullOrWhiteSpace(options.LogLevel))
        {
            var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None" };
            if (!validLogLevels.Contains(options.LogLevel, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add($"Invalid LogLevel value '{options.LogLevel}'. Valid values are: {string.Join(", ", validLogLevels)}");
            }
        }

        // Validate file logging configuration
        if (options.File.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.File.Path))
            {
                failures.Add("File.Path must be specified when File.Enabled is true");
            }
            else
            {
                try
                {
                    var directory = Path.GetDirectoryName(options.File.Path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        // Try to create the directory to validate the path
                        Directory.CreateDirectory(directory);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"Invalid File.Path: {ex.Message}");
                }
            }

            if (options.File.FileSizeLimitBytes <= 0)
            {
                failures.Add("File.FileSizeLimitBytes must be greater than 0");
            }

            if (options.File.RetainedFileCountLimit <= 0)
            {
                failures.Add("File.RetainedFileCountLimit must be greater than 0");
            }
        }

        // Validate console logging configuration
        if (options.Console.Enabled && string.IsNullOrWhiteSpace(options.Console.OutputTemplate))
        {
            failures.Add("Console.OutputTemplate must be specified when Console.Enabled is true");
        }

        // Validate Seq logging configuration
        if (options.Seq.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Seq.ServerUrl))
            {
                failures.Add("Seq.ServerUrl must be specified when Seq.Enabled is true");
            }
            else if (!Uri.TryCreate(options.Seq.ServerUrl, UriKind.Absolute, out _))
            {
                failures.Add("Seq.ServerUrl must be a valid URL");
            }
        }

        // Validate that at least one logging output is enabled
        if (!options.Console.Enabled && !options.File.Enabled)
        {
            failures.Add("At least one logging output (Console or File) must be enabled");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}