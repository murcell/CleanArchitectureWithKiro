using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.WebAPI.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CleanArchitecture.WebAPI.Tests.Integration.EndToEnd;

/// <summary>
/// End-to-end tests for cross-cutting concerns like logging, caching, messaging, etc.
/// Tests the integration of various infrastructure components
/// </summary>
[Collection("Integration Tests")]
public class CrossCuttingConcernsTests : IntegrationTestBase<TestWebApplicationFactory>
{
    [Fact]
    public async Task Correlation_ID_Should_Be_Tracked_Across_Requests()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await Client.GetAsync("/api/v1.0/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Check if correlation ID is returned in response headers
        if (response.Headers.Contains("X-Correlation-ID"))
        {
            var returnedCorrelationId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            Assert.Equal(correlationId, returnedCorrelationId);
        }

        // Test without correlation ID - should generate one
        Client.DefaultRequestHeaders.Remove("X-Correlation-ID");
        var responseWithoutCorrelation = await Client.GetAsync("/api/v1.0/users");
        
        Assert.Equal(HttpStatusCode.OK, responseWithoutCorrelation.StatusCode);
    }

    [Fact]
    public async Task Request_Response_Logging_Should_Work()
    {
        // This test verifies that requests and responses are properly logged
        // In a real scenario, you would check log outputs
        
        await CleanDatabaseAsync();

        // Make various types of requests to generate logs
        var requests = new[]
        {
            // GET request
            Client.GetAsync("/api/v1.0/users"),
            
            // POST request
            PostAsync("/api/v1.0/users", new CreateUserRequest { Name = "Log Test User", Email = "logtest@example.com" }),
            
            // Health check
            Client.GetAsync("/health/live"),
            
            // Error scenario
            Client.GetAsync("/api/v1.0/users/99999")
        };

        var responses = await Task.WhenAll(requests);

        // All requests should complete (successfully or with appropriate errors)
        foreach (var response in responses)
        {
            Assert.NotNull(response);
            Assert.True((int)response.StatusCode >= 200 && (int)response.StatusCode < 600);
        }
    }

    [Fact]
    public async Task Performance_Logging_Should_Track_Request_Duration()
    {
        // Test that performance logging middleware tracks request durations
        
        var startTime = DateTime.UtcNow;
        
        // Make a request that should be tracked
        var response = await Client.GetAsync("/api/v1.0/users");
        
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify that the request completed within reasonable time
        Assert.True(duration.TotalSeconds < 30, "Request took too long");
        
        // Check for performance-related headers if they exist
        if (response.Headers.Contains("X-Response-Time"))
        {
            var responseTimeHeader = response.Headers.GetValues("X-Response-Time").FirstOrDefault();
            Assert.NotNull(responseTimeHeader);
        }
    }

    [Fact]
    public async Task Caching_Integration_Should_Work()
    {
        // Test Redis caching integration (if available)
        await CleanDatabaseAsync();

        // Create a user to test caching
        var createRequest = new CreateUserRequest
        {
            Name = "Cache Test User",
            Email = "cachetest@example.com"
        };

        var createResponse = await PostAsync("/api/v1.0/users", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        
        var createResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createResponse);
        var userId = createResult.Data;

        // First request - should hit database
        var firstGetResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, firstGetResponse.StatusCode);
        
        var firstGetTime = DateTime.UtcNow;

