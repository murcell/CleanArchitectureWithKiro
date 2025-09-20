namespace CleanArchitecture.WebAPI.Configuration;

public class LoggingOptions
{
    public const string SectionName = "Logging";

    public bool EnableStructuredLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = true;
    public bool EnableCorrelationId { get; set; } = true;
    public bool EnableRequestResponseLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public FileLoggingOptions File { get; set; } = new();
    public ConsoleLoggingOptions Console { get; set; } = new();
    public SeqLoggingOptions Seq { get; set; } = new();
    public List<string> ExcludedPaths { get; set; } = new() { "/health", "/metrics" };
    public int PerformanceThresholdMs { get; set; } = 500;
}

public class FileLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "logs/app-.txt";
    public string RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; } = 31;
    public long FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public bool RollOnFileSizeLimit { get; set; } = true;
}

public class ConsoleLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public string OutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";
}

public class SeqLoggingOptions
{
    public bool Enabled { get; set; } = false;
    public string ServerUrl { get; set; } = "http://localhost:5341";
    public string? ApiKey { get; set; }
}