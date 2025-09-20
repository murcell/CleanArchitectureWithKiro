using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CleanArchitecture.Application.DTOs.Responses;

namespace CleanArchitecture.WebAPI.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected readonly ILogger Logger;

    protected BaseController(IMediator mediator, ILogger logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    /// <summary>
    /// Creates a successful API response
    /// </summary>
    /// <typeparam name="T">Response data type</typeparam>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>Successful API response</returns>
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Operation completed successfully")
    {
        return Ok(new ApiResponse<T>
        {
            Data = data,
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Creates a created API response for POST operations
    /// </summary>
    /// <typeparam name="T">Response data type</typeparam>
    /// <param name="data">Response data</param>
    /// <param name="actionName">Action name for location header</param>
    /// <param name="routeValues">Route values for location header</param>
    /// <param name="message">Success message</param>
    /// <returns>Created API response</returns>
    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(T data, string actionName, object routeValues, string message = "Resource created successfully")
    {
        return CreatedAtAction(actionName, routeValues, new ApiResponse<T>
        {
            Data = data,
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Creates a not found API response
    /// </summary>
    /// <param name="message">Not found message</param>
    /// <returns>Not found API response</returns>
    protected ActionResult<ApiResponse<object>> NotFoundResponse(string message = "Resource not found")
    {
        return NotFound(new ApiResponse<object>
        {
            Data = null,
            Success = false,
            Message = message
        });
    }

    /// <summary>
    /// Creates a bad request API response
    /// </summary>
    /// <param name="message">Bad request message</param>
    /// <returns>Bad request API response</returns>
    protected ActionResult<ApiResponse<object>> BadRequestResponse(string message = "Invalid request")
    {
        return BadRequest(new ApiResponse<object>
        {
            Data = null,
            Success = false,
            Message = message
        });
    }

    /// <summary>
    /// Executes a command or query and handles common exceptions
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="request">MediatR request</param>
    /// <param name="successMessage">Success message</param>
    /// <returns>API response</returns>
    protected async Task<ActionResult<ApiResponse<T>>> ExecuteAsync<T>(IRequest<T> request, string successMessage = "Operation completed successfully")
    {
        try
        {
            var result = await Mediator.Send(request);
            return SuccessResponse(result, successMessage);
        }
        catch (KeyNotFoundException ex)
        {
            Logger.LogWarning(ex, "Resource not found");
            return NotFound(new ApiResponse<T>
            {
                Data = default(T),
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Invalid operation");
            return BadRequest(new ApiResponse<T>
            {
                Data = default(T),
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred");
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Executes a command or query and returns created response
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="request">MediatR request</param>
    /// <param name="actionName">Action name for location header</param>
    /// <param name="routeValues">Route values for location header</param>
    /// <param name="successMessage">Success message</param>
    /// <returns>Created API response</returns>
    protected async Task<ActionResult<ApiResponse<T>>> ExecuteCreatedAsync<T>(IRequest<T> request, string actionName, object routeValues, string successMessage = "Resource created successfully")
    {
        try
        {
            var result = await Mediator.Send(request);
            return CreatedResponse(result, actionName, routeValues, successMessage);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Invalid operation during creation");
            return BadRequest(new ApiResponse<T>
            {
                Data = default(T),
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred during creation");
            throw; // Let global exception handler deal with it
        }
    }
}