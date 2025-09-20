using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Events;

public class ProductCreatedEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public string Name { get; }
    public Money Price { get; }
    public int UserId { get; }
    
    public ProductCreatedEvent(int productId, string name, Money price, int userId)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        UserId = userId;
    }
}

public class ProductNameUpdatedEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public string OldName { get; }
    public string NewName { get; }
    
    public ProductNameUpdatedEvent(int productId, string oldName, string newName)
    {
        ProductId = productId;
        OldName = oldName;
        NewName = newName;
    }
}

public class ProductPriceUpdatedEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public Money OldPrice { get; }
    public Money NewPrice { get; }
    
    public ProductPriceUpdatedEvent(int productId, Money oldPrice, Money newPrice)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
    }
}

public class ProductStockAddedEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public int QuantityAdded { get; }
    public int NewStock { get; }
    
    public ProductStockAddedEvent(int productId, int quantityAdded, int newStock)
    {
        ProductId = productId;
        QuantityAdded = quantityAdded;
        NewStock = newStock;
    }
}

public class ProductStockRemovedEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public int QuantityRemoved { get; }
    public int NewStock { get; }
    
    public ProductStockRemovedEvent(int productId, int quantityRemoved, int newStock)
    {
        ProductId = productId;
        QuantityRemoved = quantityRemoved;
        NewStock = newStock;
    }
}

public class ProductBecameAvailableEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public string Name { get; }
    
    public ProductBecameAvailableEvent(int productId, string name)
    {
        ProductId = productId;
        Name = name;
    }
}

public class ProductBecameUnavailableEvent : BaseDomainEvent
{
    public int ProductId { get; }
    public string Name { get; }
    
    public ProductBecameUnavailableEvent(int productId, string name)
    {
        ProductId = productId;
        Name = name;
    }
}