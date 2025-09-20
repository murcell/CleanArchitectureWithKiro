using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.MessageQueue.Handlers;
using CleanArchitecture.Infrastructure.MessageQueue.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.MessageQueue.Services;

public class MessageQueueBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageQueueBackgroundService> _logger;

    public MessageQueueBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MessageQueueBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageQueue Background Service starting");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var userEventHandler = scope.ServiceProvider.GetRequiredService<UserEventHandler>();
            var emailNotificationHandler = scope.ServiceProvider.GetRequiredService<EmailNotificationHandler>();

            // Set up message consumers
            await SetupConsumersAsync(messageConsumer, userEventHandler, emailNotificationHandler, stoppingToken);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MessageQueue Background Service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MessageQueue Background Service encountered an error");
            throw;
        }
    }

    private async Task SetupConsumersAsync(
        IMessageConsumer messageConsumer,
        UserEventHandler userEventHandler,
        EmailNotificationHandler emailNotificationHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            // User event consumers
            await messageConsumer.StartConsumingAsync<UserCreatedMessage>(
                "user.created",
                userEventHandler.HandleUserCreatedAsync,
                cancellationToken);

            await messageConsumer.StartConsumingAsync<UserUpdatedMessage>(
                "user.updated",
                userEventHandler.HandleUserUpdatedAsync,
                cancellationToken);

            await messageConsumer.StartConsumingAsync<UserDeletedMessage>(
                "user.deleted",
                userEventHandler.HandleUserDeletedAsync,
                cancellationToken);

            // Email notification consumer
            await messageConsumer.StartConsumingAsync<EmailNotificationMessage>(
                "email.notification",
                emailNotificationHandler.HandleEmailNotificationAsync,
                cancellationToken);

            _logger.LogInformation("All message consumers have been set up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up message consumers");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MessageQueue Background Service stopping");
        await base.StopAsync(cancellationToken);
    }
}