using System.Diagnostics;
using System.Text;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with performance metrics
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        // Log request
        await LogRequestAsync(context, correlationId);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request failed for {Method} {Path} - CorrelationId: {CorrelationId} - Duration: {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        
        var requestLog = new StringBuilder();
        requestLog.AppendLine($"HTTP Request - CorrelationId: {correlationId}");
        requestLog.AppendLine($"Method: {request.Method}");
        requestLog.AppendLine($"Path: {request.Path}");
        requestLog.AppendLine($"QueryString: {request.QueryString}");
        requestLog.AppendLine($"Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}"))}");

        // Log request body for POST/PUT requests (be careful with sensitive data)
        if (request.Method == "POST" || request.Method == "PUT")
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;

            // Don't log sensitive information
            if (!ContainsSensitiveData(request.Path))
            {
                requestLog.AppendLine($"Body: {bodyAsText}");
            }
            else
            {
                requestLog.AppendLine("Body: [SENSITIVE DATA HIDDEN]");
            }
        }

        _logger.LogInformation(requestLog.ToString());
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long durationMs)
    {
        var response = context.Response;
        
        var responseLog = new StringBuilder();
        responseLog.AppendLine($"HTTP Response - CorrelationId: {correlationId}");
        responseLog.AppendLine($"StatusCode: {response.StatusCode}");
        responseLog.AppendLine($"Duration: {durationMs}ms");
        responseLog.AppendLine($"Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}"))}");

        // Log response body for non-successful responses or if explicitly enabled
        if (response.StatusCode >= 400 || _logger.IsEnabled(LogLevel.Debug))
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            responseLog.AppendLine($"Body: {bodyAsText}");
        }

        // Log with appropriate level based on status code
        var logLevel = response.StatusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, responseLog.ToString());

        // Log performance metrics
        if (durationMs > 1000) // Log slow requests (> 1 second)
        {
            _logger.LogWarning("Slow request detected - {Method} {Path} took {Duration}ms - CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                durationMs,
                correlationId);
        }
    }

    private static bool ContainsSensitiveData(string path)
    {
        var sensitiveEndpoints = new[] { "/auth", "/login", "/password", "/token" };
        return sensitiveEndpoints.Any(endpoint => path.Contains(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to register the logging middleware
/// </summary>
public static class LoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingMiddleware>();
    }
}