using CleanArchitecture.Application.DTOs.Requests;
using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Validator for UpdateUserRequest
/// </summary>
public class UpdateUserRequestValidator : BaseValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        ValidateName(RuleFor(x => x.Name), "User name");
        ValidateEmail(RuleFor(x => x.Email), "Email");
    }
}