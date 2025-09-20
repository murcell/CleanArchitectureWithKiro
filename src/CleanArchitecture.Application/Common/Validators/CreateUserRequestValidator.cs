using CleanArchitecture.Application.DTOs.Requests;
using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Enhanced validator for CreateUserRequest with security validations
/// </summary>
public class CreateUserRequestValidator : BaseValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        ValidateName(RuleFor(x => x.Name), "User name");
        ValidateEmail(RuleFor(x => x.Email), "Email");
        
        // Additional security validations
        RuleFor(x => x.Name)
            .WithinRateLimit(50)
            .WithMessage("Name exceeds maximum allowed length for security reasons");
            
        RuleFor(x => x.Email)
            .WithinRateLimit(255)
            .WithMessage("Email exceeds maximum allowed length for security reasons");
    }
}