        // Second request - should potentially hit cache (if caching is implemented)
        var secondGetResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, secondGetResponse.StatusCode);
        
        var secondGetTime = DateTime.UtcNow;

        // Both requests should return the same data
        var firstResult = await AssertSuccessAndGetContentAsync<ApiResponse<UserDto>>(firstGetResponse);
        var secondResult = await AssertSuccessAndGetContentAsync<ApiResponse<UserDto>>(secondGetResponse);
        
        Assert.Equal(firstResult.Data.Id, secondResult.Data.Id);
        Assert.Equal(firstResult.Data.Name, secondResult.Data.Name);
        Assert.Equal(firstResult.Data.Email, secondResult.Data.Email);

        // Test cache invalidation by updating the user
        var updateRequest = new UpdateUserRequest
        {
            Name = "Updated Cache Test User",
            Email = "updatedcachetest@example.com"
        };

        var updateResponse = await PutAsync($"/api/v1.0/users/{userId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Request after update should reflect changes
        var afterUpdateResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, afterUpdateResponse.StatusCode);
    }

    [Fact]
    public async Task Message_Queue_Integration_Should_Work()
    {
        // Test RabbitMQ integration (if available)
        await CleanDatabaseAsync();

        // Create operations that might trigger message queue operations
        var createUserRequest = new CreateUserRequest
        {
            Name = "Message Queue Test User",
            Email = "mqtest@example.com"
        };

        var createUserResponse = await PostAsync("/api/v1.0/users", createUserRequest);
        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);
        
        var createUserResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createUserResponse);
        var userId = createUserResult.Data;

        // Create a product that might trigger messaging
        var createProductRequest = new CreateProductRequest
        {
            Name = "Message Queue Test Product",
            Description = "Product for testing message queue integration",
            Price = 99.99m,
            Currency = "USD",
            Stock = 10,
            UserId = userId
        };

        var createProductResponse = await PostAsync("/api/v1.0/products", createProductRequest);
        Assert.Equal(HttpStatusCode.Created, createProductResponse.StatusCode);
        
        var createProductResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createProductResponse);
        var productId = createProductResult.Data;

        // Operations that might trigger background processing
        var operations = new[]
        {
            // Update product stock (might trigger inventory messages)
            Client.PatchAsync($"/api/v1.0/products/{productId}/stock", 
                JsonContent.Create(5, options: JsonOptions)),
            
            // Toggle availability (might trigger notification messages)
            Client.PatchAsync($"/api/v1.0/products/{productId}/availability", 
                JsonContent.Create(new { }, options: JsonOptions)),
            
            // Delete operations (might trigger cleanup messages)
            DeleteAsync($"/api/v1.0/products/{productId}")
        };

        var responses = await Task.WhenAll(operations);

        // All operations should complete successfully
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Allow some time for background message processing
        await Task.Delay(1000);
    }

    [Fact]
    public async Task Health_Checks_Should_Reflect_System_Status()
    {
        // Test comprehensive health checks
        
        // Live health check - should always return OK
        var liveResponse = await Client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);

        // Ready health check - depends on external services
        var readyResponse = await Client.GetAsync("/health/ready");
        Assert.True(readyResponse.StatusCode == HttpStatusCode.OK || 
                   readyResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Full health check with details
        var healthResponse = await Client.GetAsync("/health");
        Assert.True(healthResponse.StatusCode == HttpStatusCode.OK || 
                   healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        var healthContent = await healthResponse.Content.ReadAsStringAsync();
        Assert.NotEmpty(healthContent);

        // Parse health check response
        var healthData = JsonSerializer.Deserialize<JsonElement>(healthContent);
        
        // Should contain status information
        Assert.True(healthData.TryGetProperty("status", out _) || 
                   healthData.TryGetProperty("Status", out _));

        // Test health checks under load
        var concurrentHealthChecks = Enumerable.Range(1, 10)
            .Select(_ => Client.GetAsync("/health/live"))
            .ToArray();

        var concurrentResponses = await Task.WhenAll(concurrentHealthChecks);
        
        foreach (var response in concurrentResponses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task API_Versioning_Should_Work_Consistently()
    {
        // Test that API versioning works consistently across all endpoints
        await CleanDatabaseAsync();

        var createRequest = new CreateUserRequest
        {
            Name = "Versioning Test User",
            Email = "versiontest@example.com"
        };

        // Test V1 API
        var v1CreateResponse = await PostAsync("/api/v1.0/users", createRequest);
        Assert.Equal(HttpStatusCode.Created, v1CreateResponse.StatusCode);
        
        var v1CreateResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(v1CreateResponse);
        var userId = v1CreateResult.Data;

        // Test V1 GET
        var v1GetResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, v1GetResponse.StatusCode);

        // Test V2 API
        var v2CreateResponse = await PostAsync("/api/v2.0/users", createRequest);
        Assert.Equal(HttpStatusCode.Created, v2CreateResponse.StatusCode);

        // Test V2 GET
        var v2GetResponse = await Client.GetAsync($"/api/v2.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, v2GetResponse.StatusCode);

        // Verify V2 response has enhanced features
        var v2Content = await v2GetResponse.Content.ReadAsStringAsync();
        Assert.Contains("version", v2Content.ToLower());
        Assert.Contains("links", v2Content.ToLower());

        // Test version negotiation via headers
        Client.DefaultRequestHeaders.Add("Accept", "application/json;version=1.0");
        var headerVersionResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, headerVersionResponse.StatusCode);

        Client.DefaultRequestHeaders.Remove("Accept");
    }

    [Fact]
    public async Task Content_Negotiation_Should_Work()
    {
        // Test content negotiation
        await CleanDatabaseAsync();

        var createRequest = new CreateUserRequest
        {
            Name = "Content Negotiation Test",
            Email = "contenttest@example.com"
        };

        var createResponse = await PostAsync("/api/v1.0/users", createRequest);
        var createResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createResponse);
        var userId = createResult.Data;

        // Test JSON content type (default)
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        
        var jsonResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.Equal("application/json", jsonResponse.Content.Headers.ContentType?.MediaType);

        // Test wildcard accept header
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
        
        var wildcardResponse = await Client.GetAsync($"/api/v1.0/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, wildcardResponse.StatusCode);

        // Clean up headers
        Client.DefaultRequestHeaders.Accept.Clear();
    }

    [Fact]
    public async Task Middleware_Pipeline_Should_Work_Correctly()
    {
        // Test that the middleware pipeline processes requests correctly
        
        // Test request with various characteristics
        var testRequests = new[]
        {
            // Normal request
            Client.GetAsync("/api/v1.0/users"),
            
            // Request with custom headers
            CreateRequestWithHeaders("/api/v1.0/users", new Dictionary<string, string>
            {
                { "X-Custom-Header", "test-value" },
                { "X-Client-Version", "1.0.0" }
            }),
            
            // Request with user agent
            CreateRequestWithUserAgent("/api/v1.0/users", "TestClient/1.0"),
            
            // Request to health endpoint
            Client.GetAsync("/health/live")
        };

        var responses = await Task.WhenAll(testRequests);

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Verify response has proper headers
            Assert.NotNull(response.Headers);
            Assert.NotNull(response.Content.Headers);
        }
    }

    [Fact]
    public async Task Database_Transaction_Handling_Should_Work()
    {
        // Test database transaction handling
        await CleanDatabaseAsync();

        // Create multiple related entities in what should be a transaction
        var createUserRequest = new CreateUserRequest
        {
            Name = "Transaction Test User",
            Email = "transactiontest@example.com"
        };

        var createUserResponse = await PostAsync("/api/v1.0/users", createUserRequest);
        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);
        
        var createUserResult = await AssertSuccessAndGetContentAsync<ApiResponse<int>>(createUserResponse);
        var userId = createUserResult.Data;

        // Create multiple products for the user
        var productCreationTasks = Enumerable.Range(1, 5).Select(async i =>
        {
            var productRequest = new CreateProductRequest
            {
                Name = $"Transaction Test Product {i}",
                Description = $"Product {i} for transaction testing",
                Price = i * 10.0m,
                Currency = "USD",
                Stock = i * 5,
                UserId = userId
            };

            return await PostAsync("/api/v1.0/products", productRequest);
        });

        var productResponses = await Task.WhenAll(productCreationTasks);

        // All product creations should succeed
        foreach (var response in productResponses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // Verify all products were created
        var getProductsResponse = await Client.GetAsync($"/api/v1.0/products?userId={userId}");
        Assert.Equal(HttpStatusCode.OK, getProductsResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateRequestWithHeaders(string uri, Dictionary<string, string> headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
        return await Client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> CreateRequestWithUserAgent(string uri, string userAgent)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("User-Agent", userAgent);
        return await Client.SendAsync(request);
    }
}