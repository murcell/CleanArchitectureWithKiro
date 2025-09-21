using CleanArchitecture.WebAPI.Services;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for handling correlation IDs
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
    {
        var correlationId = GetOrGenerateCorrelationId(context);
        
        // Set correlation ID in service
        correlationIdService.SetCorrelationId(correlationId);
        
        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;
        
        // Add correlation ID to logging context
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogDebug("Processing request with correlation ID: {CorrelationId}", correlationId);
            
            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID if not provided
        return Guid.NewGuid().ToString();
    }
}