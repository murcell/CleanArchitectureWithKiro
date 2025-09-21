using CleanArchitecture.Application.DTOs.Responses;
using CleanArchitecture.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace CleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. RequestPath: {RequestPath}", 
                context.Request.Path);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = GetErrorMessage(exception),
            Data = null,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case FluentValidation.ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Validation failed";
                response.Errors = validationEx.Errors.Select(e => new ApiError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage,
                    Code = e.ErrorCode
                }).ToList();
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = notFoundEx.Message;
                break;

            case DomainException domainEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = domainEx.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = argEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = invalidOpEx.Message;
                break;

            case TimeoutException:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Message = "Request timeout";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An internal server error occurred";
                break;
        }

        // Add correlation ID if available
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            response.CorrelationId = correlationId.FirstOrDefault();
        }

        // Add stack trace in development
        if (_environment.IsDevelopment() && exception is not FluentValidation.ValidationException)
        {
            response.Details = exception.StackTrace;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException => "One or more validation errors occurred",
            NotFoundException => exception.Message,
            DomainException => exception.Message,
            UnauthorizedAccessException => "Access denied",
            ArgumentException => "Invalid argument provided",
            InvalidOperationException => "Invalid operation",
            TimeoutException => "The request timed out",
            _ => "An error occurred while processing your request"
        };
    }
}