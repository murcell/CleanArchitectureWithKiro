namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for adding security headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Remove server information
        headers.Remove("Server");

        // X-Content-Type-Options
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // X-Frame-Options
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            headers["X-Frame-Options"] = "DENY";
        }

        // X-XSS-Protection
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Referrer-Policy
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        // Content-Security-Policy
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'";
        }

        // Strict-Transport-Security (only for HTTPS)
        if (context.Request.IsHttps && !headers.ContainsKey("Strict-Transport-Security"))
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Permissions-Policy
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = 
                "camera=(), " +
                "microphone=(), " +
                "geolocation=(), " +
                "payment=(), " +
                "usb=(), " +
                "magnetometer=(), " +
                "accelerometer=(), " +
                "gyroscope=()";
        }

        _logger.LogDebug("Security headers added to response");
    }
}