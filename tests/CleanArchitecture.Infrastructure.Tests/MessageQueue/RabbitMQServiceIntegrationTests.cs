using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Configuration;
using CleanArchitecture.Infrastructure.MessageQueue;
using CleanArchitecture.Infrastructure.MessageQueue.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchitecture.Infrastructure.Tests.MessageQueue;

public class RabbitMQServiceIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly RabbitMQService _rabbitMQService;
    private readonly MessageQueueOptions _options;
    private readonly ILogger<RabbitMQService> _logger;

    public RabbitMQServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        _options = new MessageQueueOptions
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
            Port = 5672,
            VirtualHost = "/",
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            EnableDeadLetterQueue = true
        };

        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _logger = loggerFactory.CreateLogger<RabbitMQService>();

        try
        {
            _rabbitMQService = new RabbitMQService(Options.Create(_options), _logger);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to create RabbitMQ service: {ex.Message}");
            _output.WriteLine("Make sure RabbitMQ is running on localhost:5672");
            throw new SkipException("RabbitMQ is not available for integration tests");
        }
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishMessage_WhenValidMessageProvided()
    {
        // Arrange
        var queueName = "test-queue-publish";
        var message = new UserCreatedMessage
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com"
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _rabbitMQService.PublishAsync(message, queueName));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithDelay_ShouldPublishDelayedMessage()
    {
        // Arrange
        var queueName = "test-queue-delayed";
        var message = new EmailNotificationMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };
        var delay = TimeSpan.FromSeconds(2);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _rabbitMQService.PublishAsync(message, queueName, delay));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldReceiveMessage_WhenMessagePublished()
    {
        // Arrange
        var queueName = "test-queue-subscribe";
        var message = new UserUpdatedMessage
        {
            UserId = 2,
            Name = "Updated User",
            Email = "updated@example.com"
        };

        var receivedMessage = default(UserUpdatedMessage);
        var messageReceived = new TaskCompletionSource<bool>();

        // Act
        await _rabbitMQService.SubscribeAsync<UserUpdatedMessage>(queueName, async (msg) =>
        {
            receivedMessage = msg;
            messageReceived.SetResult(true);
            return true; // Message processed successfully
        });

        await _rabbitMQService.PublishAsync(message, queueName);

        // Wait for message to be received (with timeout)
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(received);
        Assert.NotNull(receivedMessage);
        Assert.Equal(message.UserId, receivedMessage.UserId);
        Assert.Equal(message.Name, receivedMessage.Name);
        Assert.Equal(message.Email, receivedMessage.Email);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldRetryOnFailure_WhenHandlerReturnsFalse()
    {
        // Arrange
        var queueName = "test-queue-retry";
        var message = new UserDeletedMessage
        {
            UserId = 3,
            Email = "deleted@example.com"
        };

        var attemptCount = 0;
        var maxAttempts = _options.MaxRetryAttempts;
        var allAttemptsCompleted = new TaskCompletionSource<bool>();

        // Act
        await _rabbitMQService.SubscribeAsync<UserDeletedMessage>(queueName, async (msg) =>
        {
            attemptCount++;
            _output.WriteLine($"Processing attempt {attemptCount} for message {msg.UserId}");

            if (attemptCount <= maxAttempts)
            {
                if (attemptCount == maxAttempts + 1) // After all retries
                {
                    allAttemptsCompleted.SetResult(true);
                }
                return false; // Simulate failure
            }

            return true;
        });

        await _rabbitMQService.PublishAsync(message, queueName);

        // Wait for all retry attempts (with timeout)
        var completed = await allAttemptsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(completed);
        Assert.True(attemptCount >= maxAttempts);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldHandleException_WhenHandlerThrows()
    {
        // Arrange
        var queueName = "test-queue-exception";
        var message = new EmailNotificationMessage
        {
            To = "error@example.com",
            Subject = "Error Test",
            Body = "This will cause an error"
        };

        var exceptionHandled = new TaskCompletionSource<bool>();

        // Act
        await _rabbitMQService.SubscribeAsync<EmailNotificationMessage>(queueName, async (msg) =>
        {
            if (msg.Subject == "Error Test")
            {
                exceptionHandled.SetResult(true);
                throw new InvalidOperationException("Simulated error");
            }
            return true;
        });

        await _rabbitMQService.PublishAsync(message, queueName);

        // Wait for exception to be handled (with timeout)
        var handled = await exceptionHandled.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(handled);
    }

    [Fact]
    public async Task UnsubscribeAsync_ShouldStopReceivingMessages()
    {
        // Arrange
        var queueName = "test-queue-unsubscribe";
        var message1 = new UserCreatedMessage { UserId = 4, Name = "User 4", Email = "user4@example.com" };
        var message2 = new UserCreatedMessage { UserId = 5, Name = "User 5", Email = "user5@example.com" };

        var messagesReceived = new List<UserCreatedMessage>();
        var firstMessageReceived = new TaskCompletionSource<bool>();

        // Act
        await _rabbitMQService.SubscribeAsync<UserCreatedMessage>(queueName, async (msg) =>
        {
            messagesReceived.Add(msg);
            if (messagesReceived.Count == 1)
            {
                firstMessageReceived.SetResult(true);
            }
            return true;
        });

        // Publish first message
        await _rabbitMQService.PublishAsync(message1, queueName);
        await firstMessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Unsubscribe
        await _rabbitMQService.UnsubscribeAsync(queueName);

        // Publish second message
        await _rabbitMQService.PublishAsync(message2, queueName);

        // Wait a bit to ensure second message would be received if subscription was active
        await Task.Delay(2000);

        // Assert
        Assert.Single(messagesReceived);
        Assert.Equal(message1.UserId, messagesReceived[0].UserId);
    }

    public void Dispose()
    {
        _rabbitMQService?.Dispose();
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}