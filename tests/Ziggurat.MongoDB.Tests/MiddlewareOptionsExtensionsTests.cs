using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;
using Ziggurat.Idempotency;
using Ziggurat.MongoDB.Tests.Support;

namespace Ziggurat.MongoDB.Tests;

public class MiddlewareOptionsExtensionsTests
{
    [Fact]
    public void UseMongoDbIdempotency_ShouldRegister_MongoDbStorage()
    {
        // Arrange
        var options = new MiddlewareOptions<TestMessage>();
        const string mongoDatabaseName = "test";

        // Act
        options.UseMongoDbIdempotency(mongoDatabaseName);

        // Assert
        var services = new ServiceCollection();
        foreach (var extention in options.Extensions)
        {
            extention(services);
        }

        var storage = services
            .FirstOrDefault(x => x.ServiceType == typeof(IStorage) &&
                            x.ImplementationType == typeof(MongoDbStorage));
        storage.Should().NotBeNull();
        var idempotency = services
            .FirstOrDefault(x => x.ServiceType == typeof(IConsumerMiddleware<TestMessage>) &&
                            x.ImplementationType == typeof(IdempotencyMiddleware<TestMessage>));
        idempotency.Should().NotBeNull();
    }
}