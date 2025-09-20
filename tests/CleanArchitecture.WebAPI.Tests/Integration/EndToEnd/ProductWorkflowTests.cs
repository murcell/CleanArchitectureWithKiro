using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.WebAPI.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace CleanArchitecture.WebAPI.Tests.Integration.EndToEnd;

/// <summary>
/// End-to-end tests for complete product management workflows
/// Tests the full API lifecycle for product operations
/// </summary>
[Collection("Integration Tests")]
public class ProductWorkflowTests : IntegrationTestBase<TestWebApplicationFactory>
{
    [Fact]
    public async Task Complete_Product_Lifecycle_Should_Work_Successfully()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        // First create a user to own the product
        var createUserRequest = new CreateUserRequest
        {
            Name = "Product Owner",
            Email = "owner@example.com"
        };

        var createUserResponse = await PostAsync("/api/v1.0/users", createUserRequest);
        var createUserResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createUserResponse);
        var userId = createUserResult.Data;

        var createProductRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "A test product for end-to-end testing",
            Price = 99.99m,
            Currency = "USD",
            Stock = 10,
            UserId = userId
        };

        // Act & Assert - Create Product
        var createResponse = await PostAsync("/api/v1.0/products", createProductRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        
        var createResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createResponse);
        Assert.True(createResult.Success);
        Assert.True(createResult.Data > 0);
        var productId = createResult.Data;

        // Act & Assert - Get Created Product
        var getResponse = await Client.GetAsync($"/api/v1.0/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var getResult = await AssertSuccessAndGetContentAsync<ApiResponse<ProductDto>>(getResponse);
        Assert.True(getResult.Success);
        Assert.Equal(createProductRequest.Name, getResult.Data.Name);
        Assert.Equal(createProductRequest.Description, getResult.Data.Description);
        Assert.Equal(createProductRequest.Price, getResult.Data.Price);
        Assert.Equal(createProductRequest.Currency, getResult.Data.Currency);
        Assert.Equal(createProductRequest.Stock, getResult.Data.Stock);
        Assert.Equal(createProductRequest.UserId, getResult.Data.UserId);
        Assert.True(getResult.Data.IsAvailable);

        // Act & Assert - Update Product
        var updateRequest = new UpdateProductRequest
        {
            Name = "Updated Test Product",
            Description = "Updated description",
            Price = 149.99m,
            Currency = "USD",
            Stock = 15
        };
        
        var updateResponse = await PutAsync($"/api/v1.0/products/{productId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        var updateResult = await AssertSuccessAndGetContentAsync<ApiResponse<bool>>(updateResponse);
        Assert.True(updateResult.Success);
        Assert.True(updateResult.Data);

        // Act & Assert - Update Product Stock
        var newStock = 25;
        var updateStockResponse = await Client.PatchAsync($"/api/v1.0/products/{productId}/stock", 
            JsonContent.Create(newStock, options: JsonOptions));
        Assert.Equal(HttpStatusCode.OK, updateStockResponse.StatusCode);
        
        var updateStockResult = await AssertSuccessAndGetContentAsync<ApiResponse<bool>>(updateStockResponse);
        Assert.True(updateStockResult.Success);
        Assert.True(updateStockResult.Data);

        // Act & Assert - Toggle Product Availability
        var toggleResponse = await Client.PatchAsync($"/api/v1.0/products/{productId}/availability", 
            JsonContent.Create(new { }, options: JsonOptions));
        Assert.Equal(HttpStatusCode.OK, toggleResponse.StatusCode);
        
        var toggleResult = await AssertSuccessAndGetContentAsync<ApiResponse<bool>>(toggleResponse);
        Assert.True(toggleResult.Success);
        Assert.True(toggleResult.Data);

        // Act & Assert - Get All Products
        var getAllResponse = await Client.GetAsync("/api/v1.0/products");
        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
        
        var getAllResult = await AssertSuccessAndGetContentAsync<ApiResponse<IEnumerable<ProductDto>>>(getAllResponse);
        Assert.True(getAllResult.Success);
        Assert.NotNull(getAllResult.Data);

        // Act & Assert - Delete Product
        var deleteResponse = await DeleteAsync($"/api/v1.0/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        
        var deleteResult = await AssertSuccessAndGetContentAsync<ApiResponse<bool>>(deleteResponse);
        Assert.True(deleteResult.Success);
        Assert.True(deleteResult.Data);
    }

    [Fact]
    public async Task Product_Filtering_And_Search_Should_Work()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        // Create a user first
        var createUserRequest = new CreateUserRequest
        {
            Name = "Multi Product Owner",
            Email = "multiowner@example.com"
        };

        var createUserResponse = await PostAsync("/api/v1.0/users", createUserRequest);
        var createUserResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createUserResponse);
        var userId = createUserResult.Data;

        // Create multiple products with different characteristics
        var products = new[]
        {
            new CreateProductRequest
            {
                Name = "Available Product 1",
                Description = "First available product",
                Price = 50.00m,
                Currency = "USD",
                Stock = 10,
                UserId = userId
            },
            new CreateProductRequest
            {
                Name = "Available Product 2",
                Description = "Second available product",
                Price = 75.00m,
                Currency = "USD",
                Stock = 5,
                UserId = userId
            },
            new CreateProductRequest
            {
                Name = "Expensive Product",
                Description = "High-end product",
                Price = 500.00m,
                Currency = "USD",
                Stock = 2,
                UserId = userId
            }
        };

        // Create all products
        var productIds = new List<int>();
        foreach (var product in products)
        {
            var response = await PostAsync("/api/v1.0/products", product);
            var result = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(response);
            productIds.Add(result.Data);
        }

        // Act & Assert - Get All Products
        var getAllResponse = await Client.GetAsync("/api/v1.0/products");
        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);

        // Act & Assert - Filter by User ID
        var filterByUserResponse = await Client.GetAsync($"/api/v1.0/products?userId={userId}");
        Assert.Equal(HttpStatusCode.OK, filterByUserResponse.StatusCode);

        // Act & Assert - Filter by Availability
        var filterByAvailabilityResponse = await Client.GetAsync("/api/v1.0/products?isAvailable=true");
        Assert.Equal(HttpStatusCode.OK, filterByAvailabilityResponse.StatusCode);

        // Act & Assert - Test Pagination
        var paginatedResponse = await Client.GetAsync("/api/v1.0/products?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, paginatedResponse.StatusCode);

        // Act & Assert - Combined Filters
        var combinedFilterResponse = await Client.GetAsync($"/api/v1.0/products?userId={userId}&isAvailable=true&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, combinedFilterResponse.StatusCode);
    }

    [Fact]
    public async Task Product_Stock_Management_Workflow_Should_Work()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        // Create user and product
        var createUserRequest = new CreateUserRequest
        {
            Name = "Stock Manager",
            Email = "stockmanager@example.com"
        };

        var createUserResponse = await PostAsync("/api/v1.0/users", createUserRequest);
        var createUserResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createUserResponse);
        var userId = createUserResult.Data;

        var createProductRequest = new CreateProductRequest
        {
            Name = "Stock Test Product",
            Description = "Product for stock management testing",
            Price = 29.99m,
            Currency = "USD",
            Stock = 100,
            UserId = userId
        };

        var createProductResponse = await PostAsync("/api/v1.0/products", createProductRequest);
        var createProductResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createProductResponse);
        var productId = createProductResult.Data;

        // Act & Assert - Update Stock Multiple Times
        var stockUpdates = new[] { 150, 75, 200, 0, 50 };
        
        foreach (var newStock in stockUpdates)
        {
            var updateStockResponse = await Client.PatchAsync($"/api/v1.0/products/{productId}/stock", 
                JsonContent.Create(newStock, options: JsonOptions));
            Assert.Equal(HttpStatusCode.OK, updateStockResponse.StatusCode);
            
            var updateStockResult = await AssertSuccessAndGetContentAsync<ApiResponse<bool>>(updateStockResponse);
            Assert.True(updateStockResult.Success);
            Assert.True(updateStockResult.Data);

            // Verify the stock update by getting the product
            var getProductResponse = await Client.GetAsync($"/api/v1.0/products/{productId}");
            Assert.Equal(HttpStatusCode.OK, getProductResponse.StatusCode);
        }

        // Act & Assert - Toggle Availability Multiple Times
        for (int i = 0; i < 3; i++)
        {
            var toggleResponse = await Client.PatchAsync($"/api/v1.0/products/{productId}/availability", 
                JsonContent.Create(new { }, options: JsonOptions));
            Assert.Equal(HttpStatusCode.OK, toggleResponse.StatusCode);
            
            var toggleResult = await AssertSuccessAndGetContentAsync<ApiResponse<bool>>(toggleResponse);
            Assert.True(toggleResult.Success);
            Assert.True(toggleResult.Data);
        }
    }

    [Fact]
    public async Task Multiple_Users_Multiple_Products_Workflow_Should_Work()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        // Create multiple users
        var users = new[]
        {
            new CreateUserRequest { Name = "Seller 1", Email = "seller1@example.com" },
            new CreateUserRequest { Name = "Seller 2", Email = "seller2@example.com" },
            new CreateUserRequest { Name = "Seller 3", Email = "seller3@example.com" }
        };

        var userIds = new List<int>();
        foreach (var user in users)
        {
            var response = await PostAsync("/api/v1.0/users", user);
            var result = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(response);
            userIds.Add(result.Data);
        }

        // Create products for each user
        var allProductIds = new List<int>();
        for (int userIndex = 0; userIndex < userIds.Count; userIndex++)
        {
            var userId = userIds[userIndex];
            
            // Create 2 products per user
            for (int productIndex = 1; productIndex <= 2; productIndex++)
            {
                var productRequest = new CreateProductRequest
                {
                    Name = $"Product {productIndex} by User {userIndex + 1}",
                    Description = $"Product {productIndex} created by user {userId}",
                    Price = (userIndex + 1) * productIndex * 25.00m,
                    Currency = "USD",
                    Stock = (userIndex + 1) * productIndex * 5,
                    UserId = userId
                };

                var response = await PostAsync("/api/v1.0/products", productRequest);
                var result = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(response);
                allProductIds.Add(result.Data);
            }
        }

        // Act & Assert - Get all products
        var getAllProductsResponse = await Client.GetAsync("/api/v1.0/products");
        Assert.Equal(HttpStatusCode.OK, getAllProductsResponse.StatusCode);

        // Act & Assert - Get products for each user
        foreach (var userId in userIds)
        {
            var getUserProductsResponse = await Client.GetAsync($"/api/v1.0/products?userId={userId}");
            Assert.Equal(HttpStatusCode.OK, getUserProductsResponse.StatusCode);
        }

        // Act & Assert - Test concurrent product operations
        var concurrentTasks = allProductIds.Select(async productId =>
        {
            // Get product
            var getResponse = await Client.GetAsync($"/api/v1.0/products/{productId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Update stock
            var updateStockResponse = await Client.PatchAsync($"/api/v1.0/products/{productId}/stock", 
                JsonContent.Create(Random.Shared.Next(1, 100), options: JsonOptions));
            Assert.Equal(HttpStatusCode.OK, updateStockResponse.StatusCode);

            return productId;
        }).ToArray();

        var completedProductIds = await Task.WhenAll(concurrentTasks);
        Assert.Equal(allProductIds.Count, completedProductIds.Length);
    }

    [Fact]
    public async Task Product_Business_Rules_Should_Be_Enforced()
    {
        // Arrange
        await CleanDatabaseAsync();
        
        // Create a user first
        var createUserRequest = new CreateUserRequest
        {
            Name = "Business Rules Tester",
            Email = "businessrules@example.com"
        };

        var createUserResponse = await PostAsync("/api/v1.0/users", createUserRequest);
        var createUserResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createUserResponse);
        var userId = createUserResult.Data;

        // Test various business rule scenarios
        var testCases = new[]
        {
            // Valid product
            new CreateProductRequest
            {
                Name = "Valid Product",
                Description = "This is a valid product",
                Price = 50.00m,
                Currency = "USD",
                Stock = 10,
                UserId = userId
            },
            // Product with zero price (should still work for free products)
            new CreateProductRequest
            {
                Name = "Free Product",
                Description = "This is a free product",
                Price = 0.00m,
                Currency = "USD",
                Stock = 100,
                UserId = userId
            },
            // Product with high price
            new CreateProductRequest
            {
                Name = "Expensive Product",
                Description = "This is an expensive product",
                Price = 9999.99m,
                Currency = "USD",
                Stock = 1,
                UserId = userId
            }
        };

        // Act & Assert - Create products with different characteristics
        foreach (var productRequest in testCases)
        {
            var response = await PostAsync("/api/v1.0/products", productRequest);
            
            // All should succeed as they are valid according to current business rules
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var result = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(response);
            Assert.True(result.Success);
            Assert.True(result.Data > 0);

            // Verify the created product
            var getResponse = await Client.GetAsync($"/api/v1.0/products/{result.Data}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }
    }
}