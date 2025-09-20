using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Configuration;

/// <summary>
/// Configuration options for RabbitMQ message queue
/// </summary>
public class MessageQueueOptions
{
    public const string SectionName = "RabbitMQ";
    
    /// <summary>
    /// RabbitMQ server hostname
    /// </summary>
    public string HostName { get; set; } = "localhost";
    
    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string UserName { get; set; } = "guest";
    
    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";
    
    /// <summary>
    /// RabbitMQ server port
    /// </summary>
    public int Port { get; set; } = 5672;
    
    /// <summary>
    /// RabbitMQ virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";
    
    /// <summary>
    /// Maximum retry attempts for failed messages
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Enable dead letter queue for failed messages
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;
    
    /// <summary>
    /// JSON serializer options for message serialization
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Enable automatic recovery
    /// </summary>
    public bool EnableAutomaticRecovery { get; set; } = true;
    
    /// <summary>
    /// Network recovery interval in seconds
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;
    
    /// <summary>
    /// Prefetch count for consumers
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
    
    /// <summary>
    /// Enable publisher confirms
    /// </summary>
    public bool EnablePublisherConfirms { get; set; } = true;
}