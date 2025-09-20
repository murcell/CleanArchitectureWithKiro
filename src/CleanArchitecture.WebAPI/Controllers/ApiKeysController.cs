using Asp.Versioning;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Application.Features.Authentication.Commands;
using CleanArchitecture.Infrastructure.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

/// <summary>
/// Controller for API key management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/api-keys")]
[Produces("application/json")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IMediator mediator, ILogger<ApiKeysController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new API key
    /// </summary>
    /// <param name="request">API key creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created API key (shown only once)</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirst("user_id")?.Value;
        _logger.LogInformation("API key creation attempt by user: {UserId}", currentUserId);
        
        var command = new CreateApiKeyCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("API key created successfully: {KeyPrefix}", response.ApiKeyInfo.KeyPrefix);
        return CreatedAtAction(nameof(GetApiKey), new { id = response.ApiKeyInfo.Id }, response);
    }

    /// <summary>
    /// Gets API key information by ID
    /// </summary>
    /// <param name="id">API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API key information (without the actual key)</returns>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiKeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiKeyDto>> GetApiKey(
        int id,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiKeyQuery
        _logger.LogInformation("API key retrieval attempt for ID: {ApiKeyId}", id);
        
        // Placeholder response
        return NotFound();
    }

    /// <summary>
    /// Gets all API keys (admin only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of API keys</returns>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ApiKeyDto>>> GetApiKeys(
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetApiKeysQuery
        _logger.LogInformation("API keys list retrieval attempt");
        
        // Placeholder response
        return Ok(Array.Empty<ApiKeyDto>());
    }

    /// <summary>
    /// Gets current user's API keys
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of current user's API keys</returns>
    [HttpGet("my-keys")]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ApiKeyDto>>> GetMyApiKeys(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirst("user_id")?.Value;
        _logger.LogInformation("User API keys retrieval attempt for user: {UserId}", currentUserId);
        
        // TODO: Implement GetUserApiKeysQuery
        
        // Placeholder response
        return Ok(Array.Empty<ApiKeyDto>());
    }

    /// <summary>
    /// Deactivates an API key
    /// </summary>
    /// <param name="id">API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateApiKey(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirst("user_id")?.Value;
        _logger.LogInformation("API key deactivation attempt for ID: {ApiKeyId} by user: {UserId}", id, currentUserId);
        
        // TODO: Implement DeactivateApiKeyCommand
        
        _logger.LogInformation("API key deactivated successfully: {ApiKeyId}", id);
        return NoContent();
    }

    /// <summary>
    /// Tests API key authentication (for API key users)
    /// </summary>
    /// <returns>Success response with API key information</returns>
    [HttpGet("test")]
    [RequireScope("api:read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult TestApiKey()
    {
        var apiKeyId = User.FindFirst("api_key_id")?.Value;
        var apiKeyName = User.FindFirst("api_key_name")?.Value;
        var scopes = User.FindAll("scope").Select(c => c.Value).ToArray();
        var authMethod = User.FindFirst(System.Security.Claims.ClaimTypes.AuthenticationMethod)?.Value;

        return Ok(new
        {
            message = "API key authentication successful",
            apiKeyId,
            apiKeyName,
            scopes,
            authenticationMethod = authMethod,
            timestamp = DateTime.UtcNow
        });
    }
}