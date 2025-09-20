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
    // This would typically be injected with actual user context services
    // For now, providing a basic implementation

    public int? GetCurrentUserId()
    {
        // This would typically get the user ID from the current HTTP context or security context
        return null;
    }

    public IEnumerable<string> GetCurrentUserRoles()
    {
        // This would typically get roles from the current security context
        return Enumerable.Empty<string>();
    }

    public string? GetCurrentTenantId()
    {
        // This would typically get tenant ID from the current context
        return null;
    }

    public IDictionary<string, object> GetContextProperties()
    {
        // This would typically provide additional context properties
        return new Dictionary<string, object>();
    }

    public bool HasPermission(string permission)
    {
        // This would typically check permissions against the current user's roles/permissions
        return false;
    }
}