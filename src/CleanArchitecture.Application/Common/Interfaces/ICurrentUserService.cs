namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user ID
    /// </summary>
    int? UserId { get; }
    
    /// <summary>
    /// Gets the current user name
    /// </summary>
    string? UserName { get; }
    
    /// <summary>
    /// Gets the current user email
    /// </summary>
    string? Email { get; }
    
    /// <summary>
    /// Gets the current user roles
    /// </summary>
    IEnumerable<string> Roles { get; }
    
    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    bool IsInRole(string role);
    
    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    bool HasPermission(string permission);
    
    /// <summary>
    /// Gets a specific claim value
    /// </summary>
    string? GetClaimValue(string claimType);
}