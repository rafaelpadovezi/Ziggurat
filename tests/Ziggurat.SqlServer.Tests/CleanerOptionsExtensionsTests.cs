using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;
using Ziggurat.Idempotency;
using Ziggurat.SqlServer.Tests.Support;

namespace Ziggurat.SqlServer.Tests;

public class CleanerOptionsExtensionsTests
{
    [Fact]
    public void UseEntityFrameworkStorage_ShouldRegister_EntityFrameworkStorage()
    {
        // Arrange
        var options = new CleanerOptions();

        // Act
        options.UseEntityFrameworkStorage<TestDbContext>();

        // Assert
        var services = new ServiceCollection();
        options.RegisterStorage(services);
        var storage = services.FirstOrDefault(x => x.ServiceType == typeof(IStorage));
        Assert.NotNull(storage);
        Assert.Equal(typeof(EntityFrameworkStorage<TestDbContext>), storage.ImplementationType);
    }
}