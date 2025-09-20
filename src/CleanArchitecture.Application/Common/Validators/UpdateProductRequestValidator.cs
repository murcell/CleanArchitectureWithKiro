using CleanArchitecture.Application.DTOs.Requests;
using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Validator for UpdateProductRequest
/// </summary>
public class UpdateProductRequestValidator : BaseValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        ValidateName(RuleFor(x => x.Name), "Product name", 200);
        
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Product description is required.")
            .MaximumLength(1000)
            .WithMessage("Product description cannot exceed 1000 characters.");

        ValidatePositiveDecimal(RuleFor(x => x.Price), "Product price");
        ValidateCurrency(RuleFor(x => x.Currency));
        ValidateNonNegativeInteger(RuleFor(x => x.Stock), "Stock");
    }
}