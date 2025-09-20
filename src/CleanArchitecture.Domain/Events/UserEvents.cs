using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Events;

public class UserCreatedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public string Name { get; }
    public Email Email { get; }
    
    public UserCreatedEvent(int userId, string name, Email email)
    {
        UserId = userId;
        Name = name;
        Email = email;
    }
}

public class UserNameUpdatedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public string OldName { get; }
    public string NewName { get; }
    
    public UserNameUpdatedEvent(int userId, string oldName, string newName)
    {
        UserId = userId;
        OldName = oldName;
        NewName = newName;
    }
}

public class UserEmailUpdatedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public Email OldEmail { get; }
    public Email NewEmail { get; }
    
    public UserEmailUpdatedEvent(int userId, Email oldEmail, Email newEmail)
    {
        UserId = userId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
    }
}

public class UserActivatedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public string Name { get; }
    
    public UserActivatedEvent(int userId, string name)
    {
        UserId = userId;
        Name = name;
    }
}

public class UserDeactivatedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public string Name { get; }
    
    public UserDeactivatedEvent(int userId, string name)
    {
        UserId = userId;
        Name = name;
    }
}

public class UserLoggedInEvent : BaseDomainEvent
{
    public int UserId { get; }
    public DateTime LoginTime { get; }
    
    public UserLoggedInEvent(int userId, DateTime loginTime)
    {
        UserId = userId;
        LoginTime = loginTime;
    }
}

public class UserRoleChangedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public UserRole OldRole { get; }
    public UserRole NewRole { get; }
    
    public UserRoleChangedEvent(int userId, UserRole oldRole, UserRole newRole)
    {
        UserId = userId;
        OldRole = oldRole;
        NewRole = newRole;
    }
}

public class UserEmailConfirmedEvent : BaseDomainEvent
{
    public int UserId { get; }
    public Email Email { get; }
    
    public UserEmailConfirmedEvent(int userId, Email email)
    {
        UserId = userId;
        Email = email;
    }
}

public class UserLockedOutEvent : BaseDomainEvent
{
    public int UserId { get; }
    public DateTime LockoutEnd { get; }
    
    public UserLockedOutEvent(int userId, DateTime lockoutEnd)
    {
        UserId = userId;
        LockoutEnd = lockoutEnd;
    }
}

public class UserUnlockedEvent : BaseDomainEvent
{
    public int UserId { get; }
    
    public UserUnlockedEvent(int userId)
    {
        UserId = userId;
    }
}