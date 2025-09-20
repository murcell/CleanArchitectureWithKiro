namespace CleanArchitecture.Application.Common.Interfaces;

public interface IMessageQueueService
{
    Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(T message, string queueName, TimeSpan delay, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string queueName, Func<T, Task<bool>> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string queueName);
}

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(T message, string queueName, TimeSpan delay, CancellationToken cancellationToken = default);
}

public interface IMessageConsumer
{
    Task StartConsumingAsync<T>(string queueName, Func<T, Task<bool>> handler, CancellationToken cancellationToken = default);
    Task StopConsumingAsync(string queueName);
}