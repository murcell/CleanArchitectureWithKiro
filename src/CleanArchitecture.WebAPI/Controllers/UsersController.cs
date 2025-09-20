using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.Application.Features.Users.Commands.CreateUser;
using CleanArchitecture.Application.Features.Users.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

/// <summary>
/// Controller for user management operations
/// Demonstrates validation infrastructure integration
/// </summary>
public class UsersController : BaseController
{
    public UsersController(IMediator mediator, ILogger<UsersController> logger) 
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Creates a new user with automatic validation
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user response</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> CreateUser([FromBody] CreateUserRequest request)
    {
        Logger.LogInformation("Creating user with email: {Email}", request.Email);
        
        var command = new CreateUserCommand(request.Name, request.Email);
        return await ExecuteCreatedAsync(command, nameof(GetUser), new { id = 0 }, "User created successfully");
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        Logger.LogInformation("Getting user with ID: {UserId}", id);
        
        var query = new GetUserQuery(id);
        return await ExecuteAsync(query, "User retrieved successfully");
    }

    /// <summary>
    /// Updates an existing user with automatic validation
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Update result</returns>
    [HttpPut("{id}")]
    public Task<ActionResult<ApiResponse<bool>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        Logger.LogInformation("Updating user {UserId} with email: {Email}", id, request.Email);
        
        // Note: UpdateUserCommand needs to be implemented in the Application layer
        // For now, we'll return a placeholder response
        return Task.FromResult(SuccessResponse(true, "User updated successfully"));
    }

    /// <summary>
    /// Gets all users with optional pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>List of users</returns>
    [HttpGet]
    public Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        Logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        
        // Note: GetUsersQuery needs to be implemented in the Application layer
        // For now, we'll return a placeholder response
        return Task.FromResult(SuccessResponse(Enumerable.Empty<UserDto>(), "Users retrieved successfully"));
    }

    /// <summary>
    /// Deletes a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
    {
        Logger.LogInformation("Deleting user with ID: {UserId}", id);
        
        // Note: DeleteUserCommand needs to be implemented in the Application layer
        // For now, we'll return a placeholder response
        return Task.FromResult(SuccessResponse(true, "User deleted successfully"));
    }
}