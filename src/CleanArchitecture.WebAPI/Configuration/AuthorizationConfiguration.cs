using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Configuration for authorization policies
/// </summary>
public static class AuthorizationConfiguration
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Admin policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            // User management policy
            options.AddPolicy("UserManagement", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim("permission", "user.manage")));

            // Product management policy
            options.AddPolicy("ProductManagement", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.IsInRole("ProductManager") ||
                    context.User.HasClaim("permission", "product.manage")));

            // API key management policy
            options.AddPolicy("ApiKeyManagement", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim("permission", "apikey.manage")));

            // Own resource access policy
            options.AddPolicy("OwnResourceAccess", policy =>
                policy.RequireAssertion(context =>
                {
                    // This would be used with resource-based authorization
                    // The actual resource check would be done in the handler
                    return context.User.Identity?.IsAuthenticated == true;
                }));

            // Minimum age policy example
            options.AddPolicy("MinimumAge18", policy =>
                policy.RequireAssertion(context =>
                {
                    var birthDateClaim = context.User.FindFirst("birth_date");
                    if (birthDateClaim != null && DateTime.TryParse(birthDateClaim.Value, out var birthDate))
                    {
                        var age = DateTime.Today.Year - birthDate.Year;
                        if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;
                        return age >= 18;
                    }
                    return false;
                }));

            // Email verified policy
            options.AddPolicy("EmailVerified", policy =>
                policy.RequireClaim("email_verified", "true"));

            // Premium user policy
            options.AddPolicy("PremiumUser", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Premium") ||
                    context.User.HasClaim("subscription", "premium")));
        });

        return services;
    }
}

/// <summary>
/// Custom authorization requirements
/// </summary>
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }

    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}

/// <summary>
/// Handler for minimum age requirement
/// </summary>
public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var birthDateClaim = context.User.FindFirst("birth_date");
        if (birthDateClaim != null && DateTime.TryParse(birthDateClaim.Value, out var birthDate))
        {
            var age = DateTime.Today.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;
            
            if (age >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Resource-based authorization requirement
/// </summary>
public class ResourceOwnerRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler for resource owner requirement
/// </summary>
public class ResourceOwnerHandler : AuthorizationHandler<ResourceOwnerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        // This would typically check if the user owns the resource
        // The actual resource would be passed through the context
        
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Additional logic to check resource ownership would go here
        // For now, we'll just check if the user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}