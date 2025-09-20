using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.MessageQueue;

public class MessagePublisher : IMessagePublisher
{
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(IMessageQueueService messageQueueService, ILogger<MessagePublisher> logger)
    {
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Publishing message of type {MessageType} to queue {QueueName}", 
                typeof(T).Name, queueName);
            
            await _messageQueueService.PublishAsync(message, queueName, cancellationToken);
            
            _logger.LogDebug("Successfully published message of type {MessageType} to queue {QueueName}", 
                typeof(T).Name, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType} to queue {QueueName}", 
                typeof(T).Name, queueName);
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, string queueName, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Publishing delayed message of type {MessageType} to queue {QueueName} with delay {Delay}", 
                typeof(T).Name, queueName, delay);
            
            await _messageQueueService.PublishAsync(message, queueName, delay, cancellationToken);
            
            _logger.LogDebug("Successfully published delayed message of type {MessageType} to queue {QueueName} with delay {Delay}", 
                typeof(T).Name, queueName, delay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish delayed message of type {MessageType} to queue {QueueName} with delay {Delay}", 
                typeof(T).Name, queueName, delay);
            throw;
        }
    }
}