using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.DTOs.Responses;
using System.Net;
using System.Text.Json;
using System.Security;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Global exception handling middleware for the application
/// Handles validation exceptions and other application exceptions
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Log security-related exceptions with additional context
        if (IsSecurityException(exception))
        {
            _logger.LogWarning("Security exception occurred from IP {RemoteIpAddress}: {ExceptionType} - {Message}",
                context.Connection.RemoteIpAddress,
                exception.GetType().Name,
                exception.Message);
        }

        var response = exception switch
        {
            ValidationException validationEx => new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = SanitizeValidationErrors(validationEx.Errors.SelectMany(kvp => kvp.Value).ToList())
            },
            ArgumentException argEx => new ApiResponse<object>
            {
                Success = false,
                Message = SanitizeErrorMessage(argEx.Message)
            },
            UnauthorizedAccessException => new ApiResponse<object>
            {
                Success = false,
                Message = "Unauthorized access"
            },
            SecurityException => new ApiResponse<object>
            {
                Success = false,
                Message = "Security violation detected"
            },
            _ => new ApiResponse<object>
            {
                Success = false,
                Message = "An internal server error occurred"
            }
        };

        context.Response.StatusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            SecurityException => (int)HttpStatusCode.Forbidden,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static bool IsSecurityException(Exception exception)
    {
        return exception is SecurityException ||
               exception is UnauthorizedAccessException ||
               (exception is ValidationException validationEx && 
                validationEx.Errors.Any(kvp => kvp.Value.Any(error => 
                    error.Contains("dangerous") || 
                    error.Contains("XSS") || 
                    error.Contains("SQL"))));
    }

    private static List<string> SanitizeValidationErrors(List<string> errors)
    {
        // Remove potentially sensitive information from validation error messages
        return errors.Select(error => 
        {
            // Don't expose the actual malicious input in error messages
            if (error.Contains("dangerous"))
                return "Input contains invalid characters";
            
            return error;
        }).ToList();
    }

    private static string SanitizeErrorMessage(string message)
    {
        // Sanitize error messages to prevent information disclosure
        if (message.Contains("SQL") || message.Contains("database"))
            return "A data processing error occurred";
        
        if (message.Contains("file") || message.Contains("path"))
            return "A file processing error occurred";
        
        return message;
    }
}

/// <summary>
/// Extension method to register the global exception middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}