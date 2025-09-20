using CleanArchitecture.Application.Common.Behaviors;
using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Application.DTOs.Requests;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Tests.Integration;

/// <summary>
/// Integration tests for validation pipeline with MediatR
/// </summary>
public class ValidationPipelineIntegrationTests
{
    public record TestCommand(string Name, string Email) : IRequest<string>;

    public class TestCommandHandler : IRequestHandler<TestCommand, string>
    {
        public Task<string> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Processed: {request.Name} - {request.Email}");
        }
    }

    public class TestCommandValidator : BaseValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            ValidateName(RuleFor(x => x.Name), "Name");
            ValidateEmail(RuleFor(x => x.Email), "Email");
        }
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<TestCommand>();
        });
        
        // Add validation behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        // Add validators
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        
        // Add handlers
        services.AddTransient<IRequestHandler<TestCommand, string>, TestCommandHandler>();
        
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ValidationPipeline_Should_Allow_Valid_Request()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var validCommand = new TestCommand("John Doe", "john.doe@example.com");

        // Act
        var result = await mediator.Send(validCommand);

        // Assert
        Assert.Equal("Processed: John Doe - john.doe@example.com", result);
    }

    [Fact]
    public async Task ValidationPipeline_Should_Throw_ValidationException_For_Invalid_Request()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var invalidCommand = new TestCommand("", "invalid-email");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(invalidCommand));

        Assert.Contains("Name", exception.Errors.Keys);
        Assert.Contains("Email", exception.Errors.Keys);
        Assert.Contains("Name is required.", exception.Errors["Name"]);
        Assert.Contains("Email must be a valid email address.", exception.Errors["Email"]);
    }

    [Fact]
    public async Task ValidationPipeline_Should_Handle_Multiple_Validation_Errors()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var invalidCommand = new TestCommand("John123", "test@" + new string('a', 250) + ".com");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(invalidCommand));

        Assert.Contains("Name", exception.Errors.Keys);
        Assert.Contains("Email", exception.Errors.Keys);
        Assert.Contains("can only contain letters, spaces, hyphens, apostrophes, and periods", 
            string.Join(" ", exception.Errors["Name"]));
        Assert.Contains("cannot exceed 255 characters", 
            string.Join(" ", exception.Errors["Email"]));
    }

    [Fact]
    public void ValidationPipeline_Should_Work_With_CreateUserRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();
        services.AddApplication(configuration); // This should register all validators and behaviors
        
        var serviceProvider = services.BuildServiceProvider();
        var validators = serviceProvider.GetServices<IValidator<CreateUserRequest>>();

        // Act
        var validator = validators.FirstOrDefault();

        // Assert
        Assert.NotNull(validator);
        Assert.IsType<CreateUserRequestValidator>(validator);
    }
}