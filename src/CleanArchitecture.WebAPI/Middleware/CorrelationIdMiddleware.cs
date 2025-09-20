namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for handling correlation IDs across requests
/// Ensures each request has a unique correlation ID for tracking
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

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add correlation ID to the current context
        context.Items["CorrelationId"] = correlationId;
        
        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        
        // Add correlation ID to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            var id = correlationId.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        // Try to get from query parameters (useful for debugging)
        if (context.Request.Query.TryGetValue("correlationId", out var queryCorrelationId))
        {
            var id = queryCorrelationId.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension method to register the correlation ID middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

/// <summary>
/// Service for accessing correlation ID in other parts of the application
/// </summary>
public interface ICorrelationIdService
{
    string GetCorrelationId();
}

/// <summary>
/// Implementation of correlation ID service
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? "unknown";
    }
}