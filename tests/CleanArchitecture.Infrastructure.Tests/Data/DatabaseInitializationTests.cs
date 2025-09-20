using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Data.SeedData;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchitecture.Infrastructure.Tests.Data;

public class DatabaseInitializationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContext> _logger;

    public DatabaseInitializationTests(ITestOutputHelper output)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        
        // Create a simple logger for testing
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ApplicationDbContext>();
    }

    [Fact]
    public async Task SeedAsync_WithEmptyDatabase_ShouldCreateSeedData()
    {
        // Arrange
        Assert.Equal(0, await _context.Users.CountAsync());
        Assert.Equal(0, await _context.Products.CountAsync());

        // Act
        await ApplicationDbContextSeed.SeedAsync(_context, _logger);

        // Assert
        var userCount = await _context.Users.CountAsync();
        var productCount = await _context.Products.CountAsync();
        
        Assert.True(userCount > 0, "Should have created seed users");
        Assert.True(productCount > 0, "Should have created seed products");
        
        // Verify relationships
        var usersWithProducts = await _context.Users
            .Include(u => u.Products)
            .ToListAsync();
            
        Assert.All(usersWithProducts, user => 
        {
            Assert.True(user.Products.Count > 0, $"User {user.Name} should have products");
        });
    }

    [Fact]
    public async Task SeedAsync_WithExistingData_ShouldNotDuplicateData()
    {
        // Arrange - First seeding
        await ApplicationDbContextSeed.SeedAsync(_context, _logger);
        var initialUserCount = await _context.Users.CountAsync();
        var initialProductCount = await _context.Products.CountAsync();

        // Act - Second seeding
        await ApplicationDbContextSeed.SeedAsync(_context, _logger);

        // Assert - Should not have duplicated data
        var finalUserCount = await _context.Users.CountAsync();
        var finalProductCount = await _context.Products.CountAsync();
        
        Assert.Equal(initialUserCount, finalUserCount);
        Assert.Equal(initialProductCount, finalProductCount);
    }

    [Fact]
    public async Task DatabaseSchema_ShouldMatchEntityConfiguration()
    {
        // Arrange & Act
        await _context.Database.EnsureCreatedAsync();

        // Assert - Verify that we can query the entities without errors
        var users = await _context.Users.ToListAsync();
        var products = await _context.Products.ToListAsync();

        // This test passes if no exceptions are thrown during schema creation
        Assert.NotNull(users);
        Assert.NotNull(products);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}