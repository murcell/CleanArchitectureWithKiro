using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Data.Repositories;

namespace CleanArchitecture.Infrastructure.Tests.Data.Repositories;

public class ProductRepositoryTests : RepositoryTestBase
{
    private readonly ProductRepository _productRepository;

    public ProductRepositoryTests()
    {
        _productRepository = new ProductRepository(Context);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsUserProducts()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("User 1", "user1@example.com");
        var user2 = await CreateTestUserAsync("User 2", "user2@example.com");
        
        await CreateTestProductAsync("Product 1", "Description 1", 100.00m, user1.Id);
        await CreateTestProductAsync("Product 2", "Description 2", 200.00m, user1.Id);
        await CreateTestProductAsync("Product 3", "Description 3", 300.00m, user2.Id);

        // Act
        var result = await _productRepository.GetByUserIdAsync(user1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(user1.Id, p.UserId));
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNonExistentUserId_ReturnsEmptyCollection()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await CreateTestProductAsync("Product 1", "Description 1", 100.00m, user.Id);

        // Act
        var result = await _productRepository.GetByUserIdAsync(999);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableProductsAsync_ReturnsOnlyAvailableProducts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        var availableProduct1 = await CreateTestProductAsync("Available 1", "Description", 100.00m, user.Id, 10);
        var availableProduct2 = await CreateTestProductAsync("Available 2", "Description", 200.00m, user.Id, 5);
        var unavailableProduct = await CreateTestProductAsync("Unavailable", "Description", 300.00m, user.Id, 0);

        // Act
        var result = await _productRepository.GetAvailableProductsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.True(p.IsAvailable));
        Assert.Contains(result, p => p.Id == availableProduct1.Id);
        Assert.Contains(result, p => p.Id == availableProduct2.Id);
        Assert.DoesNotContain(result, p => p.Id == unavailableProduct.Id);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithDefaultThreshold_ReturnsLowStockProducts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        var lowStock1 = await CreateTestProductAsync("Low Stock 1", "Description", 100.00m, user.Id, 5);
        var lowStock2 = await CreateTestProductAsync("Low Stock 2", "Description", 200.00m, user.Id, 10);
        var highStock = await CreateTestProductAsync("High Stock", "Description", 300.00m, user.Id, 20);

        // Act
        var result = await _productRepository.GetLowStockProductsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.True(p.Stock <= 10));
        Assert.Contains(result, p => p.Id == lowStock1.Id);
        Assert.Contains(result, p => p.Id == lowStock2.Id);
        Assert.DoesNotContain(result, p => p.Id == highStock.Id);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithCustomThreshold_ReturnsProductsBelowThreshold()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        var veryLowStock = await CreateTestProductAsync("Very Low", "Description", 100.00m, user.Id, 3);
        var lowStock = await CreateTestProductAsync("Low", "Description", 200.00m, user.Id, 7);
        var mediumStock = await CreateTestProductAsync("Medium", "Description", 300.00m, user.Id, 15);

        // Act
        var result = await _productRepository.GetLowStockProductsAsync(5);

        // Assert
        Assert.Single(result);
        Assert.All(result, p => Assert.True(p.Stock <= 5));
        Assert.Contains(result, p => p.Id == veryLowStock.Id);
        Assert.DoesNotContain(result, p => p.Id == lowStock.Id); // Stock is 7, which is > 5
        Assert.DoesNotContain(result, p => p.Id == mediumStock.Id);
    }

    [Fact]
    public async Task GetByPriceRangeAsync_WithValidRange_ReturnsProductsInRange()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        var cheapProduct = await CreateTestProductAsync("Cheap", "Description", 50.00m, user.Id);
        var midProduct = await CreateTestProductAsync("Mid", "Description", 150.00m, user.Id);
        var expensiveProduct = await CreateTestProductAsync("Expensive", "Description", 300.00m, user.Id);

        // Act
        var result = await _productRepository.GetByPriceRangeAsync(100.00m, 200.00m, "USD");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.Id == midProduct.Id);
        Assert.DoesNotContain(result, p => p.Id == cheapProduct.Id);
        Assert.DoesNotContain(result, p => p.Id == expensiveProduct.Id);
    }

    [Fact]
    public async Task GetByPriceRangeAsync_WithDifferentCurrency_ReturnsEmptyCollection()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await CreateTestProductAsync("Product", "Description", 150.00m, user.Id); // USD

        // Act
        var result = await _productRepository.GetByPriceRangeAsync(100.00m, 200.00m, "EUR");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProductsWithUsersAsync_ReturnsProductsWithUserData()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("User 1", "user1@example.com");
        var user2 = await CreateTestUserAsync("User 2", "user2@example.com");
        
        await CreateTestProductAsync("Product 1", "Description", 100.00m, user1.Id);
        await CreateTestProductAsync("Product 2", "Description", 200.00m, user2.Id);

        // Act
        var result = await _productRepository.GetProductsWithUsersAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.NotNull(p.User));
        
        var product1 = result.First(p => p.Name == "Product 1");
        var product2 = result.First(p => p.Name == "Product 2");
        
        Assert.Equal("User 1", product1.User.Name);
        Assert.Equal("User 2", product2.User.Name);
    }

    [Fact]
    public async Task SearchAsync_WithNameMatch_ReturnsMatchingProducts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        await CreateTestProductAsync("Laptop Computer", "High-performance laptop", 1000.00m, user.Id);
        await CreateTestProductAsync("Desktop Computer", "Powerful desktop", 800.00m, user.Id);
        await CreateTestProductAsync("Smartphone", "Mobile phone", 500.00m, user.Id);

        // Act
        var result = await _productRepository.SearchAsync("Computer");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Contains("Computer", p.Name));
    }

    [Fact]
    public async Task SearchAsync_WithDescriptionMatch_ReturnsMatchingProducts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        await CreateTestProductAsync("Laptop", "High-performance gaming laptop", 1000.00m, user.Id);
        await CreateTestProductAsync("Mouse", "Gaming mouse with RGB", 50.00m, user.Id);
        await CreateTestProductAsync("Keyboard", "Mechanical keyboard", 100.00m, user.Id);

        // Act
        var result = await _productRepository.SearchAsync("gaming");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Name == "Laptop");
        Assert.Contains(result, p => p.Name == "Mouse");
    }

    [Fact]
    public async Task SearchAsync_WithEmptySearchTerm_ReturnsAllProducts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        await CreateTestProductAsync("Product 1", "Description", 100.00m, user.Id);
        await CreateTestProductAsync("Product 2", "Description", 200.00m, user.Id);
        await CreateTestProductAsync("Product 3", "Description", 300.00m, user.Id);

        // Act
        var result = await _productRepository.SearchAsync("");

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        await CreateTestProductAsync("Laptop", "Computer device", 1000.00m, user.Id);
        await CreateTestProductAsync("Mouse", "Input device", 50.00m, user.Id);

        // Act
        var result = await _productRepository.SearchAsync("smartphone");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        await CreateTestProductAsync("LAPTOP", "HIGH-PERFORMANCE DEVICE", 1000.00m, user.Id);

        // Act
        var result = await _productRepository.SearchAsync("laptop");

        // Assert
        Assert.Single(result);
        Assert.Equal("LAPTOP", result.First().Name);
    }

    private async Task<Product> CreateTestProductAsync(string name, string description, decimal price, int userId, int stock = 1)
    {
        var product = Product.Create(name, description, price, "USD", stock, userId);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        return product;
    }
}