using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using CleanArchitecture.Infrastructure.Configuration;

namespace CleanArchitecture.Infrastructure.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly MessageQueueOptions _options;

    public RabbitMQHealthCheck(IOptions<MessageQueueOptions> options)
    {
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(10)
            };

            using var connection = await Task.Run(() => factory.CreateConnection(), cancellationToken);
            
            if (connection.IsOpen)
            {
                using var channel = await Task.Run(() => connection.CreateModel(), cancellationToken);
                
                // Test basic channel operations
                var testQueueName = $"health_check_test_{Guid.NewGuid()}";
                
                // Declare a temporary queue
                await Task.Run(() => channel.QueueDeclare(
                    queue: testQueueName,
                    durable: false,
                    exclusive: true,
                    autoDelete: true,
                    arguments: null), cancellationToken);
                
                // Delete the test queue
                await Task.Run(() => channel.QueueDelete(testQueueName), cancellationToken);
                
                return HealthCheckResult.Healthy($"RabbitMQ is healthy. Connected to {_options.HostName}:{_options.Port}");
            }
            else
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection could not be established.");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed.", ex);
        }
    }
}