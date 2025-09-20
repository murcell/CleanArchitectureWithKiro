# Integration Test Infrastructure

This directory contains the integration test infrastructure for the Clean Architecture project. The infrastructure supports both container-based and in-memory testing approaches.

## Test Factories

### TestWebApplicationFactory
- Uses real test containers (SQL Server, Redis, RabbitMQ)
- Provides full integration testing with actual external dependencies
- Slower but more realistic testing environment
- Automatically manages container lifecycle

### InMemoryWebApplicationFactory
- Uses in-memory database and mock services
- Fast execution for unit-style integration tests
- Suitable for testing application logic without external dependencies

## Usage

### Container-based Integration Tests

```csharp
[Collection("Integration Tests")]
public class MyIntegrationTest : IntegrationTestBase<TestWebApplicationFactory>
{
    [Fact]
    public async Task Should_Test_With_Real_Dependencies()
    {
        // Test implementation
    }
}
```

### In-Memory Integration Tests

```csharp
[Collection("In-Memory Tests")]
public class MyFastTest : IntegrationTestBase<InMemoryWebApplicationFactory>
{
    [Fact]
    public async Task Should_Test_With_Mock_Dependencies()
    {
        // Test implementation
    }
}
```

## Test Data Management

Use the `TestDataBuilder` class to create consistent test data:

```csharp
// Create test users
var user = TestDataBuilder.Users.CreateValidUser("John Doe", "john@example.com");
var users = TestDataBuilder.Users.CreateMultipleUsers(5);

// Create test products
var product = TestDataBuilder.Products.CreateValidProduct("Test Product", 99.99m);
var products = TestDataBuilder.Products.CreateMultipleProducts(10);
```

## Database Operations

### Seeding Data

```csharp
await SeedDatabaseAsync(async context =>
{
    var user = TestDataBuilder.Users.CreateValidUser();
    context.Users.Add(user);
});
```

### Cleaning Data

```csharp
await CleanDatabaseAsync();
```

### Direct Database Access

```csharp
using var context = CreateDbContext();
var users = await context.Users.ToListAsync();
```

## HTTP Client Operations

The base class provides convenient methods for HTTP operations:

```csharp
// GET request with deserialization
var user = await GetAsync<UserDto>("/api/users/1");

// POST request
var response = await PostAsync("/api/users", createUserRequest);

// PUT request
var response = await PutAsync("/api/users/1", updateUserRequest);

// DELETE request
var response = await DeleteAsync("/api/users/1");

// Assert success and get content
var user = await AssertSuccessAndGetContentAsync<UserDto>(response);
```

## Environment Variables

Control test behavior with environment variables:

- `USE_REAL_CONTAINERS=true`: Force use of real containers even for fast tests
- `RUN_SLOW_TESTS=true`: Enable slow/long-running tests
- `TEST_DATABASE_NAME`: Override test database name
- `TEST_TIMEOUT_SECONDS`: Set test timeout (default: 30 seconds)

## Docker Support

### Test Containers (Automatic)
The `TestWebApplicationFactory` automatically manages test containers using the Testcontainers library.

### Manual Docker Setup
For manual container management, use the test Docker Compose file:

```bash
# Start test containers
docker-compose -f docker/docker-compose.test.yml up -d

# Stop test containers
docker-compose -f docker/docker-compose.test.yml down
```

## Test Organization

- **Integration Tests**: Full end-to-end tests with real dependencies
- **In-Memory Tests**: Fast tests with mocked external services
- **Common**: Shared test infrastructure and utilities

## Best Practices

1. Use `InMemoryWebApplicationFactory` for fast unit-style tests
2. Use `TestWebApplicationFactory` for full integration tests
3. Always clean up test data between tests
4. Use test collections to manage resource sharing
5. Leverage `TestDataBuilder` for consistent test data
6. Use environment variables to control test execution
7. Keep integration tests focused and independent

## Performance Considerations

- Container-based tests are slower but more realistic
- In-memory tests are faster but may miss integration issues
- Use test collections to share expensive resources
- Consider parallel test execution settings
- Clean up resources properly to avoid memory leaks

## Troubleshooting

### Container Issues
- Ensure Docker is running
- Check port conflicts (test containers use different ports)
- Verify container health checks are passing

### Database Issues
- Check connection strings in test configuration
- Ensure test database permissions
- Verify Entity Framework migrations

### Performance Issues
- Use in-memory tests for fast feedback
- Limit container-based tests to critical scenarios
- Consider test parallelization settings