using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Infrastructure.Security.Authorization;

/// <summary>
/// Authorization attribute that requires a specific scope
/// </summary>
public class RequireScopeAttribute : AuthorizeAttribute
{
    public RequireScopeAttribute(string scope) : base($"scope:{scope}")
    {
    }
}