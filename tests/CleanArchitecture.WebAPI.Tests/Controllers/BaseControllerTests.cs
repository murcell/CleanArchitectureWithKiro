using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.WebAPI.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Controllers;

/// <summary>
/// Unit tests for BaseController functionality
/// </summary>
public class BaseControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TestController>> _mockLogger;
    private readonly TestController _controller;

    public BaseControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TestController>>();
        _controller = new TestController(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public void SuccessResponse_Should_Return_OkResult_With_ApiResponse()
    {
        // Arrange
        var data = "test data";
        var message = "Success message";

        // Act
        var result = _controller.TestSuccessResponse(data, message);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Equal(data, response.Data);
    }

    [Fact]
    public void CreatedResponse_Should_Return_CreatedResult_With_ApiResponse()
    {
        // Arrange
        var data = 123;
        var actionName = "GetTest";
        var routeValues = new { id = 123 };
        var message = "Created message";

        // Act
        var result = _controller.TestCreatedResponse(data, actionName, routeValues, message);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApiResponse<int>>(createdResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Equal(data, response.Data);
        Assert.Equal(actionName, createdResult.ActionName);
    }

    [Fact]
    public void NotFoundResponse_Should_Return_NotFoundResult_With_ApiResponse()
    {
        // Arrange
        var message = "Not found message";

        // Act
        var result = _controller.TestNotFoundResponse(message);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void BadRequestResponse_Should_Return_BadRequestResult_With_ApiResponse()
    {
        // Arrange
        var message = "Bad request message";

        // Act
        var result = _controller.TestBadRequestResponse(message);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_SuccessResponse_When_Request_Succeeds()
    {
        // Arrange
        var expectedResult = "success";
        var request = new TestRequest();
        var message = "Operation successful";

        _mockMediator.Setup(m => m.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.TestExecuteAsync(request, message);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Equal(expectedResult, response.Data);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_NotFound_When_KeyNotFoundException_Thrown()
    {
        // Arrange
        var request = new TestRequest();
        var exceptionMessage = "Resource not found";

        _mockMediator.Setup(m => m.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

        // Act
        var result = await _controller.TestExecuteAsync(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(notFoundResult.Value);
        
        Assert.False(response.Success);
        Assert.Equal(exceptionMessage, response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_BadRequest_When_InvalidOperationException_Thrown()
    {
        // Arrange
        var request = new TestRequest();
        var exceptionMessage = "Invalid operation";

        _mockMediator.Setup(m => m.Send(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _controller.TestExecuteAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
        
        Assert.False(response.Success);
        Assert.Equal(exceptionMessage, response.Message);
    }

    [Fact]
    public async Task ExecuteCreatedAsync_Should_Return_CreatedResponse_When_Request_Succeeds()
    {
        // Arrange
        var expectedResult = 123;
        var request = new TestRequestInt();
        var actionName = "GetTest";
        var routeValues = new { id = 123 };
        var message = "Resource created";

        _mockMediator.Setup(m => m.Send(It.IsAny<TestRequestInt>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.TestExecuteCreatedAsync(request, actionName, routeValues, message);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApiResponse<int>>(createdResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Equal(expectedResult, response.Data);
    }

    // Test controller to expose protected methods for testing
    public class TestController : BaseController
    {
        public TestController(IMediator mediator, ILogger<TestController> logger) 
            : base(mediator, logger)
        {
        }

        public ActionResult<ApiResponse<T>> TestSuccessResponse<T>(T data, string message = "Operation completed successfully")
        {
            return SuccessResponse(data, message);
        }

        public ActionResult<ApiResponse<T>> TestCreatedResponse<T>(T data, string actionName, object routeValues, string message = "Resource created successfully")
        {
            return CreatedResponse(data, actionName, routeValues, message);
        }

        public ActionResult<ApiResponse<object>> TestNotFoundResponse(string message = "Resource not found")
        {
            return NotFoundResponse(message);
        }

        public ActionResult<ApiResponse<object>> TestBadRequestResponse(string message = "Invalid request")
        {
            return BadRequestResponse(message);
        }

        public async Task<ActionResult<ApiResponse<T>>> TestExecuteAsync<T>(IRequest<T> request, string successMessage = "Operation completed successfully")
        {
            return await ExecuteAsync(request, successMessage);
        }

        public async Task<ActionResult<ApiResponse<T>>> TestExecuteCreatedAsync<T>(IRequest<T> request, string actionName, object routeValues, string successMessage = "Resource created successfully")
        {
            return await ExecuteCreatedAsync(request, actionName, routeValues, successMessage);
        }
    }

    // Test request for MediatR
    private class TestRequest : IRequest<string>
    {
    }

    private class TestRequestInt : IRequest<int>
    {
    }
}