using CleanArchitecture.Domain.Events;

namespace CleanArchitecture.Domain.Common;

public abstract class BaseEntity<T>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public T Id { get; protected set; } = default!;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected BaseEntity() { }
    
    protected BaseEntity(T id)
    {
        Id = id;
    }
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<T> other)
            return false;
            
        if (ReferenceEquals(this, other))
            return true;
            
        if (GetType() != other.GetType())
            return false;
            
        if (Id!.Equals(default(T)) || other.Id!.Equals(default(T)))
            return false;
            
        return Id.Equals(other.Id);
    }
    
    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }
    
    public static bool operator ==(BaseEntity<T>? a, BaseEntity<T>? b)
    {
        if (a is null && b is null)
            return true;
            
        if (a is null || b is null)
            return false;
            
        return a.Equals(b);
    }
    
    public static bool operator !=(BaseEntity<T>? a, BaseEntity<T>? b)
    {
        return !(a == b);
    }
}