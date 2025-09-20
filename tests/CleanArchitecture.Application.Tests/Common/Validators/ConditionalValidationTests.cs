using CleanArchitecture.Application.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

/// <summary>
/// Tests for conditional validation functionality
/// </summary>
public class ConditionalValidationTests
{
    private readonly ConditionalValidationRequestValidator _validator;

    public ConditionalValidationTests()
    {
        _validator = new ConditionalValidationRequestValidator();
    }

    [Fact]
    public void Should_Require_DiscountPercentage_When_DiscountCode_Is_Provided()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Digital",
            Price = 100,
            DiscountCode = "SAVE10",
            DiscountPercentage = null,
            IsDigitalProduct = true
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DiscountPercentage)
            .WithErrorMessage("Discount percentage is required when discount code is provided.");
    }

    [Fact]
    public void Should_Not_Require_DiscountPercentage_When_DiscountCode_Is_Not_Provided()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Digital",
            Price = 100,
            DiscountCode = null,
            DiscountPercentage = null,
            IsDigitalProduct = true
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountPercentage);
    }

    [Fact]
    public void Should_Require_Weight_For_Physical_Products()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Physical",
            Price = 100,
            IsDigitalProduct = false,
            Weight = null,
            ShippingAddress = "123 Main St"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("Weight is required for physical products.");
    }

    [Fact]
    public void Should_Require_ShippingAddress_For_Physical_Products()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Physical",
            Price = 100,
            IsDigitalProduct = false,
            Weight = 1.5m,
            ShippingAddress = null
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress)
            .WithErrorMessage("Shipping address is required for physical products.");
    }

    [Fact]
    public void Should_Not_Allow_Weight_For_Digital_Products()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Digital",
            Price = 100,
            IsDigitalProduct = true,
            Weight = 1.5m
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("Digital products cannot have weight.");
    }

    [Fact]
    public void Should_Limit_Discount_For_High_Value_Products()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Digital",
            Price = 1500, // High-value product
            DiscountCode = "SAVE60",
            DiscountPercentage = 60, // Exceeds 50% limit for high-value products
            IsDigitalProduct = true
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DiscountPercentage)
            .WithErrorMessage("Discount for high-value products cannot exceed 50%.");
    }

    [Fact]
    public void Should_Allow_Valid_Discount_For_High_Value_Products()
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = "Digital",
            Price = 1500, // High-value product
            DiscountCode = "SAVE30",
            DiscountPercentage = 30, // Within 50% limit
            IsDigitalProduct = true
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountPercentage);
    }

    [Theory]
    [InlineData("Physical")]
    [InlineData("Digital")]
    public void Should_Accept_Valid_Product_Types(string productType)
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = productType,
            Price = 100,
            IsDigitalProduct = productType == "Digital"
        };

        if (!request.IsDigitalProduct)
        {
            request.Weight = 1.0m;
            request.ShippingAddress = "123 Main St";
        }

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ProductType);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    [InlineData("Service")]
    public void Should_Reject_Invalid_Product_Types(string productType)
    {
        // Arrange
        var request = new ConditionalValidationRequest
        {
            ProductType = productType,
            Price = 100,
            IsDigitalProduct = false
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ProductType);
    }
}