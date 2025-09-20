using CleanArchitecture.Infrastructure.Security;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
    }

    [Fact]
    public void HashPassword_WithSamePassword_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt uses salt, so hashes should be different
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(hashedPassword, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(hashedPassword, wrongPassword);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void HashPassword_WithInvalidPassword_ThrowsArgumentException(string invalidPassword)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordHasher.HashPassword(invalidPassword));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void VerifyPassword_WithInvalidHashedPassword_ThrowsArgumentException(string invalidHashedPassword)
    {
        // Arrange
        var password = "TestPassword123!";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordHasher.VerifyPassword(invalidHashedPassword, password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void VerifyPassword_WithInvalidProvidedPassword_ThrowsArgumentException(string invalidProvidedPassword)
    {
        // Arrange
        var hashedPassword = _passwordHasher.HashPassword("TestPassword123!");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordHasher.VerifyPassword(hashedPassword, invalidProvidedPassword));
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ReturnsFalse()
    {
        // Arrange
        var invalidHash = "invalid-hash-format";
        var password = "TestPassword123!";

        // Act
        var result = _passwordHasher.VerifyPassword(invalidHash, password);

        // Assert
        Assert.False(result);
    }
}