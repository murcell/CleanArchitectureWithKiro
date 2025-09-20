using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Events;
using CleanArchitecture.Domain.Exceptions;
using FluentAssertions;

namespace CleanArchitecture.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var price = 99.99m;
        var currency = "USD";
        var stock = 10;
        var userId = 1;
        
        // Act
        var product = Product.Create(name, description, price, currency, stock, userId);
        
        // Assert
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Price.Amount.Should().Be(price);
        product.Price.Currency.Should().Be(currency);
        product.Stock.Should().Be(stock);
        product.IsAvailable.Should().BeTrue();
        product.UserId.Should().Be(userId);
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductCreatedEvent>();
    }
    
    [Fact]
    public void Create_WithZeroStock_ShouldCreateUnavailableProduct()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var price = 99.99m;
        var currency = "USD";
        var stock = 0;
        var userId = 1;
        
        // Act
        var product = Product.Create(name, description, price, currency, stock, userId);
        
        // Assert
        product.Stock.Should().Be(0);
        product.IsAvailable.Should().BeFalse();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowDomainException(string invalidName)
    {
        // Act & Assert
        var action = () => Product.Create(invalidName, "Description", 99.99m, "USD", 10, 1);
        action.Should().Throw<DomainException>()
            .WithMessage("Product name cannot be null or empty.");
    }
    
    [Fact]
    public void Create_WithNameTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('a', 201);
        
        // Act & Assert
        var action = () => Product.Create(longName, "Description", 99.99m, "USD", 10, 1);
        action.Should().Throw<DomainException>()
            .WithMessage("Product name cannot exceed 200 characters.");
    }
    
    [Fact]
    public void Create_WithDescriptionTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longDescription = new string('a', 1001);
        
        // Act & Assert
        var action = () => Product.Create("Product", longDescription, 99.99m, "USD", 10, 1);
        action.Should().Throw<DomainException>()
            .WithMessage("Product description cannot exceed 1000 characters.");
    }
    
    [Fact]
    public void Create_WithNegativeStock_ShouldThrowDomainException()
    {
        // Act & Assert
        var action = () => Product.Create("Product", "Description", 99.99m, "USD", -1, 1);
        action.Should().Throw<DomainException>()
            .WithMessage("Product stock cannot be negative.");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidUserId_ShouldThrowDomainException(int invalidUserId)
    {
        // Act & Assert
        var action = () => Product.Create("Product", "Description", 99.99m, "USD", 10, invalidUserId);
        action.Should().Throw<DomainException>()
            .WithMessage("Product must be associated with a valid user.");
    }
    
    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var product = Product.Create("Old Name", "Description", 99.99m, "USD", 10, 1);
        product.ClearDomainEvents();
        var newName = "New Name";
        
        // Act
        product.UpdateName(newName);
        
        // Assert
        product.Name.Should().Be(newName);
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductNameUpdatedEvent>();
    }
    
    [Fact]
    public void UpdatePrice_WithValidPrice_ShouldUpdatePrice()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 99.99m, "USD", 10, 1);
        product.ClearDomainEvents();
        var newPrice = 149.99m;
        
        // Act
        product.UpdatePrice(newPrice, "USD");
        
        // Assert
        product.Price.Amount.Should().Be(newPrice);
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductPriceUpdatedEvent>();
    }
    
    [Fact]
    public void AddStock_WithValidQuantity_ShouldIncreaseStock()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 99.99m, "USD", 5, 1);
        product.ClearDomainEvents();
        var quantityToAdd = 10;
        
        // Act
        product.AddStock(quantityToAdd);
        
        // Assert
        product.Stock.Should().Be(15);
        product.IsAvailable.Should().BeTrue();
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductStockAddedEvent>();
    }
    
    [Fact]
    public void AddStock_WhenProductUnavailable_ShouldMakeAvailable()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 99.99m, "USD", 0, 1);
        product.ClearDomainEvents();
        
        // Act
        product.AddStock(5);
        
        // Assert
        product.Stock.Should().Be(5);
        product.IsAvailable.Should().BeTrue();
        product.DomainEvents.Should().HaveCount(2);
        product.DomainEvents.Should().Contain(e => e is ProductBecameAvailableEvent);
        product.DomainEvents.Should().Contain(e => e is ProductStockAddedEvent);
    }
    
    [Fact]
    public void RemoveStock_WithValidQuantity_ShouldDecreaseStock()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 99.99m, "USD", 10, 1);
        product.ClearDomainEvents();
        var quantityToRemove = 5;
        
        // Act
        product.RemoveStock(quantityToRemove);
        
        // Assert
        product.Stock.Should().Be(5);
        product.IsAvailable.Should().BeTrue();
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductStockRemovedEvent>();
    }
    
    [Fact]
    public void RemoveStock_WhenStockBecomesZero_ShouldMakeUnavailable()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 99.99m, "USD", 5, 1);
        product.ClearDomainEvents();
        
        // Act
        product.RemoveStock(5);
        
        // Assert
        product.Stock.Should().Be(0);
        product.IsAvailable.Should().BeFalse();
        product.DomainEvents.Should().HaveCount(2);
        product.DomainEvents.Should().Contain(e => e is ProductBecameUnavailableEvent);
        product.DomainEvents.Should().Contain(e => e is ProductStockRemovedEvent);
    }
    
    [Fact]
    public void RemoveStock_WithQuantityGreaterThanStock_ShouldThrowDomainException()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 99.99m, "USD", 5, 1);
        
        // Act & Assert
        var action = () => product.RemoveStock(10);
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot remove more stock than available.");
    }
}