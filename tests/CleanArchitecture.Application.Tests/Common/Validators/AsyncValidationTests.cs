using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Domain.Interfaces;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

public class AsyncValidationTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly AsyncValidationRequestValidator _validator;

    public AsyncValidationTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _validator = new AsyncValidationRequestValidator(_mockUserRepository.Object);
    }

    [Fact]
    public async Task Should_Pass_When_Username_Is_Unique()
    {
        // Arrange
        var request = new AsyncValidationRequest
        {
            Email = "test@example.com",
            Username = "uniqueuser",
            CategoryId = 1
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("uniqueuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CleanArchitecture.Domain.Entities.User?)null);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CleanArchitecture.Domain.Entities.User?)null);

        // Act & Assert
        var result = await _validator.TestValidateAsync(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_Fail_When_Username_Already_Exists()
    {
        // Arrange
        var request = new AsyncValidationRequest
        {
            Email = "test@example.com",
            Username = "existinguser",
            CategoryId = 1
        };

        var existingUser = CleanArchitecture.Domain.Entities.User.Create("Existing User", "existing@example.com");
        _mockUserRepository.Setup(x => x.GetByUsernameAsync("existinguser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var result = await _validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is already taken.");
    }

    [Fact]
    public async Task Should_Fail_When_Email_Already_Exists()
    {
        // Arrange
        var request = new AsyncValidationRequest
        {
            Email = "existing@example.com",
            Username = "newuser",
            CategoryId = 1
        };

        var existingUser = CleanArchitecture.Domain.Entities.User.Create("Existing User", "existing@example.com");
        _mockUserRepository.Setup(x => x.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CleanArchitecture.Domain.Entities.User?)null);

        // Act & Assert
        var result = await _validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is already registered.");
    }

    [Fact]
    public async Task Should_Fail_When_Category_Does_Not_Exist()
    {
        // Arrange
        var request = new AsyncValidationRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            CategoryId = 999 // Invalid category ID
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CleanArchitecture.Domain.Entities.User?)null);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CleanArchitecture.Domain.Entities.User?)null);

        // Act & Assert
        var result = await _validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Selected category does not exist.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task Should_Fail_When_Username_Is_Invalid(string username)
    {
        // Arrange
        var request = new AsyncValidationRequest
        {
            Email = "test@example.com",
            Username = username,
            CategoryId = 1
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(request);
        
        if (string.IsNullOrEmpty(username))
        {
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username is required.");
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username must be at least 3 characters long.");
        }
    }
}