using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.WebAPI.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Controllers;

/// <summary>
/// Unit tests for ProductsController
/// </summary>
public class ProductsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _controller = new ProductsController(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateProduct_Should_Return_CreatedResult_For_Valid_Request()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Currency = "USD",
            Stock = 10,
            UserId = 1
        };

        // Act
        var result = await _controller.CreateProduct(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApiResponse<int>>(createdResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Product created successfully", response.Message);
        Assert.True(response.Data > 0);
    }

    [Fact]
    public async Task GetProduct_Should_Return_OkResult_With_Product_Data()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ProductDto>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Product retrieved successfully", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(productId, response.Data.Id);
        Assert.Equal("Sample Product", response.Data.Name);
    }

    [Fact]
    public async Task GetProducts_Should_Return_OkResult_With_Empty_List()
    {
        // Arrange & Act
        var result = await _controller.GetProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<ProductDto>>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Products retrieved successfully", response.Message);
        Assert.Empty(response.Data ?? Enumerable.Empty<ProductDto>());
    }

    [Fact]
    public async Task GetProducts_Should_Accept_Query_Parameters()
    {
        // Arrange
        var page = 2;
        var pageSize = 20;
        var userId = 1;
        var isAvailable = true;

        // Act
        var result = await _controller.GetProducts(page, pageSize, userId, isAvailable);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<ProductDto>>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Products retrieved successfully", response.Message);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return_OkResult_For_Valid_Request()
    {
        // Arrange
        var productId = 1;
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 149.99m,
            Currency = "USD",
            Stock = 5
        };

        // Act
        var result = await _controller.UpdateProduct(productId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Product updated successfully", response.Message);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task DeleteProduct_Should_Return_OkResult()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Product deleted successfully", response.Message);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Return_OkResult()
    {
        // Arrange
        var productId = 1;
        var newStock = 25;

        // Act
        var result = await _controller.UpdateProductStock(productId, newStock);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Product stock updated successfully", response.Message);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task ToggleProductAvailability_Should_Return_OkResult()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.ToggleProductAvailability(productId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Product availability toggled successfully", response.Message);
        Assert.True(response.Data);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 5)]
    public async Task GetProducts_Should_Handle_Different_Pagination_Parameters(int page, int pageSize)
    {
        // Act
        var result = await _controller.GetProducts(page, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<ProductDto>>>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Products retrieved successfully", response.Message);
    }
}