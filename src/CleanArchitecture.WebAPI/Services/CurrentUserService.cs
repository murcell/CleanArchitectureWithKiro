using CleanArchitecture.Application.Common.Interfaces;
using System.Security.Claims;

namespace CleanArchitecture.WebAPI.Services;

/// <summary>
/// Service for accessing current user information from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public int? UserId
    {
        get
        {
            var userIdClaim = GetClaimValue(ClaimTypes.NameIdentifier) ?? GetClaimValue("sub") ?? GetClaimValue("user_id");
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public string? UserName => GetClaimValue(ClaimTypes.Name) ?? GetClaimValue("username");

    public string? Email => GetClaimValue(ClaimTypes.Email) ?? GetClaimValue("email");

    public IEnumerable<string> Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return Enumerable.Empty<string>();

            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList();
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true;
        }
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(role) == true;
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return false;
        
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        // Check for permission claim
        var hasPermission = user.Claims.Any(c => 
            (c.Type == "permission" || c.Type == "permissions") && 
            c.Value == permission);

        if (hasPermission) return true;

        // Check role-based permissions (admin has all permissions)
        if (IsInRole("Admin")) return true;

        _logger.LogDebug("Permission check failed for user {UserId}, permission: {Permission}", 
            UserId, permission);
        
        return false;
    }

    public string? GetClaimValue(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType)) return null;
        
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(claimType)?.Value;
    }
}