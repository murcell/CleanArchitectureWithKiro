using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Logging;

public class ApplicationLogger<T> : IApplicationLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApplicationLogger(ILogger<T> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.LogError(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void LogTrace(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }

    public void LogPerformance(string operation, long elapsedMs, params object[] args)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "Performance",
            ["Operation"] = operation,
            ["ElapsedMs"] = elapsedMs
        });

        _logger.LogInformation("Performance: {Operation} completed in {ElapsedMs}ms", operation, elapsedMs);
    }

    public void LogSlowOperation(string operation, long elapsedMs, long thresholdMs, params object[] args)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "SlowOperation",
            ["Operation"] = operation,
            ["ElapsedMs"] = elapsedMs,
            ["ThresholdMs"] = thresholdMs
        });

        _logger.LogWarning("Slow operation detected: {Operation} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)", 
            operation, elapsedMs, thresholdMs);
    }

    public void LogBusinessEvent(string eventName, object eventData)
    {
        var serializedData = JsonSerializer.Serialize(eventData, _jsonOptions);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "BusinessEvent",
            ["EventName"] = eventName,
            ["EventData"] = serializedData
        });

        _logger.LogInformation("Business event: {EventName} occurred with data: {EventData}", 
            eventName, serializedData);
    }

    public void LogUserAction(string userId, string action, object? data = null)
    {
        var serializedData = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : null;
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "UserAction",
            ["UserId"] = userId,
            ["Action"] = action,
            ["ActionData"] = serializedData ?? "null"
        });

        _logger.LogInformation("User action: User {UserId} performed {Action}", userId, action);
    }

    public void LogSecurityEvent(string eventType, string details, string? userId = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "SecurityEvent",
            ["SecurityEventType"] = eventType,
            ["UserId"] = userId ?? "Anonymous",
            ["Details"] = details
        });

        _logger.LogWarning("Security event: {EventType} - {Details} (User: {UserId})", 
            eventType, details, userId ?? "Anonymous");
    }

    public void LogAuthenticationEvent(string eventType, string userId, bool success, string? details = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AuthenticationEvent",
            ["AuthEventType"] = eventType,
            ["UserId"] = userId,
            ["Success"] = success,
            ["Details"] = details ?? "No additional details"
        });

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(logLevel, "Authentication event: {EventType} for user {UserId} - Success: {Success}", 
            eventType, userId, success);
    }
}