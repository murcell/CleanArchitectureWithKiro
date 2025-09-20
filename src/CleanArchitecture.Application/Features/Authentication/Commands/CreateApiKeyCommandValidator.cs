using FluentValidation;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Validator for create API key command
/// </summary>
public class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Request.Scopes)
            .NotEmpty().WithMessage("At least one scope is required")
            .Must(scopes => scopes != null && scopes.Length > 0)
            .WithMessage("At least one scope is required");

        RuleFor(x => x.Request.ExpiresAt)
            .Must(expiresAt => !expiresAt.HasValue || expiresAt.Value > DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future");
    }
}