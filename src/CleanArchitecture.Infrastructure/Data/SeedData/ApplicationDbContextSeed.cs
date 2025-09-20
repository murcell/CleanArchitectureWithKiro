using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.SeedData;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            await SeedUsersAsync(context, logger);
            await SeedProductsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Users.AnyAsync())
        {
            logger.LogInformation("Seeding users...");

            var users = new[]
            {
                User.Create("John Doe", "john.doe@example.com"),
                User.Create("Jane Smith", "jane.smith@example.com"),
                User.Create("Bob Johnson", "bob.johnson@example.com"),
                User.Create("Alice Brown", "alice.brown@example.com"),
                User.Create("Charlie Wilson", "charlie.wilson@example.com")
            };

            // Set created by for seed data
            foreach (var user in users)
            {
                user.SetCreatedBy("System");
            }

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            logger.LogInformation($"Seeded {users.Length} users");
        }
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Products.AnyAsync())
        {
            logger.LogInformation("Seeding products...");

            var users = await context.Users.ToListAsync();
            if (!users.Any())
            {
                logger.LogWarning("No users found for seeding products");
                return;
            }

            var products = new List<Product>();

            // Create products for each user
            foreach (var user in users)
            {
                var userProducts = new[]
                {
                    Product.Create($"Laptop - {user.Name}", "High-performance laptop for professional use", 1299.99m, "USD", 10, user.Id),
                    Product.Create($"Smartphone - {user.Name}", "Latest smartphone with advanced features", 899.99m, "USD", 25, user.Id),
                    Product.Create($"Headphones - {user.Name}", "Wireless noise-cancelling headphones", 299.99m, "USD", 50, user.Id)
                };

                foreach (var product in userProducts)
                {
                    product.SetCreatedBy("System");
                }

                products.AddRange(userProducts);
            }

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            logger.LogInformation($"Seeded {products.Count} products");
        }
    }
}