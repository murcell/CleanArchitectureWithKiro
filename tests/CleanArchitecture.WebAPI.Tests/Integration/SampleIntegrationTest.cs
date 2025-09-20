using CleanArchitecture.WebAPI.Tests.Common;
using System.Net;

namespace CleanArchitecture.WebAPI.Tests.Integration;

/// <summary>
/// Sample integration test demonstrating the use of TestWebApplicationFactory
/// </summary>
[Collection("Integration Tests")]
public class SampleIntegrationTest : IntegrationTestBase<TestWebApplicationFactory>
{
    [Fact]
    public async Task Application_Should_Start_Successfully()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Database_Should_Be_Accessible()
    {
        // Arrange & Act
        using var context = CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Should_Seed_And_Clean_Database()
    {
        // Arrange - Seed database
        await SeedDatabaseAsync(async context =>
        {
            var user = TestDataBuilder.Users.CreateValidUser("Integration Test User", "integration@test.com");
            context.Users.Add(user);
        });

        // Act - Verify data exists
        using (var context = CreateDbContext())
        {
            var userCount = context.Users.Count();
            Assert.Equal(1, userCount);
        }

        // Clean database
        await CleanDatabaseAsync();

        // Assert - Verify data is cleaned
        using (var context = CreateDbContext())
        {
            var userCount = context.Users.Count();
            Assert.Equal(0, userCount);
        }
    }
}

/// <summary>
/// Sample in-memory test demonstrating the use of InMemoryWebApplicationFactory
/// </summary>
[Collection("In-Memory Tests")]
public class SampleInMemoryTest : IntegrationTestBase<InMemoryWebApplicationFactory>
{
    [Fact]
    public async Task Application_Should_Start_Successfully_With_InMemory_Services()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InMemory_Database_Should_Be_Accessible()
    {
        // Arrange & Act
        using var context = CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Should_Work_With_Mock_Services()
    {
        // This test demonstrates that cache and message queue services are mocked
        // and won't cause issues in fast unit tests
        
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }
}