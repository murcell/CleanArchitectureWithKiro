using Asp.Versioning;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Application.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT access token and refresh token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);
        
        var command = new LoginCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Login successful for email: {Email}", request.Email);
        return Ok(response);
    }

    /// <summary>
    /// Test endpoint to check if controller is reachable
    /// </summary>
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Ok("Auth controller is working!");
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT access token and refresh token</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);
        
        var command = new RegisterCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Registration successful for email: {Email}", request.Email);
        return CreatedAtAction(nameof(Login), response);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New JWT access token and refresh token</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh attempt");
        
        var command = new RefreshTokenCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Token refresh successful");
        return Ok(response);
    }

    /// <summary>
    /// Logs out the current user by invalidating their refresh token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("user_id")?.Value;
        _logger.LogInformation("Logout attempt for user: {UserId}", userId);
        
        var command = new LogoutCommand(int.Parse(userId));
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result)
        {
            return BadRequest("Logout failed");
        }
        
        _logger.LogInformation("Logout successful for user: {UserId}", userId);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Gets the current user's profile information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("user_id")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var authMethod = User.FindFirst(System.Security.Claims.ClaimTypes.AuthenticationMethod)?.Value;

        return Ok(new
        {
            userId,
            email,
            name,
            role,
            authenticationMethod = authMethod,
            isAuthenticated = User.Identity?.IsAuthenticated == true
        });
    }
}