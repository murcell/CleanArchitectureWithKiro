using CleanArchitecture.Application.Features.Users.Commands.CreateUser;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.Application.Tests.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateUserCommandHandler>> _mockLogger;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CreateUserCommandHandler>>();
        
        _handler = new CreateUserCommandHandler(
            _mockUserRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUserAndReturnId()
    {
        // Arrange
        var command = new CreateUserCommand("John Doe", "john.doe@example.com");
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), cancellationToken))
            .ReturnsAsync((User user, CancellationToken ct) => 
            {
                // Simulate setting ID after adding to repository
                var userType = typeof(User);
                var idProperty = userType.GetProperty("Id");
                idProperty?.SetValue(user, 1);
                return user;
            });

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.Equal(1, result);
        
        _mockUserRepository.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken),
            Times.Once);
        
        _mockUserRepository.Verify(
            x => x.AddAsync(It.Is<User>(u => u.Name == command.Name && u.Email.Value == command.Email), cancellationToken),
            Times.Once);
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_UserWithEmailAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateUserCommand("John Doe", "john.doe@example.com");
        var cancellationToken = CancellationToken.None;
        var existingUser = User.Create("Jane Doe", "john.doe@example.com");

        _mockUserRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        Assert.Equal("User with email john.doe@example.com already exists", exception.Message);
        
        _mockUserRepository.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken),
            Times.Once);
        
        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "john.doe@example.com")]
    [InlineData("   ", "john.doe@example.com")]
    [InlineData(null, "john.doe@example.com")]
    public async Task Handle_InvalidName_ShouldThrowDomainException(string name, string email)
    {
        // Arrange
        var command = new CreateUserCommand(name, email);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Theory]
    [InlineData("John Doe", "")]
    [InlineData("John Doe", "   ")]
    [InlineData("John Doe", null)]
    [InlineData("John Doe", "invalid-email")]
    public async Task Handle_InvalidEmail_ShouldThrowDomainException(string name, string email)
    {
        // Arrange
        var command = new CreateUserCommand(name, email);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldLogInformationMessages()
    {
        // Arrange
        var command = new CreateUserCommand("John Doe", "john.doe@example.com");
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), cancellationToken))
            .ReturnsAsync((User user, CancellationToken ct) => 
            {
                var userType = typeof(User);
                var idProperty = userType.GetProperty("Id");
                idProperty?.SetValue(user, 1);
                return user;
            });

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating user with name: John Doe and email: john.doe@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User created successfully with ID: 1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}