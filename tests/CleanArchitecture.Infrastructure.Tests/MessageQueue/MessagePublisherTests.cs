using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.MessageQueue;
using CleanArchitecture.Infrastructure.MessageQueue.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.MessageQueue;

public class MessagePublisherTests
{
    private readonly Mock<IMessageQueueService> _mockMessageQueueService;
    private readonly Mock<ILogger<MessagePublisher>> _mockLogger;
    private readonly MessagePublisher _messagePublisher;

    public MessagePublisherTests()
    {
        _mockMessageQueueService = new Mock<IMessageQueueService>();
        _mockLogger = new Mock<ILogger<MessagePublisher>>();
        _messagePublisher = new MessagePublisher(_mockMessageQueueService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_ShouldCallMessageQueueService_WhenValidMessageProvided()
    {
        // Arrange
        var message = new UserCreatedMessage
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com"
        };
        var queueName = "test-queue";
        var cancellationToken = CancellationToken.None;

        _mockMessageQueueService
            .Setup(x => x.PublishAsync(message, queueName, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _messagePublisher.PublishAsync(message, queueName, cancellationToken);

        // Assert
        _mockMessageQueueService.Verify(
            x => x.PublishAsync(message, queueName, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithDelay_ShouldCallMessageQueueService_WhenValidMessageProvided()
    {
        // Arrange
        var message = new EmailNotificationMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };
        var queueName = "test-queue";
        var delay = TimeSpan.FromMinutes(5);
        var cancellationToken = CancellationToken.None;

        _mockMessageQueueService
            .Setup(x => x.PublishAsync(message, queueName, delay, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _messagePublisher.PublishAsync(message, queueName, delay, cancellationToken);

        // Assert
        _mockMessageQueueService.Verify(
            x => x.PublishAsync(message, queueName, delay, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogDebugMessages_WhenPublishingMessage()
    {
        // Arrange
        var message = new UserUpdatedMessage
        {
            UserId = 2,
            Name = "Updated User",
            Email = "updated@example.com"
        };
        var queueName = "test-queue";

        _mockMessageQueueService
            .Setup(x => x.PublishAsync(message, queueName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messagePublisher.PublishAsync(message, queueName);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully published message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogError_WhenMessageQueueServiceThrows()
    {
        // Arrange
        var message = new UserDeletedMessage
        {
            UserId = 3,
            Email = "deleted@example.com"
        };
        var queueName = "test-queue";
        var expectedException = new InvalidOperationException("Queue service error");

        _mockMessageQueueService
            .Setup(x => x.PublishAsync(message, queueName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messagePublisher.PublishAsync(message, queueName));

        Assert.Equal(expectedException.Message, exception.Message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish message")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithDelay_ShouldLogDebugMessages_WhenPublishingDelayedMessage()
    {
        // Arrange
        var message = new EmailNotificationMessage
        {
            To = "test@example.com",
            Subject = "Delayed Test",
            Body = "This is a delayed message"
        };
        var queueName = "test-queue";
        var delay = TimeSpan.FromHours(1);

        _mockMessageQueueService
            .Setup(x => x.PublishAsync(message, queueName, delay, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messagePublisher.PublishAsync(message, queueName, delay);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing delayed message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully published delayed message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}