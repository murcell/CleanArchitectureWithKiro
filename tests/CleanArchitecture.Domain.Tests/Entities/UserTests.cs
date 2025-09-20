using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Events;
using CleanArchitecture.Domain.Exceptions;
using FluentAssertions;

namespace CleanArchitecture.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var name = "John Doe";
        var email = "john.doe@example.com";
        
        // Act
        var user = User.Create(name, email);
        
        // Assert
        user.Name.Should().Be(name);
        user.Email.Value.Should().Be(email.ToLowerInvariant());
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserCreatedEvent>();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowDomainException(string invalidName)
    {
        // Arrange
        var email = "john.doe@example.com";
        
        // Act & Assert
        var action = () => User.Create(invalidName, email);
        action.Should().Throw<DomainException>()
            .WithMessage("User name cannot be null or empty.");
    }
    
    [Fact]
    public void Create_WithNameTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('a', 101);
        var email = "john.doe@example.com";
        
        // Act & Assert
        var action = () => User.Create(longName, email);
        action.Should().Throw<DomainException>()
            .WithMessage("User name cannot exceed 100 characters.");
    }
    
    [Fact]
    public void Create_WithInvalidEmail_ShouldThrowDomainException()
    {
        // Arrange
        var name = "John Doe";
        var invalidEmail = "invalid-email";
        
        // Act & Assert
        var action = () => User.Create(name, invalidEmail);
        action.Should().Throw<DomainException>()
            .WithMessage($"Invalid email format: {invalidEmail}");
    }
    
    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        user.ClearDomainEvents();
        var newName = "Jane Doe";
        
        // Act
        user.UpdateName(newName);
        
        // Assert
        user.Name.Should().Be(newName);
        user.UpdatedAt.Should().NotBeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserNameUpdatedEvent>();
    }
    
    [Fact]
    public void UpdateEmail_WithValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        user.ClearDomainEvents();
        var newEmail = "jane.doe@example.com";
        
        // Act
        user.UpdateEmail(newEmail);
        
        // Assert
        user.Email.Value.Should().Be(newEmail);
        user.UpdatedAt.Should().NotBeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserEmailUpdatedEvent>();
    }
    
    [Fact]
    public void Activate_WhenInactive_ShouldActivateUser()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        user.Deactivate();
        user.ClearDomainEvents();
        
        // Act
        user.Activate();
        
        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().NotBeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserActivatedEvent>();
    }
    
    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        
        // Act & Assert
        var action = () => user.Activate();
        action.Should().Throw<DomainException>()
            .WithMessage("User is already active.");
    }
    
    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateUser()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        user.ClearDomainEvents();
        
        // Act
        user.Deactivate();
        
        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().NotBeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserDeactivatedEvent>();
    }
    
    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        user.Deactivate();
        
        // Act & Assert
        var action = () => user.Deactivate();
        action.Should().Throw<DomainException>()
            .WithMessage("User is already inactive.");
    }
    
    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = User.Create("John Doe", "john.doe@example.com");
        user.ClearDomainEvents();
        var beforeLogin = DateTime.UtcNow;
        
        // Act
        user.RecordLogin();
        
        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(beforeLogin);
        user.UpdatedAt.Should().NotBeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserLoggedInEvent>();
    }
}