using CleanArchitecture.Infrastructure.Security;
using CleanArchitecture.Infrastructure.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Extension methods for configuring authentication and authorization
/// </summary>
public static class AuthenticationConfiguration
{
    /// <summary>
    /// Adds JWT authentication configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Authentication");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? "CleanArchitecture";
        var audience = jwtSettings["Audience"] ?? "CleanArchitecture";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userId = context.Principal?.FindFirst("user_id")?.Value;
                    logger.LogDebug("JWT token validated for user: {UserId}", userId);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds authorization policies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Admin policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin", "SuperAdmin"));

            // Super admin policy
            options.AddPolicy("SuperAdminOnly", policy =>
                policy.RequireRole("SuperAdmin"));

            // API scope policies
            options.AddPolicy("scope:api:read", policy =>
                policy.Requirements.Add(new ScopeRequirement("api:read")));

            options.AddPolicy("scope:api:write", policy =>
                policy.Requirements.Add(new ScopeRequirement("api:write")));

            options.AddPolicy("scope:users:read", policy =>
                policy.Requirements.Add(new ScopeRequirement("users:read")));

            options.AddPolicy("scope:users:write", policy =>
                policy.Requirements.Add(new ScopeRequirement("users:write")));
        });

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, ScopeAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Adds API key authentication middleware
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}