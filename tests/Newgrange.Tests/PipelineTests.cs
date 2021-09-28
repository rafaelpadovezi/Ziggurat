using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Newgrange.Tests
{
    public class PipelineTests
    {
        private static readonly List<string> Order = new(3);
        [Fact]
        public async Task RunPipeline_MultipleMiddlewares_RunInOrder()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<TestConsumerService>();
            services.AddScoped<IConsumerMiddleware<TestMessage>, TestMiddleware1>();
            services.AddScoped<IConsumerMiddleware<TestMessage>, TestMiddleware2>();
            services.AddScoped<IConsumerService<TestMessage>>(t => new PipelineHandler<TestMessage>(
                t,
                t.GetRequiredService<TestConsumerService>()
            ));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var pipeline = serviceProvider.GetRequiredService<IConsumerService<TestMessage>>();
            await pipeline.ProcessMessageAsync(new TestMessage());

            // Assert - Verify call order
            Order.Should().Equal(new List<string>
            {
                "TestMiddleware1",
                "TestMiddleware2",
                "TestConsumerService"
            });
        }

        public class TestMessage : IMessage
        {
            public string MessageId { get; set; }
            public string MessageGroup { get; set; }
        }

        public class TestConsumerService : IConsumerService<TestMessage>
        {
            public Task ProcessMessageAsync(TestMessage message)
            {
                Order.Add("TestConsumerService");
                return Task.CompletedTask;
            }
        }

        public class TestMiddleware1 : IConsumerMiddleware<TestMessage>
        {
            public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next)
            {
                Order.Add("TestMiddleware1");
                await next(message);
            }
        }

        public class TestMiddleware2 : IConsumerMiddleware<TestMessage>
        {
            public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next)
            {
                Order.Add("TestMiddleware2");
                await next(message);
            }
        }
    }
}