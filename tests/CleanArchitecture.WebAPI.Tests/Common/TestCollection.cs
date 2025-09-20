namespace CleanArchitecture.WebAPI.Tests.Common;

/// <summary>
/// Test collection for integration tests using test containers
/// This ensures that test containers are shared across test classes and properly disposed
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Test collection for fast in-memory tests
/// </summary>
[CollectionDefinition("In-Memory Tests")]
public class InMemoryTestCollection : ICollectionFixture<InMemoryWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}