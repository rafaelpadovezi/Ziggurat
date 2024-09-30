using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB.Tests;

public class CleanerOptionsExtensionsTests
{
    [Fact]
    public void UseMongoDbStorage_ShouldRegister_MongoDbStorage()
    {
        // Arrange
        var options = new CleanerOptions();

        // Act
        options.UseMongoDbStorage();

        // Assert
        var services = new ServiceCollection();
        options.RegisterStorage(services);
        var storage = services.FirstOrDefault(x => x.ServiceType == typeof(IStorage));
        storage.ImplementationType.Should().Be(typeof(MongoDbStorage));
    }
}