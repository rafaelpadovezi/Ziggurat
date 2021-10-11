using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Ziggurat.Idempotency;
using Ziggurat.Internal.Storage;

namespace Ziggurat.Tests.Idempotency
{
    public class IdempotencyServiceCtorTests
    {
        [Fact]
        public void IdempotencyService_ContextWithoutMessageDbSet_ThrowException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<IdempotencyMiddleware<TestMessage, TestContextWithoutMessages>>>();
            var mockStorageHelper = new Mock<IStorageHelper>();
            var context = new TestContextWithoutMessages();

            // Act
            Action act = () => new IdempotencyMiddleware<TestMessage, TestContextWithoutMessages>(
                context,
                mockStorageHelper.Object,
                mockLogger.Object);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage(
                "Cannot create IdempotencyService because a DbSet for 'MessageTracking' is not included in the model for the context.");
        }

        public class TestMessage : IMessage
        {
            public string MessageId { get; set; }
            public string MessageGroup { get; set; }
        }
    }

    public class TestContextWithoutMessages : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Test");
        }
    }
}