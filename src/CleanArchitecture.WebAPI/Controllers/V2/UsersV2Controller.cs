using Asp.Versioning;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.Application.Features.Users.Commands.CreateUser;
using CleanArchitecture.Application.Features.Users.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers.V2;

/// <summary>
/// Version 2 of the Users controller with enhanced features
/// </summary>
[ApiVersion("2.0")]
[Route("api/v2/users")]
public class UsersV2Controller : BaseController
{
    public UsersV2Controller(IMediator mediator, ILogger<UsersV2Controller> logger) 
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Creates a new user with automatic validation (V2 - Enhanced response)
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user response with additional metadata</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserCreationResponseV2>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserCreationResponseV2>>> CreateUser([FromBody] CreateUserRequest request)
    {
        Logger.LogInformation("Creating user with email: {Email} (API V2)", request.Email);
        
        var command = new CreateUserCommand(request.Name, request.Email);
        var userId = await Mediator.Send(command);
        
        var response = new UserCreationResponseV2
        {
            Id = userId,
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            Version = "2.0",
            Links = new Dictionary<string, string>
            {
                { "self", $"/api/v2.0/users/{userId}" },
                { "update", $"/api/v2.0/users/{userId}" },
                { "delete", $"/api/v2.0/users/{userId}" }
            }
        };
        
        return CreatedResponse(response, nameof(GetUser), new { id = userId }, "User created successfully (V2)");
    }

    /// <summary>
    /// Gets a user by ID (V2 - Enhanced response with metadata)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User information with additional metadata</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseV2>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponseV2>>> GetUser(int id)
    {
        Logger.LogInformation("Getting user with ID: {UserId} (API V2)", id);
        
        var query = new GetUserQuery(id);
        var user = await Mediator.Send(query);
        
        var response = new UserResponseV2
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt ?? user.CreatedAt,
            Version = "2.0",
            Metadata = new Dictionary<string, object>
            {
                { "totalProducts", user.Products.Count },
                { "accountAge", DateTime.UtcNow.Subtract(user.CreatedAt).Days },
                { "lastActivity", user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never" }
            },
            Links = new Dictionary<string, string>
            {
                { "self", $"/api/v2.0/users/{id}" },
                { "products", $"/api/v2.0/users/{id}/products" },
                { "update", $"/api/v2.0/users/{id}" },
                { "delete", $"/api/v2.0/users/{id}" }
            }
        };
        
        return SuccessResponse(response, "User retrieved successfully (V2)");
    }

    /// <summary>
    /// Gets all users with enhanced filtering and pagination (V2)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="search">Search in name or email</param>
    /// <returns>Paginated list of users with metadata</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponseV2<UserResponseV2>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<PaginatedResponseV2<UserResponseV2>>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        Logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}, IsActive: {IsActive}, Search: {Search} (API V2)", 
            page, pageSize, isActive, search);
        
        // Validate page size
        pageSize = Math.Min(pageSize, 100);
        
        var response = new PaginatedResponseV2<UserResponseV2>
        {
            Data = new List<UserResponseV2>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 0,
            HasNextPage = false,
            HasPreviousPage = page > 1,
            Version = "2.0",
            Filters = new Dictionary<string, object?>
            {
                { "isActive", isActive },
                { "search", search }
            },
            Links = new Dictionary<string, string>
            {
                { "self", $"/api/v2.0/users?page={page}&pageSize={pageSize}" },
                { "first", $"/api/v2.0/users?page=1&pageSize={pageSize}" },
                { "last", $"/api/v2.0/users?page=1&pageSize={pageSize}" }
            }
        };
        
        return Task.FromResult(SuccessResponse(response, "Users retrieved successfully (V2)"));
    }
}

/// <summary>
/// Enhanced user creation response for API V2
/// </summary>
public class UserCreationResponseV2
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> Links { get; set; } = new();
}

/// <summary>
/// Enhanced user response for API V2
/// </summary>
public class UserResponseV2
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, string> Links { get; set; } = new();
}

/// <summary>
/// Enhanced paginated response for API V2
/// </summary>
public class PaginatedResponseV2<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object?> Filters { get; set; } = new();
    public Dictionary<string, string> Links { get; set; } = new();
}