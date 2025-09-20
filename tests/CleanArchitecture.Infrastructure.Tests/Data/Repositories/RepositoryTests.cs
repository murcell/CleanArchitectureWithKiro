using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Data.Repositories;

namespace CleanArchitecture.Infrastructure.Tests.Data.Repositories;

public class RepositoryTests : RepositoryTestBase
{
    private readonly Repository<User> _userRepository;
    private readonly Repository<Product> _productRepository;

    public RepositoryTests()
    {
        _userRepository = new Repository<User>(Context);
        _productRepository = new Repository<Product>(Context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _userRepository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _userRepository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        await CreateTestUserAsync("User 1", "user1@example.com");
        await CreateTestUserAsync("User 2", "user2@example.com");
        await CreateTestUserAsync("User 3", "user3@example.com");

        // Act
        var result = await _userRepository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");
        await CreateTestUserAsync("Jane Doe", "jane@example.com");
        await CreateTestUserAsync("Bob Smith", "bob@example.com");

        // Act
        var result = await _userRepository.FindAsync(u => u.Name.Contains("Doe"));

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, u => Assert.Contains("Doe", u.Name));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatchingPredicate_ReturnsEntity()
    {
        // Arrange
        var user = await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.FirstOrDefaultAsync(u => u.Name == "John Doe");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithNonMatchingPredicate_ReturnsNull()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.FirstOrDefaultAsync(u => u.Name == "Jane Smith");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingPredicate_ReturnsTrue()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.AnyAsync(u => u.Name == "John Doe");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_WithNonMatchingPredicate_ReturnsFalse()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.AnyAsync(u => u.Name == "Jane Smith");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CountAsync_WithoutPredicate_ReturnsAllCount()
    {
        // Arrange
        await CreateTestUserAsync("User 1", "user1@example.com");
        await CreateTestUserAsync("User 2", "user2@example.com");

        // Act
        var result = await _userRepository.CountAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsMatchingCount()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");
        await CreateTestUserAsync("Jane Doe", "jane@example.com");
        await CreateTestUserAsync("Bob Smith", "bob@example.com");

        // Act
        var result = await _userRepository.CountAsync(u => u.Name.Contains("Doe"));

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_AddsEntity()
    {
        // Arrange
        var user = User.Create("New User", Email.Create("new@example.com"));

        // Act
        var result = await _userRepository.AddAsync(user);
        await Context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        
        var savedUser = await _userRepository.GetByIdAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("New User", savedUser.Name);
    }

    [Fact]
    public async Task AddAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _userRepository.AddAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_WithValidEntities_AddsAllEntities()
    {
        // Arrange
        var users = new[]
        {
            User.Create("User 1", Email.Create("user1@example.com")),
            User.Create("User 2", Email.Create("user2@example.com")),
            User.Create("User 3", Email.Create("user3@example.com"))
        };

        // Act
        var result = await _userRepository.AddRangeAsync(users);
        await Context.SaveChangesAsync();

        // Assert
        Assert.Equal(3, result.Count());
        
        var allUsers = await _userRepository.GetAllAsync();
        Assert.Equal(3, allUsers.Count());
    }

    [Fact]
    public async Task Update_WithValidEntity_UpdatesEntity()
    {
        // Arrange
        var user = await CreateTestUserAsync("Original Name", "original@example.com");
        user.UpdateName("Updated Name");

        // Act
        _userRepository.Update(user);
        await Context.SaveChangesAsync();

        // Assert
        var updatedUser = await _userRepository.GetByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated Name", updatedUser.Name);
    }

    [Fact]
    public void Update_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _userRepository.Update(null!));
    }

    [Fact]
    public async Task Delete_WithValidEntity_DeletesEntity()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        _userRepository.Delete(user);
        await Context.SaveChangesAsync();

        // Assert
        var deletedUser = await _userRepository.GetByIdAsync(user.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteByIdAsync_WithValidId_DeletesEntity()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        await _userRepository.DeleteByIdAsync(user.Id);
        await Context.SaveChangesAsync();

        // Assert
        var deletedUser = await _userRepository.GetByIdAsync(user.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await CreateTestUserAsync($"User {i}", $"user{i}@example.com");
        }

        // Act
        var result = await _userRepository.GetPagedAsync(
            pageNumber: 2, 
            pageSize: 3, 
            orderBy: u => u.Name);

        // Assert
        Assert.Equal(3, result.Count());
        
        var resultList = result.ToList();
        Assert.Equal("User 3", resultList[0].Name);
        Assert.Equal("User 4", resultList[1].Name);
        Assert.Equal("User 5", resultList[2].Name);
    }

    [Fact]
    public async Task GetPagedAsync_WithPredicate_ReturnsFilteredPagedResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            var name = i % 2 == 0 ? $"Even User {i}" : $"Odd User {i}";
            await CreateTestUserAsync(name, $"user{i}@example.com");
        }

        // Act
        var result = await _userRepository.GetPagedAsync(
            pageNumber: 1, 
            pageSize: 3, 
            predicate: u => u.Name.Contains("Even"),
            orderBy: u => u.Name);

        // Assert
        Assert.Equal(3, result.Count());
        Assert.All(result, u => Assert.Contains("Even", u.Name));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task GetPagedAsync_WithInvalidParameters_ThrowsArgumentException(int pageNumber, int pageSize)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _userRepository.GetPagedAsync(pageNumber, pageSize));
    }
}