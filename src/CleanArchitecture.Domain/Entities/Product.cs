using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Events;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Entities;

public class Product : AuditableEntity<int>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public int Stock { get; private set; }
    public bool IsAvailable { get; private set; }
    public int UserId { get; private set; }
    
    // Navigation property
    public User User { get; private set; } = null!;
    
    private Product() : base() { }
    
    public static Product Create(string name, string description, decimal price, string currency, int stock, int userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be null or empty.");
            
        if (name.Length > 200)
            throw new DomainException("Product name cannot exceed 200 characters.");
            
        if (description?.Length > 1000)
            throw new DomainException("Product description cannot exceed 1000 characters.");
            
        if (stock < 0)
            throw new DomainException("Product stock cannot be negative.");
            
        if (userId <= 0)
            throw new DomainException("Product must be associated with a valid user.");
            
        var product = new Product
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = Money.Create(price, currency),
            Stock = stock,
            IsAvailable = stock > 0,
            UserId = userId
        };
        
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Price, product.UserId));
        
        return product;
    }

    public static Product Create(string name, Money price, int userId)
    {
        return Create(name, string.Empty, price.Amount, price.Currency, 0, userId);
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be null or empty.");
            
        if (name.Length > 200)
            throw new DomainException("Product name cannot exceed 200 characters.");
            
        var oldName = Name;
        Name = name.Trim();
        MarkAsUpdated();
        
        AddDomainEvent(new ProductNameUpdatedEvent(Id, oldName, Name));
    }
    
    public void UpdateDescription(string description)
    {
        if (description?.Length > 1000)
            throw new DomainException("Product description cannot exceed 1000 characters.");
            
        Description = description?.Trim() ?? string.Empty;
        MarkAsUpdated();
    }
    
    public void UpdatePrice(decimal price, string currency)
    {
        var oldPrice = Price;
        Price = Money.Create(price, currency);
        MarkAsUpdated();
        
        AddDomainEvent(new ProductPriceUpdatedEvent(Id, oldPrice, Price));
    }
    
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Stock quantity to add must be positive.");
            
        Stock += quantity;
        
        if (!IsAvailable && Stock > 0)
        {
            IsAvailable = true;
            AddDomainEvent(new ProductBecameAvailableEvent(Id, Name));
        }
        
        MarkAsUpdated();
        AddDomainEvent(new ProductStockAddedEvent(Id, quantity, Stock));
    }
    
    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Stock quantity to remove must be positive.");
            
        if (quantity > Stock)
            throw new DomainException("Cannot remove more stock than available.");
            
        Stock -= quantity;
        
        if (IsAvailable && Stock == 0)
        {
            IsAvailable = false;
            AddDomainEvent(new ProductBecameUnavailableEvent(Id, Name));
        }
        
        MarkAsUpdated();
        AddDomainEvent(new ProductStockRemovedEvent(Id, quantity, Stock));
    }
    
    public void SetAvailability(bool isAvailable)
    {
        if (isAvailable && Stock == 0)
            throw new DomainException("Cannot make product available when stock is zero.");
            
        if (IsAvailable == isAvailable)
            return;
            
        IsAvailable = isAvailable;
        MarkAsUpdated();
        
        if (isAvailable)
            AddDomainEvent(new ProductBecameAvailableEvent(Id, Name));
        else
            AddDomainEvent(new ProductBecameUnavailableEvent(Id, Name));
    }
}