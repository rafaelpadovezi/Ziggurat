using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Ziggurat.Idempotency;

namespace Ziggurat.Tests;

public class IdempotentServiceTests
{
    [Fact]
    public async Task ProcessMessageAsync_NewMessage_CallServiceProcessMessage()
    {
        // Arrange
        var mockService = new Mock<IConsumerService<TestMessage>>();
        var mockStorage = new Mock<IStorage>();
        var service = CreateIdempotencyMiddleware(mockStorage);
        var message = new TestMessage("message1", "group1");

        mockStorage
            .Setup(x => x.HasProcessedAsync(message))
            .ReturnsAsync(false);

        // Act
        await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

        // Assert
        mockService.Verify(x => x.ProcessMessageAsync(message), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_RepeatedMessage_DontCallServiceProcessMessage()
    {
        // Arrange
        var mockService = new Mock<IConsumerService<TestMessage>>();
        var mockStorage = new Mock<IStorage>();
        var mockLogger = new Mock<ILogger<IdempotencyMiddleware<TestMessage>>>();
        var service = CreateIdempotencyMiddleware(mockStorage, mockLogger);
        var message = new TestMessage("message1", "group1");

        mockStorage
            .Setup(x => x.HasProcessedAsync(message))
            .ReturnsAsync(true);

        // Act
        await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

        // Assert
        mockService.Verify(x => x.ProcessMessageAsync(message), Times.Never);
        mockLogger.VerifyLog(logger =>
            logger.LogInformation("Message was processed already. Ignoring message1:group1."));
    }

    [Fact]
    public async Task ProcessMessageAsync_StorageDuplicatedError_LogMessageWasProcessed()
    {
        // Arrange
        var mockService = new Mock<IConsumerService<TestMessage>>();
        var mockStorage = new Mock<IStorage>();
        var mockLogger = new Mock<ILogger<IdempotencyMiddleware<TestMessage>>>();
        var service = CreateIdempotencyMiddleware(mockStorage, mockLogger);
        var message = new TestMessage("message1", "group1");

        mockStorage
            .Setup(x => x.HasProcessedAsync(message))
            .ReturnsAsync(false);
        mockStorage
            .Setup(x => x.IsMessageExistsError(It.IsAny<InvalidOperationException>()))
            .Returns(true);
        mockService
            .Setup(x => x.ProcessMessageAsync(message))
            .Throws<InvalidOperationException>();

        // Act
        await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

        // Assert
        mockLogger.VerifyLog(logger =>
            logger.LogInformation("Message was processed already. Ignoring message1:group1."));
    }

    [Fact]
    public async Task ProcessMessageAsync_OtherException_PropagateException()
    {
        // Arrange
        var mockService = new Mock<IConsumerService<TestMessage>>();
        var mockStorage = new Mock<IStorage>();
        var mockLogger = new Mock<ILogger<IdempotencyMiddleware<TestMessage>>>();
        var service = CreateIdempotencyMiddleware(mockStorage, mockLogger);
        var message = new TestMessage("message1", "group1");

        mockStorage
            .Setup(x => x.HasProcessedAsync(message))
            .ReturnsAsync(false);
        mockStorage
            .Setup(x => x.IsMessageExistsError(It.IsAny<InvalidOperationException>()))
            .Returns(false);
        mockService
            .Setup(x => x.ProcessMessageAsync(message))
            .Throws<InvalidOperationException>();

        // Act
        var action = async () =>
            await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        mockLogger.VerifyLog(logger =>
                logger.LogInformation("Message was processed already. Ignoring message1:group1."),
            Times.Never);
    }

    private static IdempotencyMiddleware<TestMessage> CreateIdempotencyMiddleware(
        Mock<IStorage> mockStorage,
        Mock<ILogger<IdempotencyMiddleware<TestMessage>>> mockLogger = null)
    {
        mockLogger ??= new();

        var service = new IdempotencyMiddleware<TestMessage>(
            mockStorage.Object,
            mockLogger.Object);
        return service;
    }

    public record TestMessage(string MessageId, string MessageGroup) : IMessage
    {
        public string MessageId { get; set; } = MessageId;
        public string MessageGroup { get; set; } = MessageGroup;
    }
}