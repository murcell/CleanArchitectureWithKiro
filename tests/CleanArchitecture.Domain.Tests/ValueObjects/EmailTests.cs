using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;

namespace CleanArchitecture.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.org")]
    [InlineData("123@456.com")]
    public void Create_WithValidEmail_ShouldCreateEmail(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);
        
        // Assert
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrEmpty_ShouldThrowDomainException(string invalidEmail)
    {
        // Act & Assert
        var action = () => Email.Create(invalidEmail);
        action.Should().Throw<DomainException>()
            .WithMessage("Email cannot be null or empty.");
    }
    
    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    [InlineData("test@.com")]
    public void Create_WithInvalidFormat_ShouldThrowDomainException(string invalidEmail)
    {
        // Act & Assert
        var action = () => Email.Create(invalidEmail);
        action.Should().Throw<DomainException>()
            .WithMessage($"Invalid email format: {invalidEmail.ToLowerInvariant()}");
    }
    
    [Fact]
    public void Create_WithEmailTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com";
        
        // Act & Assert
        var action = () => Email.Create(longEmail);
        action.Should().Throw<DomainException>()
            .WithMessage("Email cannot exceed 255 characters.");
    }
    
    [Fact]
    public void Equals_WithSameEmail_ShouldReturnTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("TEST@EXAMPLE.COM");
        
        // Act & Assert
        email1.Equals(email2).Should().BeTrue();
        (email1 == email2).Should().BeTrue();
    }
    
    [Fact]
    public void Equals_WithDifferentEmail_ShouldReturnFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");
        
        // Act & Assert
        email1.Equals(email2).Should().BeFalse();
        (email1 != email2).Should().BeTrue();
    }
    
    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);
        
        // Act & Assert
        email.ToString().Should().Be(emailValue);
    }
    
    [Fact]
    public void ImplicitConversion_ShouldReturnEmailValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);
        
        // Act
        string converted = email;
        
        // Assert
        converted.Should().Be(emailValue);
    }
}