using CleanArchitecture.Domain.Exceptions;

namespace CleanArchitecture.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    public static Money Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency cannot be null or empty.");
            
        currency = currency.Trim().ToUpperInvariant();
        
        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");
            
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");
            
        return new Money(Math.Round(amount, 2), currency);
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot add different currencies: {Currency} and {other.Currency}");
            
        return new Money(Amount + other.Amount, Currency);
    }
    
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot subtract different currencies: {Currency} and {other.Currency}");
            
        var result = Amount - other.Amount;
        if (result < 0)
            throw new DomainException("Subtraction would result in negative amount.");
            
        return new Money(result, Currency);
    }
    
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new DomainException("Multiplication factor cannot be negative.");
            
        return new Money(Amount * factor, Currency);
    }
    
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Money other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }
    
    public override string ToString()
    {
        return $"{Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
    }
    
    public static bool operator ==(Money? left, Money? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Money? left, Money? right)
    {
        return !Equals(left, right);
    }
    
    public static Money operator +(Money left, Money right)
    {
        return left.Add(right);
    }
    
    public static Money operator -(Money left, Money right)
    {
        return left.Subtract(right);
    }
    
    public static Money operator *(Money left, decimal right)
    {
        return left.Multiply(right);
    }
}