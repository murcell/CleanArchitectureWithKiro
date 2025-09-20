using CleanArchitecture.Application.Common.Validators;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace CleanArchitecture.Application.Tests.Common.Validators;

public class ContextAwareValidationTests
{
    private readonly Mock<IValidationContextService> _mockContextService;
    private readonly ContextAwareRequestValidator _validator;

    public ContextAwareValidationTests()
    {
        _mockContextService = new Mock<IValidationContextService>();
        _validator = new ContextAwareRequestValidator(_mockContextService.Object);
    }

    [Fact]
    public void Should_Allow_Public_Content_For_Admin()
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            IsPublic = true,
            Budget = 500
        };

        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { "Admin" });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.IsPublic);
    }

    [Fact]
    public void Should_Deny_Public_Content_For_Regular_User()
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            IsPublic = true,
            Budget = 500
        };

        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { "User" });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.IsPublic)
            .WithErrorMessage("Only administrators can create public content.");
    }

    [Fact]
    public void Should_Allow_Self_Assignment_For_Regular_User()
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            AssignedUserId = 123,
            Budget = 500
        };

        _mockContextService.Setup(x => x.GetCurrentUserId())
            .Returns(123);
        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { "User" });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.AssignedUserId);
    }

    [Fact]
    public void Should_Deny_Assignment_To_Others_For_Regular_User()
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            AssignedUserId = 456,
            Budget = 500
        };

        _mockContextService.Setup(x => x.GetCurrentUserId())
            .Returns(123);
        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { "User" });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AssignedUserId)
            .WithErrorMessage("You can only assign tasks to yourself unless you have manager permissions.");
    }

    [Fact]
    public void Should_Allow_Assignment_To_Others_For_Manager()
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            AssignedUserId = 456,
            Budget = 500
        };

        _mockContextService.Setup(x => x.GetCurrentUserId())
            .Returns(123);
        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { "Manager" });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.AssignedUserId);
    }

    [Theory]
    [InlineData("User", 1500)]
    [InlineData("Manager", 15000)]
    [InlineData("Admin", 150000)]
    public void Should_Enforce_Budget_Limits_Based_On_Role(string role, decimal budget)
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            Budget = budget
        };

        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { role });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Budget)
            .WithErrorMessage("Budget exceeds your authorization limit.");
    }

    [Theory]
    [InlineData("User", 500)]
    [InlineData("Manager", 5000)]
    [InlineData("Admin", 50000)]
    public void Should_Allow_Budget_Within_Limits(string role, decimal budget)
    {
        // Arrange
        var request = new ContextAwareRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            Budget = budget
        };

        _mockContextService.Setup(x => x.GetCurrentUserRoles())
            .Returns(new[] { role });

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Budget);
    }
}