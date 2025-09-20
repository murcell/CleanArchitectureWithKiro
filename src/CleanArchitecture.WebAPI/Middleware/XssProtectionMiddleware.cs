using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware to protect against XSS attacks by sanitizing request content
/// </summary>
public class XssProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<XssProtectionMiddleware> _logger;
    
    private static readonly Regex XssPattern = new(
        @"<\s*script[^>]*>.*?<\s*/\s*script\s*>|javascript:|vbscript:|onload\s*=|onerror\s*=|onclick\s*=|onmouseover\s*=|<\s*iframe|<\s*object|<\s*embed|<\s*link|<\s*meta|data:text/html",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] DangerousHeaders = 
    {
        "X-Forwarded-Host", "X-Original-URL", "X-Rewrite-URL"
    };

    public XssProtectionMiddleware(RequestDelegate next, ILogger<XssProtectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context);
        
        // Check for dangerous headers
        if (HasDangerousHeaders(context))
        {
            _logger.LogWarning("Request blocked due to dangerous headers from IP: {RemoteIpAddress}", 
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad Request: Dangerous headers detected");
            return;
        }

        // Check query parameters
        if (HasXssInQueryParameters(context))
        {
            _logger.LogWarning("XSS attempt detected in query parameters from IP: {RemoteIpAddress}", 
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad Request: Potentially dangerous content detected");
            return;
        }

        // Check request body for POST/PUT requests
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            context.Request.EnableBuffering();
            
            var body = await ReadRequestBodyAsync(context.Request);
            if (!string.IsNullOrEmpty(body) && ContainsXss(body))
            {
                _logger.LogWarning("XSS attempt detected in request body from IP: {RemoteIpAddress}", 
                    context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Bad Request: Potentially dangerous content detected");
                return;
            }
            
            // Reset the request body stream position
            context.Request.Body.Position = 0;
        }

        await _next(context);
    }

    private static void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        
        // XSS Protection
        response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Content Type Options
        response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Frame Options
        response.Headers["X-Frame-Options"] = "DENY";
        
        // Content Security Policy
        response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";
        
        // Referrer Policy
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Permissions Policy
        response.Headers["Permissions-Policy"] = 
            "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=()";
    }

    private static bool HasDangerousHeaders(HttpContext context)
    {
        return DangerousHeaders.Any(header => context.Request.Headers.ContainsKey(header));
    }

    private static bool HasXssInQueryParameters(HttpContext context)
    {
        foreach (var param in context.Request.Query)
        {
            if (ContainsXss(param.Key) || param.Value.Any(ContainsXss))
            {
                return true;
            }
        }
        return false;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static bool ContainsXss(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return XssPattern.IsMatch(input);
    }
}

/// <summary>
/// Extension method to register the XSS protection middleware
/// </summary>
public static class XssProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseXssProtection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<XssProtectionMiddleware>();
    }
}