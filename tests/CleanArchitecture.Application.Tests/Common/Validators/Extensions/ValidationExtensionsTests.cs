using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Application.Common.Validators.Extensions;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators.Extensions;

/// <summary>
/// Tests for validation extension methods
/// </summary>
public class ValidationExtensionsTests
{
    public class TestRequest
    {
        public string AlphanumericField { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal DecimalValue { get; set; }
        public string RestrictedField { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    public class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.AlphanumericField)
                .AlphanumericWithSpaces();

            RuleFor(x => x.PhoneNumber)
                .PhoneNumber();

            RuleFor(x => x.DecimalValue)
                .MaxDecimalPlaces(2);

            RuleFor(x => x.RestrictedField)
                .NotInForbiddenList("admin", "root", "system");

            RuleFor(x => x.Items)
                .CountBetween(1, 5);
        }
    }

    private readonly TestRequestValidator _validator;

    public ValidationExtensionsTests()
    {
        _validator = new TestRequestValidator();
    }

    [Theory]
    [InlineData("Hello World 123")]
    [InlineData("Test123")]
    [InlineData("ABC 456")]
    public void AlphanumericWithSpaces_Should_Pass_For_Valid_Input(string input)
    {
        // Arrange
        var request = new TestRequest 
        { 
            AlphanumericField = input,
            PhoneNumber = "+1234567890",
            DecimalValue = 10.50m,
            RestrictedField = "user",
            Items = new List<string> { "item1" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.AlphanumericField);
    }

    [Theory]
    [InlineData("Hello@World")]
    [InlineData("Test-123")]
    [InlineData("ABC#456")]
    public void AlphanumericWithSpaces_Should_Fail_For_Invalid_Input(string input)
    {
        // Arrange
        var request = new TestRequest { AlphanumericField = input };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AlphanumericField)
            .WithErrorMessage("Field can only contain letters, numbers, and spaces.");
    }

    [Theory]
    [InlineData("+1234567890")]
    [InlineData("+905551234567")]
    [InlineData("1234567890")]
    public void PhoneNumber_Should_Pass_For_Valid_Input(string phoneNumber)
    {
        // Arrange
        var request = new TestRequest 
        { 
            AlphanumericField = "Test",
            PhoneNumber = phoneNumber,
            DecimalValue = 10.50m,
            RestrictedField = "user",
            Items = new List<string> { "item1" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("+0123456789")]
    [InlineData("123-456-7890")]
    public void PhoneNumber_Should_Fail_For_Invalid_Input(string phoneNumber)
    {
        // Arrange
        var request = new TestRequest 
        { 
            AlphanumericField = "Test123",
            PhoneNumber = phoneNumber,
            DecimalValue = 10.50m,
            RestrictedField = "user",
            Items = new List<string> { "item1" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number must be in valid international format.");
    }

    [Theory]
    [InlineData(10.50)]
    [InlineData(100.99)]
    [InlineData(0.01)]
    public void MaxDecimalPlaces_Should_Pass_For_Valid_Decimal_Places(decimal value)
    {
        // Arrange
        var request = new TestRequest 
        { 
            AlphanumericField = "Test",
            PhoneNumber = "+1234567890",
            DecimalValue = value,
            RestrictedField = "user",
            Items = new List<string> { "item1" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.DecimalValue);
    }

    [Theory]
    [InlineData(10.555)]
    [InlineData(100.9999)]
    public void MaxDecimalPlaces_Should_Fail_For_Too_Many_Decimal_Places(decimal value)
    {
        // Arrange
        var request = new TestRequest { DecimalValue = value };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DecimalValue)
            .WithErrorMessage("Value cannot have more than 2 decimal places.");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("ROOT")]
    [InlineData("System")]
    public void NotInForbiddenList_Should_Fail_For_Forbidden_Values(string value)
    {
        // Arrange
        var request = new TestRequest { RestrictedField = value };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RestrictedField)
            .WithErrorMessage("Value cannot be one of the following: admin, root, system");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("guest")]
    [InlineData("customer")]
    public void NotInForbiddenList_Should_Pass_For_Allowed_Values(string value)
    {
        // Arrange
        var request = new TestRequest 
        { 
            AlphanumericField = "Test",
            PhoneNumber = "+1234567890",
            DecimalValue = 10.50m,
            RestrictedField = value,
            Items = new List<string> { "item1" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.RestrictedField);
    }

    [Fact]
    public void CountBetween_Should_Pass_For_Valid_Collection_Size()
    {
        // Arrange
        var request = new TestRequest 
        { 
            AlphanumericField = "Test",
            PhoneNumber = "+1234567890",
            DecimalValue = 10.50m,
            RestrictedField = "user",
            Items = new List<string> { "item1", "item2", "item3" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void CountBetween_Should_Fail_For_Empty_Collection()
    {
        // Arrange
        var request = new TestRequest { Items = new List<string>() };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Collection must contain between 1 and 5 items.");
    }

    [Fact]
    public void CountBetween_Should_Fail_For_Too_Large_Collection()
    {
        // Arrange
        var request = new TestRequest 
        { 
            Items = new List<string> { "1", "2", "3", "4", "5", "6" }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Collection must contain between 1 and 5 items.");
    }
}