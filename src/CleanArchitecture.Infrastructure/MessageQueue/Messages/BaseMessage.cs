namespace CleanArchitecture.Infrastructure.MessageQueue.Messages;

public abstract class BaseMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class UserCreatedMessage : BaseMessage
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserUpdatedMessage : BaseMessage
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Dictionary<string, object>? Changes { get; set; }
}

public class UserDeletedMessage : BaseMessage
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class EmailNotificationMessage : BaseMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Template { get; set; }
    public Dictionary<string, object>? TemplateData { get; set; }
}