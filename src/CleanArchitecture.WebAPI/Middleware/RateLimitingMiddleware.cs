using System.Collections.Concurrent;
using System.Net;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for API rate limiting
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private static readonly ConcurrentDictionary<string, ClientRateLimit> _clients = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = configuration.GetSection("RateLimit").Get<RateLimitOptions>() ?? new RateLimitOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        var key = $"{clientId}:{endpoint}";

        var rateLimit = _clients.GetOrAdd(key, _ => new ClientRateLimit
        {
            RequestCount = 0,
            WindowStart = DateTime.UtcNow
        });

        lock (rateLimit)
        {
            var now = DateTime.UtcNow;
            
            // Reset window if expired
            if (now - rateLimit.WindowStart > TimeSpan.FromMinutes(_options.WindowSizeInMinutes))
            {
                rateLimit.RequestCount = 0;
                rateLimit.WindowStart = now;
            }

            rateLimit.RequestCount++;

            // Check if limit exceeded
            if (rateLimit.RequestCount > _options.MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. Count: {Count}",
                    clientId, endpoint, rateLimit.RequestCount);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = (_options.WindowSizeInMinutes * 60).ToString();
                
                return;
            }
        }

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = 
            Math.Max(0, _options.MaxRequests - rateLimit.RequestCount).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = 
            ((DateTimeOffset)(rateLimit.WindowStart.AddMinutes(_options.WindowSizeInMinutes))).ToUnixTimeSeconds().ToString();

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User?.FindFirst("sub")?.Value ?? 
                    context.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Try to get API key
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
        {
            return $"apikey:{apiKey[..8]}"; // Use first 8 chars for identification
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        return $"{context.Request.Method}:{context.Request.Path}";
    }
}

public class RateLimitOptions
{
    public int MaxRequests { get; set; } = 100;
    public int WindowSizeInMinutes { get; set; } = 1;
}

public class ClientRateLimit
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
}