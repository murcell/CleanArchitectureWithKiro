using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators.Extensions;

/// <summary>
/// Extension methods for FluentValidation to provide additional validation capabilities
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates that a string contains only alphanumeric characters and spaces
    /// </summary>
    public static IRuleBuilderOptions<T, string> AlphanumericWithSpaces<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Matches(@"^[a-zA-Z0-9\s]+$")
            .WithMessage("Field can only contain letters, numbers, and spaces.");
    }

    /// <summary>
    /// Validates that a string is a valid phone number format
    /// </summary>
    public static IRuleBuilderOptions<T, string> PhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Matches(@"^\+?[1-9]\d{7,14}$")
            .WithMessage("Phone number must be in valid international format.");
    }

    /// <summary>
    /// Validates that a decimal value has a maximum number of decimal places
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> MaxDecimalPlaces<T>(this IRuleBuilder<T, decimal> ruleBuilder, int maxDecimalPlaces)
    {
        return ruleBuilder.Must(value => 
        {
            var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
            return decimalPlaces <= maxDecimalPlaces;
        }).WithMessage($"Value cannot have more than {maxDecimalPlaces} decimal places.");
    }

    /// <summary>
    /// Validates that a string is not in a list of forbidden values (case-insensitive)
    /// </summary>
    public static IRuleBuilderOptions<T, string> NotInForbiddenList<T>(this IRuleBuilder<T, string> ruleBuilder, params string[] forbiddenValues)
    {
        return ruleBuilder.Must(value => 
            !forbiddenValues.Any(forbidden => 
                string.Equals(value, forbidden, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"Value cannot be one of the following: {string.Join(", ", forbiddenValues)}");
    }

    /// <summary>
    /// Validates that a collection has a minimum and maximum number of items
    /// </summary>
    public static IRuleBuilderOptions<T, IEnumerable<TElement>> CountBetween<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>> ruleBuilder, 
        int min, 
        int max)
    {
        return ruleBuilder.Must(collection => 
        {
            var count = collection?.Count() ?? 0;
            return count >= min && count <= max;
        }).WithMessage($"Collection must contain between {min} and {max} items.");
    }
}