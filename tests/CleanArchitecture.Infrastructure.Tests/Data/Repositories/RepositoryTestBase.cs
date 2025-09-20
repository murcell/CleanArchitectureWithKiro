using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Data;

namespace CleanArchitecture.Infrastructure.Tests.Data.Repositories;

public abstract class RepositoryTestBase : IDisposable
{
    protected readonly ApplicationDbContext Context;
    private readonly ServiceProvider _serviceProvider;

    protected RepositoryTestBase()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                   .EnableSensitiveDataLogging()
                   .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole())));

        _serviceProvider = services.BuildServiceProvider();
        Context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    protected async Task<User> CreateTestUserAsync(string name = "Test User", string email = "test@example.com")
    {
        var user = User.Create(name, Email.Create(email));
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    protected async Task<Product> CreateTestProductAsync(string name = "Test Product", decimal price = 100.00m, int userId = 1)
    {
        var product = Product.Create(name, Money.Create(price, "USD"), userId);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        return product;
    }

    public void Dispose()
    {
        Context?.Dispose();
        _serviceProvider?.Dispose();
    }
}