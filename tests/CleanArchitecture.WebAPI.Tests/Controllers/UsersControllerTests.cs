using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.Application.Features.Users.Commands.CreateUser;
using CleanArchitecture.Application.Features.Users.Queries.GetUser;
using CleanArchitecture.WebAPI.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Controllers;

/// <summary>
/// Unit tests for UsersController to verify validation integration
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateUser_Should_Return_CreatedResult_For_Valid_Request()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        var expectedUserId = 123;
        _mockMediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserId);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApiResponse<int>>(createdResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("User created successfully", response.Message);
        Assert.Equal(expectedUserId, response.Data);
        
        _mockMediator.Verify(m => m.Send(It.Is<CreateUserCommand>(c => 
            c.Name == request.Name && c.Email == request.Email), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_Should_Return_OkResult_With_User_Data()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new UserDto
        {
            Id = userId,
            Name = "John Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("User retrieved successfully", response.Message);
        Assert.Equal(expectedUser, response.Data);
        
        _mockMediator.Verify(m => m.Send(It.Is<GetUserQuery>(q => q.Id == userId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_Should_Return_OkResult_For_Valid_Request()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRequest
        {
            Name = "Jane Doe",
            Email = "jane.doe@example.com"
        };

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("User updated successfully", response.Message);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task GetUser_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        // Arrange
        var userId = 999;
        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new KeyNotFoundException($"User with ID {userId} not found"));

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserDto>>(notFoundResult.Value);
        
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task CreateUser_Should_Return_BadRequest_When_User_Already_Exists()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException($"User with email {request.Email} already exists"));

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<int>>(badRequestResult.Value);
        
        Assert.False(response.Success);
        Assert.Contains("already exists", response.Message);
    }

    [Fact]
    public async Task GetUsers_Should_Return_OkResult_With_Empty_List()
    {
        // Arrange & Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<UserDto>>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Users retrieved successfully", response.Message);
        Assert.Empty(response.Data ?? Enumerable.Empty<UserDto>());
    }

    [Fact]
    public async Task DeleteUser_Should_Return_OkResult()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("User deleted successfully", response.Message);
        Assert.True(response.Data);
    }
}