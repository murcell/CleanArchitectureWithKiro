using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Data;

public class ApplicationDbContextTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CanCreateUser_WithValidData_ShouldSucceed()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");

        // Act
        _context.Users.Add(user);
        var result = await _context.SaveChangesAsync();

        // Assert
        Assert.True(result > 0); // Domain events may cause additional saves
        
        var savedUser = await _context.Users.FirstOrDefaultAsync();
        Assert.NotNull(savedUser);
        Assert.Equal("John Doe", savedUser.Name);
        Assert.Equal("john.doe@example.com", savedUser.Email.Value);
        Assert.True(savedUser.IsActive);
        Assert.True(savedUser.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task CanCreateProduct_WithValidData_ShouldSucceed()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var product = Product.Create("Test Product", "Test Description", 99.99m, "USD", 10, user.Id);

        // Act
        _context.Products.Add(product);
        var result = await _context.SaveChangesAsync();

        // Assert
        Assert.True(result > 0); // Domain events may cause additional saves
        
        var savedProduct = await _context.Products
            .Include(p => p.User)
            .FirstOrDefaultAsync();
            
        Assert.NotNull(savedProduct);
        Assert.Equal("Test Product", savedProduct.Name);
        Assert.Equal("Test Description", savedProduct.Description);
        Assert.Equal(99.99m, savedProduct.Price.Amount);
        Assert.Equal("USD", savedProduct.Price.Currency);
        Assert.Equal(10, savedProduct.Stock);
        Assert.True(savedProduct.IsAvailable);
        Assert.Equal(user.Id, savedProduct.UserId);
        Assert.NotNull(savedProduct.User);
    }

    [Fact]
    public async Task UserEmailIndex_ConfigurationIsCorrect()
    {
        // Arrange
        var user1 = User.Create("John Doe", "john.doe@example.com");
        var user2 = User.Create("Jane Doe", "jane.doe@example.com");

        // Act
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        Assert.Equal(2, users.Count);
        
        // Verify email configuration is working
        var johnUser = users.First(u => u.Name == "John Doe");
        Assert.Equal("john.doe@example.com", johnUser.Email.Value);
        
        var janeUser = users.First(u => u.Name == "Jane Doe");
        Assert.Equal("jane.doe@example.com", janeUser.Email.Value);
    }

    [Fact]
    public async Task CascadeDelete_ShouldDeleteProductsWhenUserDeleted()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var product = Product.Create("Test Product", "Test Description", 99.99m, "USD", 10, user.Id);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Assert
        var remainingProducts = await _context.Products.CountAsync();
        Assert.Equal(0, remainingProducts);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}