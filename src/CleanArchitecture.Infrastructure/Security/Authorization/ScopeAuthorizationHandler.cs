using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Security.Authorization;

/// <summary>
/// Authorization handler for API key scopes
/// </summary>
public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    private readonly ILogger<ScopeAuthorizationHandler> _logger;

    public ScopeAuthorizationHandler(ILogger<ScopeAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        // Check if user has the required scope
        var scopes = context.User.FindAll("scope").Select(c => c.Value).ToList();
        
        if (scopes.Contains(requirement.Scope, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("User has required scope: {Scope}", requirement.Scope);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User does not have required scope: {Scope}. Available scopes: {Scopes}", 
                requirement.Scope, string.Join(", ", scopes));
        }

        return Task.CompletedTask;
    }
}