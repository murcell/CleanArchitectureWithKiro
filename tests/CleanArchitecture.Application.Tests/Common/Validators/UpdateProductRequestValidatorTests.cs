using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Application.DTOs.Requests;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

public class UpdateProductRequestValidatorTests
{
    private readonly UpdateProductRequestValidator _validator;

    public UpdateProductRequestValidatorTests()
    {
        _validator = new UpdateProductRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "", 
            Description = "Test description",
            Price = 10.99m,
            Currency = "USD",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = new string('a', 201), 
            Description = "Test description",
            Price = 10.99m,
            Currency = "USD",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name cannot exceed 200 characters.");
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Empty()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "",
            Price = 10.99m,
            Currency = "USD",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Product description is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = new string('a', 1001),
            Price = 10.99m,
            Currency = "USD",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Product description cannot exceed 1000 characters.");
    }

    [Fact]
    public void Should_Have_Error_When_Price_Is_Zero_Or_Negative()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 0,
            Currency = "USD",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Product price must be greater than 0.");
    }

    [Fact]
    public void Should_Have_Error_When_Price_Exceeds_Maximum()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 1000001,
            Currency = "USD",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Product price cannot exceed 1.000.000.");
    }

    [Fact]
    public void Should_Have_Error_When_Currency_Is_Empty()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 10.99m,
            Currency = "",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Currency_Is_Invalid_Length()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 10.99m,
            Currency = "US",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be exactly 3 characters (ISO 4217 format).");
    }

    [Fact]
    public void Should_Have_Error_When_Currency_Is_Not_Uppercase()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 10.99m,
            Currency = "usd",
            Stock = 5
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be in uppercase ISO 4217 format (e.g., USD, EUR, TRY).");
    }

    [Fact]
    public void Should_Have_Error_When_Stock_Is_Negative()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 10.99m,
            Currency = "USD",
            Stock = -1
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Stock)
            .WithErrorMessage("Stock cannot be negative.");
    }

    [Fact]
    public void Should_Have_Error_When_Stock_Exceeds_Maximum()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "Test description",
            Price = 10.99m,
            Currency = "USD",
            Stock = 100001
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Stock)
            .WithErrorMessage("Stock cannot exceed 100.000.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        // Arrange
        var request = new UpdateProductRequest 
        { 
            Name = "Test Product", 
            Description = "A great test product",
            Price = 29.99m,
            Currency = "USD",
            Stock = 100
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}