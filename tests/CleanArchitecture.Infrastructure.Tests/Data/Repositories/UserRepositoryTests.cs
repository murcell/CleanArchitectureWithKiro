using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Data.Repositories;

namespace CleanArchitecture.Infrastructure.Tests.Data.Repositories;

public class UserRepositoryTests : RepositoryTestBase
{
    private readonly UserRepository _userRepository;

    public UserRepositoryTests()
    {
        _userRepository = new UserRepository(Context);
    }

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.GetByEmailAsync("john@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("john@example.com", result.Email.Value);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetByEmailAsync_WithInvalidEmail_ThrowsArgumentException(string email)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userRepository.GetByEmailAsync(email));
    }

    [Fact]
    public async Task GetByUsernameAsync_WithValidUsername_ReturnsUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("JohnDoe", "john@example.com");

        // Act
        var result = await _userRepository.GetByUsernameAsync("JohnDoe");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("JohnDoe", result.Name);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistentUsername_ReturnsNull()
    {
        // Arrange
        await CreateTestUserAsync("JohnDoe", "john@example.com");

        // Act
        var result = await _userRepository.GetByUsernameAsync("JaneDoe");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetByUsernameAsync_WithInvalidUsername_ThrowsArgumentException(string username)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userRepository.GetByUsernameAsync(username));
    }

    [Fact]
    public async Task IsEmailTakenAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.IsEmailTakenAsync("john@example.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEmailTakenAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Arrange
        await CreateTestUserAsync("John Doe", "john@example.com");

        // Act
        var result = await _userRepository.IsEmailTakenAsync("jane@example.com");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task IsEmailTakenAsync_WithInvalidEmail_ReturnsFalse(string email)
    {
        // Act
        var result = await _userRepository.IsEmailTakenAsync(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUsernameTakenAsync_WithExistingUsername_ReturnsTrue()
    {
        // Arrange
        await CreateTestUserAsync("JohnDoe", "john@example.com");

        // Act
        var result = await _userRepository.IsUsernameTakenAsync("JohnDoe");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUsernameTakenAsync_WithNonExistentUsername_ReturnsFalse()
    {
        // Arrange
        await CreateTestUserAsync("JohnDoe", "john@example.com");

        // Act
        var result = await _userRepository.IsUsernameTakenAsync("JaneDoe");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task IsUsernameTakenAsync_WithInvalidUsername_ReturnsFalse(string username)
    {
        // Act
        var result = await _userRepository.IsUsernameTakenAsync(username);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetActiveUsersAsync_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var activeUser1 = await CreateTestUserAsync("Active User 1", "active1@example.com");
        var activeUser2 = await CreateTestUserAsync("Active User 2", "active2@example.com");
        var inactiveUser = await CreateTestUserAsync("Inactive User", "inactive@example.com");
        
        // Deactivate one user
        inactiveUser.Deactivate();
        Context.Users.Update(inactiveUser);
        await Context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetActiveUsersAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, u => Assert.True(u.IsActive));
        Assert.Contains(result, u => u.Id == activeUser1.Id);
        Assert.Contains(result, u => u.Id == activeUser2.Id);
        Assert.DoesNotContain(result, u => u.Id == inactiveUser.Id);
    }

    [Fact]
    public async Task GetUsersWithProductsAsync_ReturnsUsersWithProducts()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("User 1", "user1@example.com");
        var user2 = await CreateTestUserAsync("User 2", "user2@example.com");
        
        await CreateTestProductAsync("Product 1", 100.00m, user1.Id);
        await CreateTestProductAsync("Product 2", 200.00m, user1.Id);
        await CreateTestProductAsync("Product 3", 300.00m, user2.Id);

        // Act
        var result = await _userRepository.GetUsersWithProductsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        
        var user1Result = result.First(u => u.Id == user1.Id);
        var user2Result = result.First(u => u.Id == user2.Id);
        
        Assert.Equal(2, user1Result.Products.Count);
        Assert.Equal(1, user2Result.Products.Count);
    }

    [Fact]
    public async Task GetUsersWithProductsAsync_WithNoProducts_ReturnsUsersWithEmptyProductCollections()
    {
        // Arrange
        await CreateTestUserAsync("User 1", "user1@example.com");
        await CreateTestUserAsync("User 2", "user2@example.com");

        // Act
        var result = await _userRepository.GetUsersWithProductsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, u => Assert.Empty(u.Products));
    }
}