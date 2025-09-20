using System.Text.RegularExpressions;
using CleanArchitecture.Domain.Exceptions;

namespace CleanArchitecture.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public string Value { get; }
    
    private Email(string value)
    {
        Value = value;
    }
    
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be null or empty.");
            
        email = email.Trim().ToLowerInvariant();
        
        if (!EmailRegex.IsMatch(email))
            throw new DomainException($"Invalid email format: {email}");
            
        if (email.Length > 255)
            throw new DomainException("Email cannot exceed 255 characters.");
            
        return new Email(email);
    }
    
    public bool Equals(Email? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Email other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
    public override string ToString()
    {
        return Value;
    }
    
    public static implicit operator string(Email email)
    {
        return email.Value;
    }
    
    public static bool operator ==(Email? left, Email? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Email? left, Email? right)
    {
        return !Equals(left, right);
    }
}