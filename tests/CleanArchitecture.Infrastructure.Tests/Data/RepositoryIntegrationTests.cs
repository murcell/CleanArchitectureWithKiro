using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Tests.Data.Repositories;

namespace CleanArchitecture.Infrastructure.Tests.Data;

/// <summary>
/// Integration tests that verify the interaction between UnitOfWork and multiple repositories
/// </summary>
public class RepositoryIntegrationTests : RepositoryTestBase
{
    private readonly IUnitOfWork _unitOfWork;

    public RepositoryIntegrationTests()
    {
        _unitOfWork = Context;
    }

    [Fact]
    public async Task MultipleRepositories_WithoutTransaction_WorksCorrectly()
    {
        // Arrange
        var userRepository = _unitOfWork.Repository<User>();
        var productRepository = _unitOfWork.Repository<Product>();

        var user = User.Create("Test User", Email.Create("test@example.com"));
        
        // Act - Without transaction (since in-memory DB doesn't support transactions)
        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); // Save to get user ID
        
        var product1 = Product.Create("Product 1", "Description 1", 100.00m, "USD", 10, user.Id);
        var product2 = Product.Create("Product 2", "Description 2", 200.00m, "USD", 5, user.Id);
        
        await productRepository.AddAsync(product1);
        await productRepository.AddAsync(product2);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedUser = await userRepository.GetByIdAsync(user.Id);
        var savedProducts = await productRepository.FindAsync(p => p.UserId == user.Id);
        
