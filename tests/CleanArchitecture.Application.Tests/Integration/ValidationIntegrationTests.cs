using CleanArchitecture.Application.Common.Commands;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Tests.Integration;

/// <summary>
/// Integration tests for validation behavior with MediatR pipeline
/// </summary>
public class ValidationIntegrationTests
{
    // Test command for integration testing
    public record TestCommand(string Name, string Email) : ICommand<int>;

    // Test command handler
    public class TestCommandHandler : IRequestHandler<TestCommand, int>
    {
        public Task<int> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    // Test command validator
    public class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Email must be valid.");
        }
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add logging services required by MediatR
        services.AddLogging();
        
        // Add Application layer services
        var configuration = new ConfigurationBuilder().Build();
        services.AddApplication(configuration);
        
        // Add test-specific services
        services.AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_Execute_Command_When_Validation_Passes()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new TestCommand("John Doe", "john@example.com");

        // Act
        var result = await mediator.Send(command);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Should_Throw_ValidationException_When_Validation_Fails()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new TestCommand("", "invalid-email");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(command));

        Assert.Contains("Name", exception.Errors.Keys);
        Assert.Contains("Email", exception.Errors.Keys);
        Assert.Contains("Name is required.", exception.Errors["Name"]);
        Assert.Contains("Email must be valid.", exception.Errors["Email"]);
    }

    [Fact]
    public async Task Should_Throw_ValidationException_With_Multiple_Errors_For_Same_Property()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new TestCommand("", "");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(command));

        Assert.Contains("Name", exception.Errors.Keys);
        Assert.Contains("Email", exception.Errors.Keys);
        Assert.Contains("Name is required.", exception.Errors["Name"]);
        Assert.Contains("Email is required.", exception.Errors["Email"]);
    }
}