using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;

namespace CleanArchitecture.WebAPI.Tests.Common;

/// <summary>
/// Base class for integration tests providing common functionality
/// </summary>
public abstract class IntegrationTestBase<TFactory> : IAsyncLifetime
    where TFactory : class, IAsyncLifetime, new()
{
    protected TFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public virtual async Task InitializeAsync()
    {
        Factory = new TFactory();
        await Factory.InitializeAsync();
        
        if (Factory is TestWebApplicationFactory testFactory)
        {
            Client = testFactory.CreateClient();
        }
        else if (Factory is InMemoryWebApplicationFactory inMemoryFactory)
        {
            Client = inMemoryFactory.CreateClient();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported factory type: {typeof(TFactory)}");
        }
    }

    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        var scope = GetServiceScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service scope for accessing scoped services
    /// </summary>
    protected IServiceScope GetServiceScope()
    {
        if (Factory is TestWebApplicationFactory testFactory)
        {
            return testFactory.Services.CreateScope();
        }
        else if (Factory is InMemoryWebApplicationFactory inMemoryFactory)
        {
            return inMemoryFactory.Services.CreateScope();
        }
        
        throw new InvalidOperationException($"Unsupported factory type: {typeof(TFactory)}");
    }

    /// <summary>
    /// Creates a database context for direct database operations
    /// </summary>
    protected ApplicationDbContext CreateDbContext()
    {
        if (Factory is TestWebApplicationFactory testFactory)
        {
            return testFactory.CreateDbContext();
        }
        else if (Factory is InMemoryWebApplicationFactory inMemoryFactory)
        {
            return inMemoryFactory.CreateDbContext();
        }
        
        throw new InvalidOperationException($"Unsupported factory type: {typeof(TFactory)}");
    }

    /// <summary>
    /// Seeds the test database with initial data
    /// </summary>
    protected async Task SeedDatabaseAsync(Func<ApplicationDbContext, Task> seedAction)
    {
        if (Factory is TestWebApplicationFactory testFactory)
        {
            await testFactory.SeedDatabaseAsync(seedAction);
        }
        else if (Factory is InMemoryWebApplicationFactory inMemoryFactory)
        {
            await inMemoryFactory.SeedDatabaseAsync(seedAction);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported factory type: {typeof(TFactory)}");
        }
    }

    /// <summary>
    /// Cleans the test database
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        if (Factory is TestWebApplicationFactory testFactory)
        {
            await testFactory.CleanDatabaseAsync();
        }
        else if (Factory is InMemoryWebApplicationFactory inMemoryFactory)
        {
            await inMemoryFactory.CleanDatabaseAsync();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported factory type: {typeof(TFactory)}");
        }
    }

    /// <summary>
    /// Sends a GET request and deserializes the response
    /// </summary>
    protected async Task<T?> GetAsync<T>(string requestUri)
    {
        var response = await Client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// Sends a POST request with JSON content
    /// </summary>
    protected async Task<HttpResponseMessage> PostAsync<T>(string requestUri, T content)
    {
        return await Client.PostAsJsonAsync(requestUri, content, JsonOptions);
    }

    /// <summary>
    /// Sends a PUT request with JSON content
    /// </summary>
    protected async Task<HttpResponseMessage> PutAsync<T>(string requestUri, T content)
    {
        return await Client.PutAsJsonAsync(requestUri, content, JsonOptions);
    }

    /// <summary>
    /// Sends a DELETE request
    /// </summary>
    protected async Task<HttpResponseMessage> DeleteAsync(string requestUri)
    {
        return await Client.DeleteAsync(requestUri);
    }

    /// <summary>
    /// Asserts that a response is successful and returns the deserialized content
    /// </summary>
    protected async Task<T> AssertSuccessAndGetContentAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<T>(json, JsonOptions);
        
        Assert.NotNull(content);
        return content;
    }
}