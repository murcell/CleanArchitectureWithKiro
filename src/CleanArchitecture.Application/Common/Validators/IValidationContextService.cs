using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Service interface for providing validation context information
/// </summary>
public interface IValidationContextService
{
    /// <summary>
    /// Gets the current user ID for validation context
    /// </summary>
    int? GetCurrentUserId();

    /// <summary>
    /// Gets the current user roles for validation context
    /// </summary>
    IEnumerable<string> GetCurrentUserRoles();

    /// <summary>
    /// Gets the current tenant ID for multi-tenant validation
    /// </summary>
    string? GetCurrentTenantId();

    /// <summary>
    /// Gets additional validation context properties
    /// </summary>
    IDictionary<string, object> GetContextProperties();

    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    bool HasPermission(string permission);
}

/// <summary>
/// Default implementation of validation context service
/// </summary>
public class ValidationContextService : IValidationContextService
{
    private readonly ICurrentUserService _currentUserService;

    public ValidationContextService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public int? GetCurrentUserId()
    {
        return _currentUserService.UserId;
    }

    public IEnumerable<string> GetCurrentUserRoles()
    {
        return _currentUserService.Roles;
    }

    public string? GetCurrentTenantId()
    {
        // This would typically get tenant ID from the current context
        // For now, we can get it from claims if implemented
        return _currentUserService.GetClaimValue("tenant_id");
    }

    public IDictionary<string, object> GetContextProperties()
    {
        var properties = new Dictionary<string, object>();
        
        if (_currentUserService.UserId.HasValue)
            properties["UserId"] = _currentUserService.UserId.Value;
            
        if (!string.IsNullOrEmpty(_currentUserService.UserName))
            properties["UserName"] = _currentUserService.UserName;
            
        if (!string.IsNullOrEmpty(_currentUserService.Email))
            properties["Email"] = _currentUserService.Email;
            
        properties["IsAuthenticated"] = _currentUserService.IsAuthenticated;
        properties["Roles"] = _currentUserService.Roles.ToArray();
        
        return properties;
    }

    public bool HasPermission(string permission)
    {
        return _currentUserService.HasPermission(permission);
    }
}