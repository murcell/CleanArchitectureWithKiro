using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Infrastructure.Security.Authorization;

/// <summary>
/// Authorization requirement for API key scopes
/// </summary>
public class ScopeRequirement : IAuthorizationRequirement
{
    public string Scope { get; }

    public ScopeRequirement(string scope)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }
}