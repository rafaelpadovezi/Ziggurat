using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Ziggurat.Idempotency;
using Ziggurat.Internal.Storage;
using Ziggurat.Tests.Support;

namespace Ziggurat.Tests.Idempotency
{
    public class IdempotentServiceTests : TestFixture
    {
        [Fact]
        public async Task ProcessMessageAsync_NewMessage_CallServiceProcessMessage()
        {
            // Arrange
            var mockService = new Mock<IConsumerService<TestMessage>>();
            var service = CreateIdempotencyMiddleware();
            var message = new TestMessage("message1", "group1");

            // Act
            await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

            // Assert
            mockService.Verify(x => x.ProcessMessageAsync(message), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_SameMessageIdDifferentGroup_CallServiceProcessMessage()
        {
            // Arrange
            var mockService = new Mock<IConsumerService<TestMessage>>();
            Context.Messages.Add(new MessageTracking("message1", "group1"));
            await Context.SaveChangesAsync();

            var service = CreateIdempotencyMiddleware();
            var message = new TestMessage("message1", "group2");

            // Act
            await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

            // Assert
            mockService.Verify(x => x.ProcessMessageAsync(message), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_NewMessage_SaveMessage()
        {
            // Arrange
            var mockService = new Mock<IConsumerService<TestMessage>>();
            mockService
                .Setup(x => x.ProcessMessageAsync(It.IsAny<TestMessage>()))
                .Callback(() => Context.SaveChanges());

            var service = CreateIdempotencyMiddleware();
            var message = new TestMessage("message1", "group1");

            // Act
            await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

            // Assert
            var storedMessage = await Context.Messages.AsNoTracking().SingleOrDefaultAsync();
            storedMessage.Id.Should().Be(message.MessageId);
            storedMessage.Type.Should().Be(message.MessageGroup);
        }

        [Fact]
        public async Task ProcessMessageAsync_RepeatedMessage_DontCallServiceProcessMessage()
        {
            // Arrange
            var mockService = new Mock<IConsumerService<TestMessage>>();
            var mockLogger = new Mock<ILogger<IdempotencyMiddleware<TestMessage, TestDbContext>>>();
            Context.Messages.Add(new MessageTracking("message1", "group1"));
            await Context.SaveChangesAsync();

            var service = CreateIdempotencyMiddleware(mockLogger);
            var message = new TestMessage("message1", "group1");

            // Act
            await service.OnExecutingAsync(message, testMessage => mockService.Object.ProcessMessageAsync(testMessage));

            // Assert
            mockService.Verify(x => x.ProcessMessageAsync(message), Times.Never);
            mockLogger.VerifyLog(logger =>
                logger.LogInformation("Message was processed already. Ignoring message1:group1."));
        }

        private IdempotencyMiddleware<TestMessage, TestDbContext> CreateIdempotencyMiddleware(
            Mock<ILogger<IdempotencyMiddleware<TestMessage, TestDbContext>>> mockLogger = null)
        {
            mockLogger ??= new Mock<ILogger<IdempotencyMiddleware<TestMessage, TestDbContext>>>();
            var mockStorageHelper = new Mock<IStorageHelper>();

            var service = new IdempotencyMiddleware<TestMessage, TestDbContext>(
                Context,
                mockStorageHelper.Object,
                mockLogger.Object);
            return service;
        }

        public record TestMessage : IMessage
        {
            public TestMessage(string messageId, string messageGroup)
            {
                MessageId = messageId;
                MessageGroup = messageGroup;
            }

            public string MessageId { get; set; }
            public string MessageGroup { get; set; }
        }
    }
}