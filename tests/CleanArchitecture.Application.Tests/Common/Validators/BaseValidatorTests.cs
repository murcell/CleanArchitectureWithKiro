using CleanArchitecture.Application.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

/// <summary>
/// Tests for BaseValidator functionality
/// </summary>
public class BaseValidatorTests
{
    // Test class to test BaseValidator functionality
    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int UserId { get; set; }
    }

    public class TestRequestValidator : BaseValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            ValidateName(RuleFor(x => x.Name), "Name");
            ValidateEmail(RuleFor(x => x.Email), "Email");
            ValidateCurrency(RuleFor(x => x.Currency));
            ValidatePositiveDecimal(RuleFor(x => x.Price), "Price");
            ValidateNonNegativeInteger(RuleFor(x => x.Stock), "Stock");
            ValidatePositiveInteger(RuleFor(x => x.UserId), "User ID");
        }
    }

    private readonly TestRequestValidator _validator;

    public BaseValidatorTests()
    {
        _validator = new TestRequestValidator();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_Should_Have_Error_When_Name_Is_Empty_Or_Whitespace(string name)
    {
        // Arrange
        var request = new TestRequest { Name = name };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void ValidateName_Should_Have_Error_When_Name_Is_Null()
    {
        // Arrange
        var request = new TestRequest { Name = null! };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void ValidateName_Should_Have_Error_When_Name_Contains_Invalid_Characters()
    {
        // Arrange
        var request = new TestRequest { Name = "John123@" };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods.");
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("Mary-Jane")]
    [InlineData("O'Connor")]
    [InlineData("Dr. Smith")]
    public void ValidateName_Should_Not_Have_Error_When_Name_Is_Valid(string name)
    {
        // Arrange
        var request = new TestRequest 
        { 
            Name = name,
            Email = "test@example.com",
            Currency = "USD",
            Price = 100,
            Stock = 10,
            UserId = 1
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("TRY")]
    public void ValidateCurrency_Should_Not_Have_Error_When_Currency_Is_Valid(string currency)
    {
        // Arrange
        var request = new TestRequest 
        { 
            Name = "Test",
            Email = "test@example.com",
            Currency = currency,
            Price = 100,
            Stock = 10,
            UserId = 1
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    public void ValidateCurrency_Should_Have_Error_When_Currency_Is_Invalid(string currency)
    {
        // Arrange
        var request = new TestRequest { Currency = currency };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidatePositiveDecimal_Should_Have_Error_When_Price_Is_Not_Positive(decimal price)
    {
        // Arrange
        var request = new TestRequest { Price = price };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than 0.");
    }

    [Fact]
    public void ValidatePositiveDecimal_Should_Have_Error_When_Price_Exceeds_Maximum()
    {
        // Arrange
        var request = new TestRequest { Price = 1000001 };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price cannot exceed 1.000.000.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateNonNegativeInteger_Should_Have_Error_When_Stock_Is_Negative(int stock)
    {
        // Arrange
        var request = new TestRequest { Stock = stock };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Stock)
            .WithErrorMessage("Stock cannot be negative.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidatePositiveInteger_Should_Have_Error_When_UserId_Is_Not_Positive(int userId)
    {
        // Arrange
        var request = new TestRequest { UserId = userId };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("Valid User ID is required.");
    }
}