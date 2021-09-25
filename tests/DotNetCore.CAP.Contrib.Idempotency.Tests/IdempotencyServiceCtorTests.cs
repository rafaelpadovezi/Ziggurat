using DotNetCore.CAP.Contrib.Idempotency.Storage;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace DotNetCore.CAP.Contrib.Idempotency.Tests
{
    public class IdempotencyServiceCtorTests
    {
        [Fact]
        public void IdempotencyService_ContextWithoutMessageDbSet_ThrowException()
        {
            // Arrange
            var mockService = new Mock<IConsumerService<TestMessage>>();
            var mockLogger = new Mock<ILogger<IdempotencyService<TestMessage, TestContextWithoutMessages>>>();
            var mockStorageHelper = new Mock<IStorageHelper>();
            var context = new TestContextWithoutMessages();

            // Act
            Action act = () => new IdempotencyService<TestMessage, TestContextWithoutMessages>(
                context,
                mockService.Object,
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