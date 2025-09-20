using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Example of conditional validation for complex business rules
/// This demonstrates how to implement conditional validation based on other field values
/// </summary>
public class ConditionalValidationRequest
{
    public string ProductType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? DiscountCode { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsDigitalProduct { get; set; }
    public decimal? Weight { get; set; }
    public string? ShippingAddress { get; set; }
}

/// <summary>
/// Validator demonstrating conditional validation patterns
/// </summary>
public class ConditionalValidationRequestValidator : BaseValidator<ConditionalValidationRequest>
{
    public ConditionalValidationRequestValidator()
    {
        // Basic validation
        RuleFor(x => x.ProductType)
            .NotEmpty()
            .WithMessage("Product type is required.")
            .Must(BeValidProductType)
            .WithMessage("Product type must be either 'Physical' or 'Digital'.");

        ValidatePositiveDecimal(RuleFor(x => x.Price), "Price");

        // Conditional validation: If discount code is provided, discount percentage is required
        RuleFor(x => x.DiscountPercentage)
            .NotNull()
            .WithMessage("Discount percentage is required when discount code is provided.")
            .GreaterThan(0)
            .WithMessage("Discount percentage must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Discount percentage cannot exceed 100%.")
            .When(x => !string.IsNullOrEmpty(x.DiscountCode));

        // Conditional validation: Physical products require weight and shipping address
        RuleFor(x => x.Weight)
            .NotNull()
            .WithMessage("Weight is required for physical products.")
            .GreaterThan(0)
            .WithMessage("Weight must be greater than 0.")
            .When(x => !x.IsDigitalProduct);

        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage("Shipping address is required for physical products.")
            .MaximumLength(500)
            .WithMessage("Shipping address cannot exceed 500 characters.")
            .When(x => !x.IsDigitalProduct);

        // Cross-field validation: Digital products cannot have weight
        RuleFor(x => x.Weight)
            .Null()
            .WithMessage("Digital products cannot have weight.")
            .When(x => x.IsDigitalProduct);

        // Complex conditional validation: High-value products require additional validation
        When(x => x.Price > 1000, () =>
        {
            RuleFor(x => x.DiscountPercentage)
                .LessThanOrEqualTo(50)
                .WithMessage("Discount for high-value products cannot exceed 50%.")
                .When(x => x.DiscountPercentage.HasValue);
        });
    }

    private static bool BeValidProductType(string productType)
    {
        var validTypes = new[] { "Physical", "Digital" };
        return validTypes.Contains(productType, StringComparer.OrdinalIgnoreCase);
    }
}