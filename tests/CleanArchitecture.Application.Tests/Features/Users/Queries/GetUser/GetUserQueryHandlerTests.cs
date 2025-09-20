using AutoMapper;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Features.Users.Queries.GetUser;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.Application.Tests.Features.Users.Queries.GetUser;

public class GetUserQueryHandlerTests
{
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GetUserQueryHandler>> _mockLogger;
    private readonly GetUserQueryHandler _handler;

    public GetUserQueryHandlerTests()
    {
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GetUserQueryHandler>>();
        
        _handler = new GetUserQueryHandler(
            _mockUserRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnUserDto()
    {
        // Arrange
        var userId = 1;
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;
        
        var user = User.Create("John Doe", "john.doe@example.com");
        var userDto = new UserDto
        {
            Id = userId,
            Name = "John Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            LastLoginAt = null,
            Products = new List<ProductDto>()
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync(user);

        _mockMapper
            .Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userDto.Id, result.Id);
        Assert.Equal(userDto.Name, result.Name);
        Assert.Equal(userDto.Email, result.Email);
        Assert.Equal(userDto.IsActive, result.IsActive);
        
        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
        _mockMapper.Verify(x => x.Map<UserDto>(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var userId = 999;
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(query, cancellationToken));

        Assert.Equal($"User with ID {userId} not found", exception.Message);
        
        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
        _mockMapper.Verify(x => x.Map<UserDto>(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_InvalidUserId_ShouldThrowKeyNotFoundException(int userId)
    {
        // Arrange
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(query, cancellationToken));

        Assert.Equal($"User with ID {userId} not found", exception.Message);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldLogInformationMessages()
    {
        // Arrange
        var userId = 1;
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;
        
        var user = User.Create("John Doe", "john.doe@example.com");
        var userDto = new UserDto
        {
            Id = userId,
            Name = "John Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            LastLoginAt = null,
            Products = new List<ProductDto>()
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync(user);

        _mockMapper
            .Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Getting user with ID: {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User retrieved successfully: {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldLogWarningMessage()
    {
        // Arrange
        var userId = 999;
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(query, cancellationToken));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User with ID {userId} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var userId = 1;
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database connection failed");

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, cancellationToken));

        Assert.Equal(expectedException.Message, exception.Message);
        _mockMapper.Verify(x => x.Map<UserDto>(It.IsAny<User>()), Times.Never);
    }
}