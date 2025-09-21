using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Events;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class User : AuditableEntity<int>
{
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastLogoutAt { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsEmailConfirmed { get; private set; }
    public string? EmailConfirmationToken { get; private set; }
    public DateTime? EmailConfirmationTokenExpiryTime { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiryTime { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    
    // Navigation properties
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();
    
    private User() : base() { }
    
    public static User Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("User name cannot be null or empty.");
            
        if (name.Length > 100)
            throw new DomainException("User name cannot exceed 100 characters.");
            
        var user = new User
        {
            Name = name.Trim(),
            Email = Email.Create(email),
            IsActive = true
        };
        
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Name, user.Email));
        
        return user;
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("User name cannot be null or empty.");
            
        if (name.Length > 100)
            throw new DomainException("User name cannot exceed 100 characters.");
            
        var oldName = Name;
        Name = name.Trim();
        MarkAsUpdated();
        
        AddDomainEvent(new UserNameUpdatedEvent(Id, oldName, Name));
    }
    
    public void UpdateEmail(string email)
    {
        var oldEmail = Email;
        Email = Email.Create(email);
        MarkAsUpdated();
        
        AddDomainEvent(new UserEmailUpdatedEvent(Id, oldEmail, Email));
    }
    
    public void Activate()
    {
        if (IsActive)
            throw new DomainException("User is already active.");
            
        IsActive = true;
        MarkAsUpdated();
        
        AddDomainEvent(new UserActivatedEvent(Id, Name));
    }
    
    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("User is already inactive.");
            
        IsActive = false;
        MarkAsUpdated();
        
        AddDomainEvent(new UserDeactivatedEvent(Id, Name));
    }
    
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        MarkAsUpdated();
        
        AddDomainEvent(new UserLoggedInEvent(Id, LastLoginAt.Value));
    }
    
    public void RecordLogout()
    {
        LastLogoutAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        AddDomainEvent(new UserLoggedOutEvent(Id, LastLogoutAt.Value));
    }
    
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be null or empty.");
            
        PasswordHash = passwordHash;
        MarkAsUpdated();
    }
    
    public void SetRefreshToken(string refreshToken, DateTime expiryTime)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new DomainException("Refresh token cannot be null or empty.");
            
        if (expiryTime <= DateTime.UtcNow)
            throw new DomainException("Refresh token expiry time must be in the future.");
            
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
        MarkAsUpdated();
    }
    
    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
        MarkAsUpdated();
    }
    
    public bool IsRefreshTokenValid(string refreshToken)
    {
        return RefreshToken == refreshToken && 
               RefreshTokenExpiryTime.HasValue && 
               RefreshTokenExpiryTime.Value > DateTime.UtcNow;
    }
    
    public void SetRole(UserRole role)
    {
        var oldRole = Role;
        Role = role;
        MarkAsUpdated();
        
        AddDomainEvent(new UserRoleChangedEvent(Id, oldRole, role));
    }
    
    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
        EmailConfirmationToken = null;
        EmailConfirmationTokenExpiryTime = null;
        MarkAsUpdated();
        
        AddDomainEvent(new UserEmailConfirmedEvent(Id, Email));
    }
    
    public void SetEmailConfirmationToken(string token, DateTime expiryTime)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Email confirmation token cannot be null or empty.");
            
        if (expiryTime <= DateTime.UtcNow)
            throw new DomainException("Email confirmation token expiry time must be in the future.");
            
        EmailConfirmationToken = token;
        EmailConfirmationTokenExpiryTime = expiryTime;
        MarkAsUpdated();
    }
    
    public bool IsEmailConfirmationTokenValid(string token)
    {
        return EmailConfirmationToken == token && 
               EmailConfirmationTokenExpiryTime.HasValue && 
               EmailConfirmationTokenExpiryTime.Value > DateTime.UtcNow;
    }
    
    public void SetPasswordResetToken(string token, DateTime expiryTime)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Password reset token cannot be null or empty.");
            
        if (expiryTime <= DateTime.UtcNow)
            throw new DomainException("Password reset token expiry time must be in the future.");
            
        PasswordResetToken = token;
        PasswordResetTokenExpiryTime = expiryTime;
        MarkAsUpdated();
    }
    
    public bool IsPasswordResetTokenValid(string token)
    {
        return PasswordResetToken == token && 
               PasswordResetTokenExpiryTime.HasValue && 
               PasswordResetTokenExpiryTime.Value > DateTime.UtcNow;
    }
    
    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiryTime = null;
        MarkAsUpdated();
    }
    
    public void RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;
        MarkAsUpdated();
        
        // Lock account after 5 failed attempts for 30 minutes
        if (FailedLoginAttempts >= 5)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            AddDomainEvent(new UserLockedOutEvent(Id, LockoutEnd.Value));
        }
    }
    
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }
    
    public void UnlockAccount()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        MarkAsUpdated();
        
        AddDomainEvent(new UserUnlockedEvent(Id));
    }
    
    public void AddProduct(Product product)
    {
        if (product == null)
            throw new DomainException("Product cannot be null.");
            
        if (_products.Any(p => p.Id == product.Id))
            throw new DomainException("Product is already associated with this user.");
            
        _products.Add(product);
        MarkAsUpdated();
    }
    
    public void RemoveProduct(int productId)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product == null)
            throw new DomainException("Product not found in user's products.");
            
        _products.Remove(product);
        MarkAsUpdated();
    }
}