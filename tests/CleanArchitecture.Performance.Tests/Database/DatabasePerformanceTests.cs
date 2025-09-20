using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Performance.Tests.Common;
using System.Diagnostics;

namespace CleanArchitecture.Performance.Tests.Database;

public class DatabasePerformanceTests : PerformanceTestBase
{
    private ApplicationDbContext _context = null!;
    private List<User> _testUsers = null!;
    private List<Product> _testProducts = null!;

    protected override async Task InitializeAsync()
    {
        _context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Seed test data
        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        _testUsers = new List<User>();
        for (int i = 0; i < 1000; i++)
        {
            _testUsers.Add(User.Create($"TestUser{i}", $"test{i}@example.com"));
        }
        
        _context.Users.AddRange(_testUsers);

        // Create test products
        _testProducts = new List<Product>();
        for (int i = 0; i < 500; i++)
        {
            var userId = _testUsers[i % _testUsers.Count].Id;
            _testProducts.Add(Product.Create($"TestProduct{i}", $"Description for product {i}", 99.99m + i, "USD", 10, userId));
        }
        
        _context.Products.AddRange(_testProducts);
        
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task<List<User>> GetAllUsers_Performance()
    {
        return await _context.Users.ToListAsync();
    }

    [Benchmark]
    public async Task<User?> GetUserById_Performance()
    {
        var randomId = _testUsers[Random.Shared.Next(_testUsers.Count)].Id;
        return await _context.Users.FindAsync(randomId);
    }

    [Benchmark]
    public async Task<List<User>> GetUsersByEmailDomain_Performance()
    {
        return await _context.Users
            .Where(u => u.Email.Value.Contains("@example.com"))
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Product>> GetAllProducts_Performance()
    {
        return await _context.Products.ToListAsync();
    }

    [Benchmark]
    public async Task<Product?> GetProductById_Performance()
    {
        var randomId = _testProducts[Random.Shared.Next(_testProducts.Count)].Id;
        return await _context.Products.FindAsync(randomId);
    }

    [Benchmark]
    public async Task<List<Product>> GetProductsByPriceRange_Performance()
    {
        return await _context.Products
            .Where(p => p.Price.Amount >= 100m && p.Price.Amount <= 200m)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<int> CreateUser_Performance()
    {
        var user = User.Create($"BenchmarkUser_{Guid.NewGuid()}", $"benchmark_{Guid.NewGuid()}@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    [Benchmark]
    public async Task<int> CreateProduct_Performance()
    {
        var userId = _testUsers.First().Id;
        var product = Product.Create($"BenchmarkProduct_{Guid.NewGuid()}", "Benchmark product", 199.99m, "USD", 10, userId);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product.Id;
    }

    [Benchmark]
    public async Task UpdateUser_Performance()
    {
        var user = _testUsers[Random.Shared.Next(_testUsers.Count)];
        user.UpdateName($"UpdatedUser_{Guid.NewGuid()}");
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdateProduct_Performance()
    {
        var product = _testProducts[Random.Shared.Next(_testProducts.Count)];
        product.UpdatePrice(299.99m, "USD");
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task DatabaseQuery_Performance_ShouldMeetThresholds()
    {
        // Arrange
        var context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stopwatch = new Stopwatch();

        // Test: Get all users should complete within 1 second
        stopwatch.Start();
        var users = await context.Users.Take(100).ToListAsync();
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Get 100 users took {stopwatch.ElapsedMilliseconds}ms, exceeds 1000ms threshold");
        Assert.True(users.Count > 0, "No users returned");

        // Test: Complex query should complete within 2 seconds
        stopwatch.Restart();
        var complexQuery = await context.Users
            .Where(u => u.Email.Value.Contains("@example.com"))
            .OrderBy(u => u.Name)
            .Take(50)
            .ToListAsync();
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Complex query took {stopwatch.ElapsedMilliseconds}ms, exceeds 2000ms threshold");

        // Test: Single entity lookup should complete within 100ms
        if (users.Any())
        {
            stopwatch.Restart();
            var singleUser = await context.Users.FindAsync(users.First().Id);
            stopwatch.Stop();
            
            Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                $"Single user lookup took {stopwatch.ElapsedMilliseconds}ms, exceeds 100ms threshold");
            Assert.NotNull(singleUser);
        }
    }

    [Fact]
    public async Task DatabaseWrite_Performance_ShouldMeetThresholds()
    {
        // Arrange
        var context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stopwatch = new Stopwatch();

        // Test: Single insert should complete within 500ms
        stopwatch.Start();
        var user = User.Create($"PerformanceTestUser_{Guid.NewGuid()}", $"perf_{Guid.NewGuid()}@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Single user insert took {stopwatch.ElapsedMilliseconds}ms, exceeds 500ms threshold");

        // Test: Batch insert should complete within 2 seconds
        var batchUsers = new List<User>();
        for (int i = 0; i < 10; i++)
        {
            batchUsers.Add(User.Create($"BatchUser_{i}_{Guid.NewGuid()}", $"batch_{i}_{Guid.NewGuid()}@example.com"));
        }

        stopwatch.Restart();
        context.Users.AddRange(batchUsers);
        await context.SaveChangesAsync();
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Batch insert of 10 users took {stopwatch.ElapsedMilliseconds}ms, exceeds 2000ms threshold");

        // Test: Update should complete within 300ms
        stopwatch.Restart();
        user.UpdateName($"UpdatedName_{Guid.NewGuid()}");
        await context.SaveChangesAsync();
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 300, 
            $"Single user update took {stopwatch.ElapsedMilliseconds}ms, exceeds 300ms threshold");
    }

    [Fact]
    public async Task DatabaseConcurrency_Performance_ShouldHandleMultipleConnections()
    {
        // Arrange
        var tasks = new List<Task>();
        var stopwatch = new Stopwatch();

        // Test: Multiple concurrent reads should complete within 3 seconds
        stopwatch.Start();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var users = await context.Users.Take(10).ToListAsync();
                Assert.True(users.Count >= 0);
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
            $"20 concurrent reads took {stopwatch.ElapsedMilliseconds}ms, exceeds 3000ms threshold");

        // Test: Mixed read/write operations
        tasks.Clear();
        stopwatch.Restart();
        
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                if (index % 2 == 0)
                {
                    // Read operation
                    var users = await context.Users.Take(5).ToListAsync();
                    Assert.True(users.Count >= 0);
                }
                else
                {
                    // Write operation
                    var user = User.Create($"ConcurrentUser_{index}_{Guid.NewGuid()}", $"concurrent_{index}_{Guid.NewGuid()}@example.com");
                    context.Users.Add(user);
                    await context.SaveChangesAsync();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Mixed concurrent operations took {stopwatch.ElapsedMilliseconds}ms, exceeds 5000ms threshold");
    }

    [Fact]
    public async Task DatabasePagination_Performance_ShouldBeEfficient()
    {
        // Arrange
        var context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stopwatch = new Stopwatch();

        // Test: Paginated queries should complete within reasonable time
        var pageSize = 20;
        var totalPages = 10;

        stopwatch.Start();
        for (int page = 0; page < totalPages; page++)
        {
            var users = await context.Users
                .OrderBy(u => u.Id)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            Assert.True(users.Count >= 0);
        }
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Paginated queries took {stopwatch.ElapsedMilliseconds}ms, exceeds 2000ms threshold");

        // Test: Large offset pagination should still be reasonable
        stopwatch.Restart();
        var largeOffsetUsers = await context.Users
            .OrderBy(u => u.Id)
            .Skip(500)
            .Take(20)
            .ToListAsync();
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Large offset pagination took {stopwatch.ElapsedMilliseconds}ms, exceeds 1000ms threshold");
        Assert.True(largeOffsetUsers.Count >= 0);
    }

    protected override async Task CleanupAsync()
    {
        if (_context != null)
        {
            // Clean up test data
            var testUsers = await _context.Users
                .Where(u => u.Name.StartsWith("TestUser") || u.Name.StartsWith("BenchmarkUser") || u.Name.StartsWith("PerformanceTestUser") || u.Name.StartsWith("BatchUser"))
                .ToListAsync();
            
            var testProducts = await _context.Products
                .Where(p => p.Name.StartsWith("TestProduct") || p.Name.StartsWith("BenchmarkProduct"))
                .ToListAsync();

            _context.Users.RemoveRange(testUsers);
            _context.Products.RemoveRange(testProducts);
            
            await _context.SaveChangesAsync();
        }
    }
}