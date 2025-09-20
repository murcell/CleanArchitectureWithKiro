using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.MessageQueue;

public class RabbitMQService : IMessageQueueService, IMessagePublisher, IMessageConsumer, IDisposable
{
    private readonly MessageQueueOptions _options;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ConcurrentDictionary<string, EventingBasicConsumer> _consumers;
    private readonly ConcurrentDictionary<string, string> _consumerTags;
    private readonly JsonSerializerOptions _serializerOptions;
    private bool _disposed;

    public RabbitMQService(IOptions<MessageQueueOptions> options, ILogger<RabbitMQService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _consumers = new ConcurrentDictionary<string, EventingBasicConsumer>();
        _consumerTags = new ConcurrentDictionary<string, string>();
        
        _serializerOptions = _options.SerializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        await PublishInternalAsync(message, queueName, null, cancellationToken);
    }

    public async Task PublishAsync<T>(T message, string queueName, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await PublishInternalAsync(message, queueName, delay, cancellationToken);
    }

    private async Task PublishInternalAsync<T>(T message, string queueName, TimeSpan? delay, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQService));

        try
        {
            // Declare queue with dead letter exchange if enabled
            var queueArguments = new Dictionary<string, object>();
            
            if (_options.EnableDeadLetterQueue)
            {
                var deadLetterExchange = $"{queueName}.dlx";
                var deadLetterQueue = $"{queueName}.dlq";
                
                // Declare dead letter exchange and queue
                _channel.ExchangeDeclare(deadLetterExchange, ExchangeType.Direct, durable: true);
                _channel.QueueDeclare(deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(deadLetterQueue, deadLetterExchange, deadLetterQueue);
                
                queueArguments["x-dead-letter-exchange"] = deadLetterExchange;
                queueArguments["x-dead-letter-routing-key"] = deadLetterQueue;
            }

            // Handle delayed messages
            if (delay.HasValue)
            {
                var delayedExchange = $"{queueName}.delayed";
                var delayedQueue = $"{queueName}.delayed";
                
                _channel.ExchangeDeclare(delayedExchange, ExchangeType.Direct, durable: true);
                
                var delayedQueueArgs = new Dictionary<string, object>(queueArguments)
                {
                    ["x-message-ttl"] = (int)delay.Value.TotalMilliseconds,
                    ["x-dead-letter-exchange"] = "",
                    ["x-dead-letter-routing-key"] = queueName
                };
                
                _channel.QueueDeclare(delayedQueue, durable: true, exclusive: false, autoDelete: false, delayedQueueArgs);
                _channel.QueueBind(delayedQueue, delayedExchange, delayedQueue);
                
                queueName = delayedQueue;
            }

            // Declare main queue
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, queueArguments);

            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message, _serializerOptions);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = typeof(T).Name;

            _channel.BasicPublish(
                exchange: delay.HasValue ? $"{queueName.Replace(".delayed", "")}.delayed" : "",
                routingKey: queueName,
                basicProperties: properties,
                body: messageBody);

            _logger.LogDebug("Message published to queue {QueueName} with ID {MessageId}", 
                queueName, properties.MessageId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(string queueName, Func<T, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        await StartConsumingAsync(queueName, handler, cancellationToken);
    }

    public async Task StartConsumingAsync<T>(string queueName, Func<T, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQService));

        try
        {
            // Declare queue with dead letter exchange if enabled
            var queueArguments = new Dictionary<string, object>();
            
            if (_options.EnableDeadLetterQueue)
            {
                var deadLetterExchange = $"{queueName}.dlx";
                var deadLetterQueue = $"{queueName}.dlq";
                
                _channel.ExchangeDeclare(deadLetterExchange, ExchangeType.Direct, durable: true);
                _channel.QueueDeclare(deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(deadLetterQueue, deadLetterExchange, deadLetterQueue);
                
                queueArguments["x-dead-letter-exchange"] = deadLetterExchange;
                queueArguments["x-dead-letter-routing-key"] = deadLetterQueue;
            }

            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, queueArguments);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                var messageId = ea.BasicProperties?.MessageId ?? "unknown";
                var retryCount = GetRetryCount(ea.BasicProperties);
                
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(messageJson, _serializerOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Received null message from queue {QueueName}, message ID: {MessageId}", 
                            queueName, messageId);
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogDebug("Processing message from queue {QueueName}, message ID: {MessageId}, retry: {RetryCount}", 
                        queueName, messageId, retryCount);

                    var success = await handler(message);
                    
                    if (success)
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogDebug("Message processed successfully from queue {QueueName}, message ID: {MessageId}", 
                            queueName, messageId);
                    }
                    else
                    {
                        await HandleMessageFailure(ea, queueName, messageId, retryCount, "Handler returned false");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}, message ID: {MessageId}, retry: {RetryCount}", 
                        queueName, messageId, retryCount);
                    
                    await HandleMessageFailure(ea, queueName, messageId, retryCount, ex.Message);
                }
            };

            var consumerTag = _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _consumers.TryAdd(queueName, consumer);
            _consumerTags.TryAdd(queueName, consumerTag);
            
            _logger.LogInformation("Started consuming messages from queue {QueueName} with consumer tag {ConsumerTag}", 
                queueName, consumerTag);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming from queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string queueName)
    {
        await StopConsumingAsync(queueName);
    }

    public async Task StopConsumingAsync(string queueName)
    {
        if (_consumers.TryRemove(queueName, out var consumer) && 
            _consumerTags.TryRemove(queueName, out var consumerTag))
        {
            try
            {
                // Cancel the consumer using the stored consumer tag
                _channel.BasicCancel(consumerTag);
                
                _logger.LogInformation("Stopped consuming from queue {QueueName} with consumer tag {ConsumerTag}", 
                    queueName, consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping consumer for queue {QueueName}", queueName);
            }
        }

        await Task.CompletedTask;
    }

    private async Task HandleMessageFailure(BasicDeliverEventArgs ea, string queueName, string messageId, int retryCount, string errorMessage)
    {
        if (retryCount < _options.MaxRetryAttempts)
        {
            // Retry with delay
            _logger.LogWarning("Message processing failed, scheduling retry {RetryCount}/{MaxRetries} for message ID: {MessageId} from queue {QueueName}. Error: {Error}", 
                retryCount + 1, _options.MaxRetryAttempts, messageId, queueName, errorMessage);

            // Reject and requeue with delay
            _channel.BasicNack(ea.DeliveryTag, false, false);
            
            // Republish with retry count incremented
            await Task.Delay(_options.RetryDelay);
            await RepublishWithRetry(ea, queueName, retryCount + 1);
        }
        else
        {
            // Max retries exceeded, send to dead letter queue or discard
            _logger.LogError("Message processing failed after {MaxRetries} attempts, message ID: {MessageId} from queue {QueueName}. Error: {Error}", 
                _options.MaxRetryAttempts, messageId, queueName, errorMessage);
            
            _channel.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    private async Task RepublishWithRetry(BasicDeliverEventArgs ea, string queueName, int retryCount)
    {
        try
        {
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = ea.BasicProperties?.Type ?? "Unknown";
            
            // Add retry count to headers
            properties.Headers = new Dictionary<string, object>
            {
                ["x-retry-count"] = retryCount
            };

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: ea.Body);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to republish message for retry");
        }
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers?.TryGetValue("x-retry-count", out var retryCountObj) == true)
        {
            return retryCountObj switch
            {
                int count => count,
                byte[] bytes => BitConverter.ToInt32(bytes, 0),
                _ => 0
            };
        }
        return 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            // Stop all consumers
            foreach (var queueName in _consumers.Keys.ToList())
            {
                StopConsumingAsync(queueName).GetAwaiter().GetResult();
            }

            _channel?.Close();
            _channel?.Dispose();
            
            _connection?.Close();
            _connection?.Dispose();
            
            _logger.LogInformation("RabbitMQ connection disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
        finally
        {
            _disposed = true;
        }
    }
}