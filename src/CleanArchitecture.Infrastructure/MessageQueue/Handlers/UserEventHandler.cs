using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.MessageQueue.Messages;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.MessageQueue.Handlers;

public class UserEventHandler
{
    private readonly ILogger<UserEventHandler> _logger;
    private readonly ICacheService _cacheService;

    public UserEventHandler(ILogger<UserEventHandler> logger, ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<bool> HandleUserCreatedAsync(UserCreatedMessage message)
    {
        try
        {
            _logger.LogInformation("Processing user created event for user {UserId}: {Name} ({Email})", 
                message.UserId, message.Name, message.Email);

            // Example: Invalidate user-related cache
            var cacheKey = $"user:{message.UserId}";
            await _cacheService.RemoveAsync(cacheKey);

            // Example: Send welcome email (would integrate with email service)
            _logger.LogInformation("Welcome email would be sent to {Email}", message.Email);

            // Example: Update user statistics
            await UpdateUserStatisticsAsync();

            _logger.LogInformation("Successfully processed user created event for user {UserId}", message.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user created event for user {UserId}", message.UserId);
            return false;
        }
    }

    public async Task<bool> HandleUserUpdatedAsync(UserUpdatedMessage message)
    {
        try
        {
            _logger.LogInformation("Processing user updated event for user {UserId}: {Name} ({Email})", 
                message.UserId, message.Name, message.Email);

            // Example: Invalidate user-related cache
            var cacheKey = $"user:{message.UserId}";
            await _cacheService.RemoveAsync(cacheKey);

            // Example: Log changes
            if (message.Changes != null)
            {
                foreach (var change in message.Changes)
                {
                    _logger.LogInformation("User {UserId} field {Field} changed to {NewValue}", 
                        message.UserId, change.Key, change.Value);
                }
            }

            _logger.LogInformation("Successfully processed user updated event for user {UserId}", message.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user updated event for user {UserId}", message.UserId);
            return false;
        }
    }

    public async Task<bool> HandleUserDeletedAsync(UserDeletedMessage message)
    {
        try
        {
            _logger.LogInformation("Processing user deleted event for user {UserId} ({Email})", 
                message.UserId, message.Email);

            // Example: Clean up user-related cache
            var cacheKey = $"user:{message.UserId}";
            await _cacheService.RemoveAsync(cacheKey);

            // Example: Clean up user sessions
            var sessionCacheKey = $"user_sessions:{message.UserId}";
            await _cacheService.RemoveAsync(sessionCacheKey);

            // Example: Update user statistics
            await UpdateUserStatisticsAsync();

            _logger.LogInformation("Successfully processed user deleted event for user {UserId}", message.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user deleted event for user {UserId}", message.UserId);
            return false;
        }
    }

    private async Task UpdateUserStatisticsAsync()
    {
        // Example: Update cached user count
        var statsKey = "user_statistics";
        await _cacheService.RemoveAsync(statsKey);
        
        _logger.LogDebug("User statistics cache invalidated");
        await Task.CompletedTask;
    }
}

public class EmailNotificationHandler
{
    private readonly ILogger<EmailNotificationHandler> _logger;

    public EmailNotificationHandler(ILogger<EmailNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleEmailNotificationAsync(EmailNotificationMessage message)
    {
        try
        {
            _logger.LogInformation("Processing email notification to {To}: {Subject}", 
                message.To, message.Subject);

            // Example: Validate email address
            if (string.IsNullOrWhiteSpace(message.To) || !message.To.Contains("@"))
            {
                _logger.LogWarning("Invalid email address: {Email}", message.To);
                return false;
            }

            // Example: Send email (would integrate with actual email service)
            await SendEmailAsync(message);

            _logger.LogInformation("Successfully sent email notification to {To}", message.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification to {To}", message.To);
            return false;
        }
    }

    private async Task SendEmailAsync(EmailNotificationMessage message)
    {
        // Simulate email sending delay
        await Task.Delay(100);
        
        _logger.LogDebug("Email sent to {To} with subject '{Subject}'", message.To, message.Subject);
    }
}