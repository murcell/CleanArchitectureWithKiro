using CleanArchitecture.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CleanArchitecture.Infrastructure.Security;

/// <summary>
/// Middleware for API key authentication
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyQueryParameter = "apikey";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyRepository apiKeyRepository)
    {
        // Skip API key authentication if already authenticated or for certain paths
        if (context.User.Identity?.IsAuthenticated == true || ShouldSkipAuthentication(context))
        {
            await _next(context);
            return;
        }

        var apiKey = ExtractApiKey(context);
        if (string.IsNullOrEmpty(apiKey))
        {
            await _next(context);
            return;
        }

        try
        {
            var isAuthenticated = await AuthenticateApiKeyAsync(context, apiKey, apiKeyRepository);
            if (!isAuthenticated)
            {
                _logger.LogWarning("Invalid API key attempted from {RemoteIpAddress}", context.Connection.RemoteIpAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API key authentication");
        }

        await _next(context);
    }

    private string? ExtractApiKey(HttpContext context)
    {
        // Try header first
        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // Try query parameter
        if (context.Request.Query.TryGetValue(ApiKeyQueryParameter, out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        return null;
    }

    private async Task<bool> AuthenticateApiKeyAsync(HttpContext context, string apiKey, IApiKeyRepository apiKeyRepository)
    {
        // Validate API key format (should start with "ca_")
        if (!apiKey.StartsWith("ca_"))
        {
            return false;
        }

        // Extract the actual key (remove "ca_" prefix)
        var actualKey = apiKey.Substring(3);
        
        // Hash the key for lookup
        var keyHash = HashApiKey(actualKey);
        
        // Find the API key in the database
        var apiKeyEntity = await apiKeyRepository.GetByKeyHashAsync(keyHash);
        
        if (apiKeyEntity == null || !apiKeyEntity.IsValid())
        {
            return false;
        }

        // Record usage
        apiKeyEntity.RecordUsage();
        await apiKeyRepository.UpdateAsync(apiKeyEntity);

        // Create claims for the API key
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKeyEntity.Id.ToString()),
            new(ClaimTypes.Name, apiKeyEntity.Name),
            new(ClaimTypes.AuthenticationMethod, "ApiKey"),
            new("api_key_id", apiKeyEntity.Id.ToString()),
            new("api_key_name", apiKeyEntity.Name)
        };

        // Add scopes as claims
        foreach (var scope in apiKeyEntity.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        // Add user claims if API key is associated with a user
        if (apiKeyEntity.UserId.HasValue && apiKeyEntity.User != null)
        {
            claims.Add(new Claim("user_id", apiKeyEntity.UserId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, apiKeyEntity.User.Role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        _logger.LogInformation("API key authentication successful for key: {KeyPrefix}", apiKeyEntity.KeyPrefix);
        return true;
    }

    private bool ShouldSkipAuthentication(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        // Skip authentication for health checks, swagger, and other public endpoints
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh"
        };

        return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
    }

    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashedBytes);
    }
}