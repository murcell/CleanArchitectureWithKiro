namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Represents the roles that a user can have in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular user with basic permissions
    /// </summary>
    User = 0,
    
    /// <summary>
    /// Administrator with elevated permissions
    /// </summary>
    Admin = 1,
    
    /// <summary>
    /// Super administrator with full system access
    /// </summary>
    SuperAdmin = 2,
    
    /// <summary>
    /// Moderator with content management permissions
    /// </summary>
    Moderator = 3,
    
    /// <summary>
    /// API user for system integrations
    /// </summary>
    ApiUser = 4
}