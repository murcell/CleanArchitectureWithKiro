using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.WebAPI.Tests.Common;

/// <summary>
/// Builder class for creating test data
/// </summary>
public static class TestDataBuilder
{
    public static class Users
    {
        public static User CreateValidUser(string? name = null, string? email = null)
        {
            return User.Create(
                name ?? "Test User",
                email ?? "test@example.com"
            );
        }

        public static User CreateUserWithId(int id, string? name = null, string? email = null)
        {
            var user = CreateValidUser(name, email);
            // Use reflection to set the ID for testing purposes
            typeof(User).GetProperty("Id")?.SetValue(user, id);
            return user;
        }

        public static List<User> CreateMultipleUsers(int count)
        {
            var users = new List<User>();
            for (int i = 1; i <= count; i++)
            {
                users.Add(CreateValidUser($"User {i}", $"user{i}@example.com"));
            }
            return users;
        }
    }

    public static class Products
    {
        public static Product CreateValidProduct(string? name = null, decimal? price = null, int? userId = null)
        {
            return Product.Create(
                name ?? "Test Product",
                Money.Create(price ?? 99.99m, "USD"),
                userId ?? 1
            );
        }

        public static Product CreateProductWithId(int id, string? name = null, decimal? price = null, int? userId = null)
        {
            var product = CreateValidProduct(name, price, userId);
            // Use reflection to set the ID for testing purposes
            typeof(Product).GetProperty("Id")?.SetValue(product, id);
            return product;
        }

        public static List<Product> CreateMultipleProducts(int count, int? userId = null)
        {
            var products = new List<Product>();
            for (int i = 1; i <= count; i++)
            {
                products.Add(CreateValidProduct($"Product {i}", i * 10.0m, userId ?? 1));
            }
            return products;
        }
    }
}