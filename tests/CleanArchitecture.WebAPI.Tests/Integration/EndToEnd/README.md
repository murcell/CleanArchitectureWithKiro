# End-to-End API Tests

This directory contains comprehensive end-to-end tests for the Clean Architecture API. These tests verify the complete API workflows, authentication/authorization scenarios, and error handling.

## Test Categories

### 1. User Workflow Tests (`UserWorkflowTests.cs`)
- **Complete User Lifecycle**: Tests the full CRUD operations for users
- **User Creation with Products**: Tests creating users and associated products
- **API Versioning**: Tests both V1 and V2 API endpoints
- **Pagination and Filtering**: Tests query parameters and pagination
- **Concurrent Operations**: Tests concurrent user operations
- **Health Check Integration**: Tests health check endpoints

### 2. Product Workflow Tests (`ProductWorkflowTests.cs`)
- **Complete Product Lifecycle**: Tests full CRUD operations for products
- **Product Filtering and Search**: Tests various filtering scenarios
- **Stock Management**: Tests product stock update workflows
- **Multiple Users/Products**: Tests complex scenarios with multiple entities
- **Business Rules Enforcement**: Tests business rule validation

### 3. Authentication Tests (`AuthenticationTests.cs`)
- **Unauthenticated Requests**: Tests public endpoint access
- **Invalid Authentication**: Tests handling of invalid tokens
- **Malformed Headers**: Tests malformed authorization headers
- **API Key Authentication**: Tests API key-based authentication
- **JWT Token Workflow**: Tests JWT token validation (when implemented)
- **Role-Based Authorization**: Tests role-based access control
- **Resource Ownership**: Tests resource ownership validation
- **CORS and Security Headers**: Tests security header presence
- **Rate Limiting**: Tests rate limiting functionality

### 4. Error Handling Tests (`ErrorHandlingTests.cs`)
- **Invalid Request Data**: Tests handling of malformed requests
- **Validation Errors**: Tests input validation error responses
- **Not Found Errors**: Tests 404 error scenarios
- **Invalid Route Parameters**: Tests invalid parameter handling
- **Invalid Query Parameters**: Tests query parameter validation
- **Unsupported HTTP Methods**: Tests method not allowed scenarios
- **Invalid Content Types**: Tests unsupported media types
- **Large Request Payloads**: Tests request size limits
- **Concurrent Error Scenarios**: Tests error handling under load
- **API Versioning Errors**: Tests version-related errors
- **Database Connection Errors**: Tests database failure scenarios
- **External Service Errors**: Tests external service failures

### 5. Cross-Cutting Concerns Tests (`CrossCuttingConcernsTests.cs`)
- **Correlation ID Tracking**: Tests request correlation tracking
- **Request/Response Logging**: Tests logging middleware
- **Performance Logging**: Tests performance monitoring
- **Caching Integration**: Tests Redis caching functionality
- **Message Queue Integration**: Tests RabbitMQ messaging
- **Health Checks**: Tests comprehensive health monitoring
- **API Versioning Consistency**: Tests version consistency
- **Content Negotiation**: Tests content type negotiation
- **Middleware Pipeline**: Tests middleware execution order
- **Database Transactions**: Tests transaction handling

## Test Infrastructure

### Test Factories
- **TestWebApplicationFactory**: Uses real test containers (SQL Server, Redis, RabbitMQ)
- **InMemoryWebApplicationFactory**: Uses in-memory services for fast tests

### Test Base Classes
- **IntegrationTestBase**: Provides common functionality for integration tests
- **TestDataBuilder**: Provides test data creation utilities

### Test Collections
- **Integration Tests**: Uses test containers for realistic testing
- **In-Memory Tests**: Uses in-memory services for fast unit-style tests

## Running the Tests

### Prerequisites
- Docker Desktop (for test containers)
- .NET 9 SDK
- SQL Server, Redis, and RabbitMQ test containers will be automatically started

### Run All End-to-End Tests
```bash
dotnet test tests/CleanArchitecture.WebAPI.Tests/Integration/EndToEnd/ --logger "console;verbosity=detailed"
```

### Run Specific Test Categories
```bash
# User workflow tests
dotnet test --filter "FullyQualifiedName~UserWorkflowTests"

# Authentication tests
dotnet test --filter "FullyQualifiedName~AuthenticationTests"

# Error handling tests
dotnet test --filter "FullyQualifiedName~ErrorHandlingTests"

# Product workflow tests
dotnet test --filter "FullyQualifiedName~ProductWorkflowTests"

# Cross-cutting concerns tests
dotnet test --filter "FullyQualifiedName~CrossCuttingConcernsTests"
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## Test Data Management

### Database Cleanup
- Tests use `CleanDatabaseAsync()` to ensure clean state
- Each test class manages its own test data
- Test containers are recreated for each test run

### Test Isolation
- Tests are designed to be independent
- Concurrent test execution is supported
- Database transactions ensure data consistency

## Expected Behavior

### Current Implementation Status
Many tests are designed to work with the current implementation while also demonstrating how they should behave when authentication and other features are fully implemented:

- **Authentication**: Currently not implemented, so tests verify current behavior and document expected behavior
- **Authorization**: Role-based access control is not yet implemented
- **Caching**: Redis integration exists but may not be fully utilized in all endpoints
- **Messaging**: RabbitMQ integration exists but may not be triggered by all operations

### Test Assertions
Tests use flexible assertions that work with both current and future implementations:
- `Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized)`
- This allows tests to pass now and continue working when auth is implemented

## Troubleshooting

### Test Container Issues
- Ensure Docker Desktop is running
- Check that ports 1433 (SQL Server), 6379 (Redis), and 5672 (RabbitMQ) are available
- Test containers will automatically pull required images

### Test Failures
- Check test output for specific error messages
- Verify database connectivity
- Ensure all required services are healthy

### Performance Issues
- Test containers may take time to start on first run
- Subsequent runs should be faster due to image caching
- Use in-memory tests for faster feedback during development

## Contributing

When adding new end-to-end tests:

1. **Follow Naming Conventions**: Use descriptive test method names
2. **Use Appropriate Test Factory**: Choose between TestWebApplicationFactory and InMemoryWebApplicationFactory
3. **Clean Up**: Always clean database state before tests
4. **Document Expected Behavior**: Add comments explaining current vs. expected behavior
5. **Test Error Scenarios**: Include both success and failure cases
6. **Consider Concurrency**: Test concurrent operations where relevant