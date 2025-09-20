using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CleanArchitecture.Infrastructure.MessageQueue;

public class MessageConsumer : IMessageConsumer, IHostedService
{
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<MessageConsumer> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumerCancellationTokens;

    public MessageConsumer(IMessageQueueService messageQueueService, ILogger<MessageConsumer> logger)
    {
        _messageQueueService = messageQueueService;
        _logger = logger;
        _consumerCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    public async Task StartConsumingAsync<T>(string queueName, Func<T, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting consumer for queue {QueueName} with message type {MessageType}", 
                queueName, typeof(T).Name);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumerCancellationTokens.TryAdd(queueName, cts);

            await _messageQueueService.SubscribeAsync(queueName, handler, cts.Token);
            
            _logger.LogInformation("Successfully started consumer for queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consumer for queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task StopConsumingAsync(string queueName)
    {
        try
        {
            _logger.LogInformation("Stopping consumer for queue {QueueName}", queueName);

            if (_consumerCancellationTokens.TryRemove(queueName, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            await _messageQueueService.UnsubscribeAsync(queueName);
            
            _logger.LogInformation("Successfully stopped consumer for queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop consumer for queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MessageConsumer hosted service starting");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MessageConsumer hosted service stopping");

        var stopTasks = _consumerCancellationTokens.Keys
            .Select(queueName => StopConsumingAsync(queueName))
            .ToArray();

        await Task.WhenAll(stopTasks);
        
        _logger.LogInformation("MessageConsumer hosted service stopped");
    }
}