        Assert.NotNull(savedUser);
        Assert.Equal(2, savedProducts.Count());
        Assert.All(savedProducts, p => Assert.Equal(user.Id, p.UserId));
    }

    [Fact]
    public async Task MultipleRepositories_WithSaveChanges_PersistsCorrectly()
    {
        // Arrange
        var userRepository = _unitOfWork.Repository<User>();
        var productRepository = _unitOfWork.Repository<Product>();

        var user = User.Create("Test User", Email.Create("test@example.com"));
        
        // Act - Test that changes are persisted correctly
        await userRepository.AddAsync(user);
        var userSaveResult = await _unitOfWork.SaveChangesAsync();
        
        var product = Product.Create("Product", "Description", 100.00m, "USD", 10, user.Id);
        await productRepository.AddAsync(product);
        var productSaveResult = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.True(userSaveResult >= 1); // At least 1 user saved (may include audit updates)
        Assert.True(productSaveResult >= 1); // At least 1 product saved (may include audit updates)
        
        var userCount = await userRepository.CountAsync();
        var productCount = await productRepository.CountAsync();
        
        Assert.Equal(1, userCount);
        Assert.Equal(1, productCount);
    }

    [Fact]
    public async Task RepositoryPattern_WithComplexQuery_WorksCorrectly()
    {
        // Arrange
        var userRepository = _unitOfWork.Repository<User>();
        var productRepository = _unitOfWork.Repository<Product>();

        // Create test data
        var user1 = User.Create("Active User", Email.Create("active@example.com"));
        var user2 = User.Create("Inactive User", Email.Create("inactive@example.com"));
        
        await userRepository.AddAsync(user1);
        await userRepository.AddAsync(user2);
        await _unitOfWork.SaveChangesAsync();
        
        user2.Deactivate();
        userRepository.Update(user2);
        await _unitOfWork.SaveChangesAsync();

        var product1 = Product.Create("Expensive Product", "High-end item", 500.00m, "USD", 2, user1.Id);
        var product2 = Product.Create("Cheap Product", "Budget item", 50.00m, "USD", 20, user1.Id);
        var product3 = Product.Create("Medium Product", "Mid-range item", 200.00m, "USD", 0, user2.Id);
        
        await productRepository.AddAsync(product1);
        await productRepository.AddAsync(product2);
        await productRepository.AddAsync(product3);
        await _unitOfWork.SaveChangesAsync();

        // Act - Complex queries using repository methods
        var activeUsers = await userRepository.FindAsync(u => u.IsActive);
        var expensiveProducts = await productRepository.FindAsync(p => p.Price.Amount > 100);
        var lowStockProducts = await productRepository.FindAsync(p => p.Stock <= 5);
        var availableProducts = await productRepository.FindAsync(p => p.IsAvailable);

        // Assert
        Assert.Single(activeUsers);
        Assert.Equal("Active User", activeUsers.First().Name);
        
        Assert.Equal(2, expensiveProducts.Count());
        Assert.Contains(expensiveProducts, p => p.Name == "Expensive Product");
        Assert.Contains(expensiveProducts, p => p.Name == "Medium Product");
        
        Assert.Equal(2, lowStockProducts.Count());
        Assert.Contains(lowStockProducts, p => p.Name == "Expensive Product");
        Assert.Contains(lowStockProducts, p => p.Name == "Medium Product");
        
        Assert.Equal(2, availableProducts.Count());
        Assert.DoesNotContain(availableProducts, p => p.Name == "Medium Product");
    }

    [Fact]
    public async Task RepositoryPattern_WithPagination_WorksCorrectly()
    {
        // Arrange
        var userRepository = _unitOfWork.Repository<User>();
        var productRepository = _unitOfWork.Repository<Product>();

        var user = User.Create("Test User", Email.Create("test@example.com"));
        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Create 15 products
        for (int i = 1; i <= 15; i++)
        {
            var product = Product.Create($"Product {i:D2}", $"Description {i}", i * 10.0m, "USD", i, user.Id);
            await productRepository.AddAsync(product);
        }
        await _unitOfWork.SaveChangesAsync();

        // Act - Test pagination
        var page1 = await productRepository.GetPagedAsync(1, 5, orderBy: p => p.Name);
        var page2 = await productRepository.GetPagedAsync(2, 5, orderBy: p => p.Name);
        var page3 = await productRepository.GetPagedAsync(3, 5, orderBy: p => p.Name);

        // Assert
        Assert.Equal(5, page1.Count());
        Assert.Equal(5, page2.Count());
        Assert.Equal(5, page3.Count());
        
        var page1List = page1.ToList();
        var page2List = page2.ToList();
        var page3List = page3.ToList();
        
        Assert.Equal("Product 01", page1List[0].Name);
        Assert.Equal("Product 05", page1List[4].Name);
        Assert.Equal("Product 06", page2List[0].Name);
        Assert.Equal("Product 10", page2List[4].Name);
        Assert.Equal("Product 11", page3List[0].Name);
        Assert.Equal("Product 15", page3List[4].Name);
    }

    [Fact]
    public async Task RepositoryPattern_WithBulkOperations_WorksCorrectly()
    {
        // Arrange
        var userRepository = _unitOfWork.Repository<User>();
        var productRepository = _unitOfWork.Repository<Product>();

        var user = User.Create("Test User", Email.Create("test@example.com"));
        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var products = new List<Product>();
        for (int i = 1; i <= 10; i++)
        {
            products.Add(Product.Create($"Product {i}", $"Description {i}", i * 10.0m, "USD", i, user.Id));
        }

        // Act - Bulk add
        await productRepository.AddRangeAsync(products);
        await _unitOfWork.SaveChangesAsync();

        // Verify bulk add
        var allProducts = await productRepository.GetAllAsync();
        Assert.Equal(10, allProducts.Count());

        // Act - Bulk update
        var productsToUpdate = allProducts.Take(5).ToList();
        foreach (var product in productsToUpdate)
        {
            product.UpdateName($"Updated {product.Name}");
        }
        productRepository.UpdateRange(productsToUpdate);
        await _unitOfWork.SaveChangesAsync();

        // Verify bulk update
        var updatedProducts = await productRepository.FindAsync(p => p.Name.StartsWith("Updated"));
        Assert.Equal(5, updatedProducts.Count());

        // Act - Bulk delete
        var productsToDelete = allProducts.Skip(5).Take(3).ToList();
        productRepository.DeleteRange(productsToDelete);
        await _unitOfWork.SaveChangesAsync();

        // Verify bulk delete
        var remainingProducts = await productRepository.GetAllAsync();
        Assert.Equal(7, remainingProducts.Count());
    }

    [Fact]
    public async Task RepositoryPattern_WithSequentialAccess_HandlesCorrectly()
    {
        // Arrange
        var userRepository = _unitOfWork.Repository<User>();
        
        var user = User.Create("Test User", Email.Create("test@example.com"));
        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act - Sequential access to test repository operations
        for (int i = 0; i < 3; i++)
        {
            var retrievedUser = await userRepository.GetByIdAsync(user.Id);
            Assert.NotNull(retrievedUser);
            
            retrievedUser.UpdateName($"Updated Name {i}");
            userRepository.Update(retrievedUser);
            await _unitOfWork.SaveChangesAsync();
        }

        // Assert - Verify final state
        var finalUser = await userRepository.GetByIdAsync(user.Id);
        Assert.NotNull(finalUser);
        Assert.Equal("Updated Name 2", finalUser.Name);
    }


}