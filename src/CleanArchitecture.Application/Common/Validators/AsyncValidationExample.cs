using FluentValidation;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Example of async validation for complex business rules that require database access
/// </summary>
public class AsyncValidationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}

/// <summary>
/// Validator demonstrating async validation patterns with dependency injection
/// </summary>
public class AsyncValidationRequestValidator : BaseValidator<AsyncValidationRequest>
{
    private readonly IUserRepository _userRepository;

    public AsyncValidationRequestValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        ValidateEmail(RuleFor(x => x.Email), "Email");
        
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50)
            .WithMessage("Username cannot exceed 50 characters.")
            .MustAsync(BeUniqueUsername)
            .WithMessage("Username is already taken.");

        RuleFor(x => x.Email)
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email is already registered.");

        RuleFor(x => x.CategoryId)
            .MustAsync(CategoryExists)
            .WithMessage("Selected category does not exist.");
    }

    /// <summary>
    /// Async validation to check if username is unique
    /// </summary>
    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username))
            return true; // Let the NotEmpty rule handle this

        var existingUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        return existingUser == null;
    }

    /// <summary>
    /// Async validation to check if email is unique
    /// </summary>
    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email))
            return true; // Let the email validation rule handle this

        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        return existingUser == null;
    }

    /// <summary>
    /// Async validation to check if category exists
    /// </summary>
    private async Task<bool> CategoryExists(int categoryId, CancellationToken cancellationToken)
    {
        if (categoryId <= 0)
            return false;

        // This would typically call a category repository
        // For now, we'll simulate with a simple check
        await Task.Delay(1, cancellationToken); // Simulate async operation
        return categoryId <= 100; // Simulate valid category IDs
    }
}