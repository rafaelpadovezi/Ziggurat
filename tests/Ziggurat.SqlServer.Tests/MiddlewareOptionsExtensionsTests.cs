using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;
using Ziggurat.Idempotency;
using Ziggurat.SqlServer.Tests.Support;

namespace Ziggurat.SqlServer.Tests;

public class MiddlewareOptionsExtensionsTests
{
    [Fact]
    public void UseEntityFrameworkIdempotency_ShouldRegister_Storage()
    {
        // Arrange
        var options = new MiddlewareOptions<TestMessage>();

        // Act
        options.UseEntityFrameworkIdempotency<TestMessage, TestDbContext>();

        // Assert
        var services = new ServiceCollection();
        foreach (var extention in options.Extensions)
        {
            extention(services);
        }

        var storage = services
            .FirstOrDefault(x => x.ServiceType == typeof(IStorage) &&
                                 x.ImplementationType == typeof(EntityFrameworkStorage<TestDbContext>));
        Assert.NotNull(storage);
        var idempotency = services
            .FirstOrDefault(x => x.ServiceType == typeof(IConsumerMiddleware<TestMessage>) &&
                                 x.ImplementationType == typeof(IdempotencyMiddleware<TestMessage>));
        Assert.NotNull(idempotency);
    }
}