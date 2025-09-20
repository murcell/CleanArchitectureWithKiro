using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;

namespace CleanArchitecture.WebAPI.Configuration;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var loggingOptions = builder.Configuration
            .GetSection(LoggingOptions.SectionName)
            .Get<LoggingOptions>() ?? new LoggingOptions();

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentName()
                .Enrich.WithCorrelationId()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Application", "CleanArchitecture")
                .Enrich.WithProperty("Version", GetApplicationVersion())
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"));

            // Configure minimum level
            var logLevel = Enum.TryParse<LogEventLevel>(loggingOptions.LogLevel, true, out var level) 
                ? level 
                : LogEventLevel.Information;
            configuration.MinimumLevel.Is(logLevel);

            // Override specific namespaces
            configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("System", LogEventLevel.Warning);

            // Configure sinks
            ConfigureSinks(configuration, loggingOptions, context.HostingEnvironment);
        });
    }

    private static void ConfigureSinks(LoggerConfiguration configuration, LoggingOptions options, IHostEnvironment environment)
    {
        // Console sink
        if (options.Console.Enabled)
        {
            configuration.WriteTo.Console(
                outputTemplate: options.Console.OutputTemplate,
                restrictedToMinimumLevel: LogEventLevel.Information);
        }

        // File sink
        if (options.File.Enabled)
        {
            var rollingInterval = Enum.TryParse<RollingInterval>(options.File.RollingInterval, true, out var interval)
                ? interval
                : RollingInterval.Day;

            configuration.WriteTo.File(
                path: options.File.Path,
                rollingInterval: rollingInterval,
                retainedFileCountLimit: options.File.RetainedFileCountLimit,
                fileSizeLimitBytes: options.File.FileSizeLimitBytes,
                rollOnFileSizeLimit: options.File.RollOnFileSizeLimit,
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // Seq sink (for structured logging visualization)
        if (options.Seq.Enabled && !string.IsNullOrEmpty(options.Seq.ServerUrl))
        {
            configuration.WriteTo.Seq(
                serverUrl: options.Seq.ServerUrl,
                apiKey: options.Seq.ApiKey,
                restrictedToMinimumLevel: LogEventLevel.Information);
        }

        // Development specific sinks
        if (environment.IsDevelopment())
        {
            configuration.WriteTo.Debug(
                restrictedToMinimumLevel: LogEventLevel.Debug,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }
    }

    private static string GetApplicationVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }
}