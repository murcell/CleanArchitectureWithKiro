namespace CleanArchitecture.Domain.Events;

public class ApiKeyCreatedEvent : BaseDomainEvent
{
    public int ApiKeyId { get; }
    public string Name { get; }
    public string KeyPrefix { get; }
    
    public ApiKeyCreatedEvent(int apiKeyId, string name, string keyPrefix)
    {
        ApiKeyId = apiKeyId;
        Name = name;
        KeyPrefix = keyPrefix;
    }
}

public class ApiKeyUpdatedEvent : BaseDomainEvent
{
    public int ApiKeyId { get; }
    public string OldName { get; }
    public string NewName { get; }
    
    public ApiKeyUpdatedEvent(int apiKeyId, string oldName, string newName)
    {
        ApiKeyId = apiKeyId;
        OldName = oldName;
        NewName = newName;
    }
}

public class ApiKeyScopesUpdatedEvent : BaseDomainEvent
{
    public int ApiKeyId { get; }
    public string[] OldScopes { get; }
    public string[] NewScopes { get; }
    
    public ApiKeyScopesUpdatedEvent(int apiKeyId, string[] oldScopes, string[] newScopes)
    {
        ApiKeyId = apiKeyId;
        OldScopes = oldScopes;
        NewScopes = newScopes;
    }
}

public class ApiKeyActivatedEvent : BaseDomainEvent
{
    public int ApiKeyId { get; }
    public string Name { get; }
    
    public ApiKeyActivatedEvent(int apiKeyId, string name)
    {
        ApiKeyId = apiKeyId;
        Name = name;
    }
}

public class ApiKeyDeactivatedEvent : BaseDomainEvent
{
    public int ApiKeyId { get; }
    public string Name { get; }
    
    public ApiKeyDeactivatedEvent(int apiKeyId, string name)
    {
        ApiKeyId = apiKeyId;
        Name = name;
    }
}

public class ApiKeyUsedEvent : BaseDomainEvent
{
    public int ApiKeyId { get; }
    public DateTime UsedAt { get; }
    
    public ApiKeyUsedEvent(int apiKeyId, DateTime usedAt)
    {
        ApiKeyId = apiKeyId;
        UsedAt = usedAt;
    }
}