using FluentValidation;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Example request that requires context-aware validation
/// </summary>
public class ContextAwareRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int? AssignedUserId { get; set; }
    public decimal Budget { get; set; }
}

/// <summary>
/// Validator that uses validation context service for business rule validation
/// </summary>
public class ContextAwareRequestValidator : BaseValidator<ContextAwareRequest>
{
    private readonly IValidationContextService _contextService;

    public ContextAwareRequestValidator(IValidationContextService contextService)
    {
        _contextService = contextService;

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required.")
            .MaximumLength(5000)
            .WithMessage("Content cannot exceed 5000 characters.");

        // Context-aware validation: Only admins can create public content
        RuleFor(x => x.IsPublic)
            .Must(BeAllowedToCreatePublicContent)
            .WithMessage("Only administrators can create public content.")
            .When(x => x.IsPublic);

        // Context-aware validation: Users can only assign to themselves unless they're managers
        RuleFor(x => x.AssignedUserId)
            .Must(BeValidAssignment)
            .WithMessage("You can only assign tasks to yourself unless you have manager permissions.")
            .When(x => x.AssignedUserId.HasValue);

        // Context-aware validation: Budget limits based on user role
        RuleFor(x => x.Budget)
            .Must(BeWithinBudgetLimit)
            .WithMessage("Budget exceeds your authorization limit.")
            .When(x => x.Budget > 0);

        // Multi-tenant validation: Content must belong to current tenant
        RuleFor(x => x)
            .Must(BelongToCurrentTenant)
            .WithMessage("Content must belong to your organization.")
            .WithName("Request");
    }

    private bool BeAllowedToCreatePublicContent(bool isPublic)
    {
        if (!isPublic) return true;
        
        var userRoles = _contextService.GetCurrentUserRoles();
        return userRoles.Contains("Admin") || userRoles.Contains("ContentManager");
    }

    private bool BeValidAssignment(int? assignedUserId)
    {
        if (!assignedUserId.HasValue) return true;

        var currentUserId = _contextService.GetCurrentUserId();
        var userRoles = _contextService.GetCurrentUserRoles();

        // Managers can assign to anyone
        if (userRoles.Contains("Manager") || userRoles.Contains("Admin"))
            return true;

        // Regular users can only assign to themselves
        return currentUserId == assignedUserId;
    }

    private bool BeWithinBudgetLimit(decimal budget)
    {
        var userRoles = _contextService.GetCurrentUserRoles();
        
        return userRoles.Contains("Admin") ? budget <= 100000 :
               userRoles.Contains("Manager") ? budget <= 10000 :
               budget <= 1000; // Regular users
    }

    private bool BelongToCurrentTenant(ContextAwareRequest request)
    {
        var currentTenantId = _contextService.GetCurrentTenantId();
        
        // If no tenant context, allow (single-tenant scenario)
        if (string.IsNullOrEmpty(currentTenantId))
            return true;

        // In a real implementation, you would check if the request
        // belongs to the current tenant context
        return true;
    }
}