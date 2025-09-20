using Serilog;
using SerilogTimings;
using System.Diagnostics;

namespace CleanArchitecture.WebAPI.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private readonly int _performanceThresholdMs;

    public PerformanceLoggingMiddleware(
        RequestDelegate next, 
        ILogger<PerformanceLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _performanceThresholdMs = configuration.GetValue<int>("Logging:PerformanceThresholdMs", 500);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.TraceIdentifier;

        using var operation = Operation.Begin("HTTP {Method} {Path}", 
            context.Request.Method, 
            context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            var logLevel = elapsedMs > _performanceThresholdMs 
                ? LogLevel.Warning 
                : LogLevel.Information;

            _logger.Log(logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs,
                correlationId);

            // Log slow requests with additional details
            if (elapsedMs > _performanceThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {ElapsedMs}ms (threshold: {ThresholdMs}ms). " +
                    "StatusCode: {StatusCode}, ContentLength: {ContentLength}, UserAgent: {UserAgent}, CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs,
                    _performanceThresholdMs,
                    context.Response.StatusCode,
                    context.Response.ContentLength,
                    context.Request.Headers.UserAgent.ToString(),
                    correlationId);
            }

            operation.Complete();
        }
    }
}