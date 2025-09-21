using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.Application.Features.Products.Commands;
using CleanArchitecture.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

/// <summary>
/// Controller for product management operations
/// </summary>
public class ProductsController : BaseController
{
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger) 
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Creates a new product with automatic validation
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <returns>Created product response</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        Logger.LogInformation("Creating product with name: {Name}", request.Name);
        
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.Stock,
            request.UserId);
            
        var productId = await Mediator.Send(command);
        
        return CreatedResponse(productId, nameof(GetProduct), new { id = productId }, "Product created successfully");
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
    {
        Logger.LogInformation("Getting product with ID: {ProductId}", id);
        
        var query = new GetProductQuery(id);
        var product = await Mediator.Send(query);
        
        return SuccessResponse(product, "Product retrieved successfully");
    }

    /// <summary>
    /// Gets all products with optional pagination and filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="isAvailable">Filter by availability (optional)</param>
    /// <returns>List of products</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetProducts(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] int? userId = null,
        [FromQuery] bool? isAvailable = null)
    {
        Logger.LogInformation("Getting products - Page: {Page}, PageSize: {PageSize}, UserId: {UserId}, IsAvailable: {IsAvailable}", 
            page, pageSize, userId, isAvailable);
        
        var query = new GetProductsQuery(page, pageSize, userId, isAvailable);
        var products = await Mediator.Send(query);
        
        return SuccessResponse(products, "Products retrieved successfully");
    }

    /// <summary>
    /// Updates an existing product with automatic validation
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <returns>Update result</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        Logger.LogInformation("Updating product {ProductId} with name: {Name}", id, request.Name);
        
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.Stock);
            
        var result = await Mediator.Send(command);
        
        return SuccessResponse(result, "Product updated successfully");
    }

    /// <summary>
    /// Deletes a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
    {
        Logger.LogInformation("Deleting product with ID: {ProductId}", id);
        
        var command = new DeleteProductCommand(id);
        var result = await Mediator.Send(command);
        
        return SuccessResponse(result, "Product deleted successfully");
    }

    /// <summary>
    /// Updates product stock
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="stock">New stock amount</param>
    /// <returns>Update result</returns>
    [HttpPatch("{id}/stock")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProductStock(int id, [FromBody] int stock)
    {
        Logger.LogInformation("Updating stock for product {ProductId} to {Stock}", id, stock);
        
        var command = new UpdateProductStockCommand(id, stock);
        var result = await Mediator.Send(command);
        
        return SuccessResponse(result, "Product stock updated successfully");
    }

    /// <summary>
    /// Toggles product availability
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Update result</returns>
    [HttpPatch("{id}/availability")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleProductAvailability(int id)
    {
        Logger.LogInformation("Toggling availability for product {ProductId}", id);
        
        var command = new ToggleProductAvailabilityCommand(id);
        var result = await Mediator.Send(command);
        
        return SuccessResponse(result, "Product availability toggled successfully");
    }
}