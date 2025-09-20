using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Application.DTOs.Requests;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator;

    public CreateUserRequestValidatorTests()
    {
        _validator = new CreateUserRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "", Email = "test@example.com" };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("User name is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var request = new CreateUserRequest 
        { 
            Name = new string('a', 101), 
            Email = "test@example.com" 
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("User name cannot exceed 100 characters.");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Contains_Invalid_Characters()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John123", Email = "test@example.com" };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("User name can only contain letters, spaces, hyphens, apostrophes, and periods.");
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John Doe", Email = "" };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John Doe", Email = "invalid-email" };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Should_Have_Error_When_Email_Exceeds_MaxLength()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com";
        var request = new CreateUserRequest { Name = "John Doe", Email = longEmail };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 255 characters.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        // Arrange
        var request = new CreateUserRequest 
        { 
            Name = "John Doe", 
            Email = "john.doe@example.com" 
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}