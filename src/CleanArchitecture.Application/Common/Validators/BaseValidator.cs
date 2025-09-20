using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Base validator class with common validation rules and utilities
/// </summary>
/// <typeparam name="T">The type being validated</typeparam>
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Common validation rules for email addresses with security enhancements
    /// </summary>
    protected void ValidateEmail(IRuleBuilder<T, string> ruleBuilder, string fieldName)
    {
        ruleBuilder
            .NotEmpty()
            .WithMessage($"{fieldName} is required.")
            .EmailAddress()
            .WithMessage($"{fieldName} must be a valid email address.")
            .MaximumLength(255)
            .WithMessage($"{fieldName} cannot exceed 255 characters.")
            .NoXss()
            .NoSqlInjection()
            .SafeCharactersOnly();
    }

    /// <summary>
    /// Common validation rules for names with security enhancements
    /// </summary>
    protected void ValidateName(IRuleBuilder<T, string> ruleBuilder, string fieldName, int maxLength = 100)
    {
        ruleBuilder
            .NotEmpty()
            .WithMessage($"{fieldName} is required.")
            .MaximumLength(maxLength)
            .WithMessage($"{fieldName} cannot exceed {maxLength} characters.")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage($"{fieldName} can only contain letters, spaces, hyphens, apostrophes, and periods.")
            .NoXss()
            .NoSqlInjection()
            .NoHtmlTags();
    }

    /// <summary>
    /// Common validation rules for currency codes
    /// </summary>
    protected void ValidateCurrency(IRuleBuilder<T, string> ruleBuilder)
    {
        ruleBuilder
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters (ISO 4217 format).")
            .Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency must be in uppercase ISO 4217 format (e.g., USD, EUR, TRY).");
    }

    /// <summary>
    /// Common validation rules for positive decimal values
    /// </summary>
    protected void ValidatePositiveDecimal(IRuleBuilder<T, decimal> ruleBuilder, string fieldName, decimal maxValue = 1000000)
    {
        ruleBuilder
            .GreaterThan(0)
            .WithMessage($"{fieldName} must be greater than 0.")
            .LessThanOrEqualTo(maxValue)
            .WithMessage($"{fieldName} cannot exceed {maxValue:N0}.");
    }

    /// <summary>
    /// Common validation rules for non-negative integers
    /// </summary>
    protected void ValidateNonNegativeInteger(IRuleBuilder<T, int> ruleBuilder, string fieldName, int maxValue = 100000)
    {
        ruleBuilder
            .GreaterThanOrEqualTo(0)
            .WithMessage($"{fieldName} cannot be negative.")
            .LessThanOrEqualTo(maxValue)
            .WithMessage($"{fieldName} cannot exceed {maxValue:N0}.");
    }

    /// <summary>
    /// Common validation rules for positive integers (IDs)
    /// </summary>
    protected void ValidatePositiveInteger(IRuleBuilder<T, int> ruleBuilder, string fieldName)
    {
        ruleBuilder
            .GreaterThan(0)
            .WithMessage($"Valid {fieldName} is required.");
    }
}