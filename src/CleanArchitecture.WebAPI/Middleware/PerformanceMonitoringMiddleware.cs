using CleanArchitecture.Application.Common.Interfaces;
using System.Diagnostics;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for monitoring API performance and collecting metrics
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            // Log performance metrics
            LogPerformanceMetrics(method, path, statusCode, duration);

            // Record metrics
            RecordMetrics(method, path, statusCode, duration);
        }
    }

    private void LogPerformanceMetrics(string method, string path, int statusCode, double duration)
    {
        var logLevel = GetLogLevel(statusCode, duration);
        
        _logger.Log(logLevel, 
            "HTTP {Method} {Path} responded {StatusCode} in {Duration:F2}ms",
            method, path, statusCode, duration);

        // Log slow requests
        if (duration > 1000) // Slower than 1 second
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} took {Duration:F2}ms",
                method, path, duration);
        }
    }

    private void RecordMetrics(string method, string path, int statusCode, double duration)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var metricsService = scope.ServiceProvider.GetService<IMetricsService>();
            
            if (metricsService != null)
            {
                var tags = new Dictionary<string, string>
                {
                    ["method"] = method,
                    ["endpoint"] = SanitizeEndpoint(path),
                    ["status_code"] = statusCode.ToString()
                };

                // Record request count
                metricsService.IncrementCounter("http_requests_total", tags);

                // Record request duration
                metricsService.RecordHistogram("http_request_duration_ms", duration, tags);

                // Record status code specific metrics
                if (statusCode >= 400)
                {
                    metricsService.IncrementCounter("http_requests_errors_total", tags);
                }

                if (statusCode >= 500)
                {
                    metricsService.IncrementCounter("http_requests_server_errors_total", tags);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording performance metrics");
        }
    }

    private static LogLevel GetLogLevel(int statusCode, double duration)
    {
        if (statusCode >= 500)
            return LogLevel.Error;
        
        if (statusCode >= 400)
            return LogLevel.Warning;
        
        if (duration > 5000) // Slower than 5 seconds
            return LogLevel.Warning;
        
        return LogLevel.Information;
    }

    private static string SanitizeEndpoint(string path)
    {
        // Replace dynamic segments with placeholders for better metric grouping
        // Example: /api/v1/users/123 -> /api/v1/users/{id}
        
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var sanitized = new List<string>();

        foreach (var segment in segments)
        {
            // Check if segment looks like an ID (numeric or GUID)
            if (int.TryParse(segment, out _) || Guid.TryParse(segment, out _))
            {
                sanitized.Add("{id}");
            }
            else
            {
                sanitized.Add(segment);
            }
        }

        return "/" + string.Join("/", sanitized);
    }
}