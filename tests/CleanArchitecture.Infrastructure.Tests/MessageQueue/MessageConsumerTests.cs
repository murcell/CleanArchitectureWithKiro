using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.MessageQueue;
using CleanArchitecture.Infrastructure.MessageQueue.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.MessageQueue;

public class MessageConsumerTests
{
    private readonly Mock<IMessageQueueService> _mockMessageQueueService;
    private readonly Mock<ILogger<MessageConsumer>> _mockLogger;
    private readonly MessageConsumer _messageConsumer;

    public MessageConsumerTests()
    {
        _mockMessageQueueService = new Mock<IMessageQueueService>();
        _mockLogger = new Mock<ILogger<MessageConsumer>>();
        _messageConsumer = new MessageConsumer(_mockMessageQueueService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StartConsumingAsync_ShouldCallMessageQueueService_WhenValidParametersProvided()
    {
        // Arrange
        var queueName = "test-queue";
        var handler = new Func<UserCreatedMessage, Task<bool>>(msg => Task.FromResult(true));
        var cancellationToken = CancellationToken.None;

        _mockMessageQueueService
            .Setup(x => x.SubscribeAsync(queueName, handler, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageConsumer.StartConsumingAsync(queueName, handler, cancellationToken);

        // Assert
        _mockMessageQueueService.Verify(
            x => x.SubscribeAsync(queueName, handler, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_ShouldLogInformation_WhenStartingConsumer()
    {
        // Arrange
        var queueName = "test-queue";
        var handler = new Func<EmailNotificationMessage, Task<bool>>(msg => Task.FromResult(true));

        _mockMessageQueueService
            .Setup(x => x.SubscribeAsync(queueName, handler, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageConsumer.StartConsumingAsync(queueName, handler);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting consumer for queue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully started consumer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_ShouldLogError_WhenMessageQueueServiceThrows()
    {
        // Arrange
        var queueName = "test-queue";
        var handler = new Func<UserUpdatedMessage, Task<bool>>(msg => Task.FromResult(true));
        var expectedException = new InvalidOperationException("Queue service error");

        _mockMessageQueueService
            .Setup(x => x.SubscribeAsync(queueName, handler, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageConsumer.StartConsumingAsync(queueName, handler));

        Assert.Equal(expectedException.Message, exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start consumer")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopConsumingAsync_ShouldCallMessageQueueService_WhenValidQueueNameProvided()
    {
        // Arrange
        var queueName = "test-queue";
        var handler = new Func<UserDeletedMessage, Task<bool>>(msg => Task.FromResult(true));

        // First start consuming to add to internal tracking
        _mockMessageQueueService
            .Setup(x => x.SubscribeAsync(queueName, handler, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMessageQueueService
            .Setup(x => x.UnsubscribeAsync(queueName))
            .Returns(Task.CompletedTask);

        await _messageConsumer.StartConsumingAsync(queueName, handler);

        // Act
        await _messageConsumer.StopConsumingAsync(queueName);

        // Assert
        _mockMessageQueueService.Verify(
            x => x.UnsubscribeAsync(queueName),
            Times.Once);
    }

    [Fact]
    public async Task StopConsumingAsync_ShouldLogInformation_WhenStoppingConsumer()
    {
        // Arrange
        var queueName = "test-queue";

        _mockMessageQueueService
            .Setup(x => x.UnsubscribeAsync(queueName))
            .Returns(Task.CompletedTask);

        // Act
        await _messageConsumer.StopConsumingAsync(queueName);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping consumer for queue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully stopped consumer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopConsumingAsync_ShouldLogError_WhenMessageQueueServiceThrows()
    {
        // Arrange
        var queueName = "test-queue";
        var expectedException = new InvalidOperationException("Queue service error");

        _mockMessageQueueService
            .Setup(x => x.UnsubscribeAsync(queueName))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageConsumer.StopConsumingAsync(queueName));

        Assert.Equal(expectedException.Message, exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to stop consumer")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldLogInformation()
    {
        // Act
        await _messageConsumer.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageConsumer hosted service starting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogInformation()
    {
        // Act
        await _messageConsumer.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageConsumer hosted service stopping")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageConsumer hosted service stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}