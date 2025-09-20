namespace CleanArchitecture.Application.Common.Interfaces;

public interface IApplicationLogger<T>
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogTrace(string message, params object[] args);
    
    // Performance logging
    void LogPerformance(string operation, long elapsedMs, params object[] args);
    void LogSlowOperation(string operation, long elapsedMs, long thresholdMs, params object[] args);
    
    // Business event logging
    void LogBusinessEvent(string eventName, object eventData);
    void LogUserAction(string userId, string action, object? data = null);
    
    // Security logging
    void LogSecurityEvent(string eventType, string details, string? userId = null);
    void LogAuthenticationEvent(string eventType, string userId, bool success, string? details = null);
}