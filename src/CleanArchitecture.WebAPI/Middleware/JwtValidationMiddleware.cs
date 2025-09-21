using CleanArchitecture.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Middleware for validating JWT tokens and checking blacklist
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(
        RequestDelegate next, 
        IServiceProvider serviceProvider,
        ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractTokenFromRequest(context);
        
        if (!string.IsNullOrEmpty(token))
        {
            using var scope = _serviceProvider.CreateScope();
            var blacklistService = scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();
            
            if (await blacklistService.IsTokenBlacklistedAsync(token))
            {
                _logger.LogWarning("Blacklisted token access attempt from {IP}", 
                    context.Connection.RemoteIpAddress);
                
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token has been revoked");
                return;
            }
        }

        await _next(context);
    }

    private string? ExtractTokenFromRequest(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }
}