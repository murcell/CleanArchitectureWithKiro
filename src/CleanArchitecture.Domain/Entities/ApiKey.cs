using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Events;

namespace CleanArchitecture.Domain.Entities;

public class ApiKey : AuditableEntity<int>
{
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public string[] Scopes { get; private set; } = Array.Empty<string>();
    public string? Description { get; private set; }
    public int? UserId { get; private set; }
    
    // Navigation properties
    public User? User { get; private set; }
    
    private ApiKey() : base() { }
    
    public static ApiKey Create(string name, string keyHash, string keyPrefix, string[] scopes, string? description = null, DateTime? expiresAt = null, int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("API key name cannot be null or empty.");
            
        if (name.Length > 100)
            throw new DomainException("API key name cannot exceed 100 characters.");
            
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new DomainException("API key hash cannot be null or empty.");
            
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new DomainException("API key prefix cannot be null or empty.");
            
        if (scopes == null || scopes.Length == 0)
            throw new DomainException("API key must have at least one scope.");
            
        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new DomainException("API key expiry time must be in the future.");
            
        var apiKey = new ApiKey
        {
            Name = name.Trim(),
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Scopes = scopes,
            Description = description?.Trim(),
            ExpiresAt = expiresAt,
            UserId = userId,
            IsActive = true
        };
        
        apiKey.AddDomainEvent(new ApiKeyCreatedEvent(apiKey.Id, apiKey.Name, apiKey.KeyPrefix));
        
        return apiKey;
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("API key name cannot be null or empty.");
            
        if (name.Length > 100)
            throw new DomainException("API key name cannot exceed 100 characters.");
            
        var oldName = Name;
        Name = name.Trim();
        MarkAsUpdated();
        
        AddDomainEvent(new ApiKeyUpdatedEvent(Id, oldName, Name));
    }
    
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }
    
    public void UpdateScopes(string[] scopes)
    {
        if (scopes == null || scopes.Length == 0)
            throw new DomainException("API key must have at least one scope.");
            
        var oldScopes = Scopes;
        Scopes = scopes;
        MarkAsUpdated();
        
        AddDomainEvent(new ApiKeyScopesUpdatedEvent(Id, oldScopes, scopes));
    }
    
    public void Activate()
    {
        if (IsActive)
            throw new DomainException("API key is already active.");
            
        IsActive = true;
        MarkAsUpdated();
        
        AddDomainEvent(new ApiKeyActivatedEvent(Id, Name));
    }
    
    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("API key is already inactive.");
            
        IsActive = false;
        MarkAsUpdated();
        
        AddDomainEvent(new ApiKeyDeactivatedEvent(Id, Name));
    }
    
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        AddDomainEvent(new ApiKeyUsedEvent(Id, LastUsedAt.Value));
    }
    
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }
    
    public bool IsValid()
    {
        return IsActive && !IsExpired();
    }
    
    public bool HasScope(string scope)
    {
        return Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
    
    public void SetExpiry(DateTime? expiresAt)
    {
        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new DomainException("API key expiry time must be in the future.");
            
        ExpiresAt = expiresAt;
        MarkAsUpdated();
    }
}