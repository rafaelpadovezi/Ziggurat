using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Ziggurat.Idempotency;

namespace Ziggurat.Tests;

public class CleanerTests
{
    [Fact]
    public async Task StartAsync_ShouldCallStorageDeleteAndLog()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        mockStorage
            .Setup(x => x.DeleteMessagesHistoryOlderThanAsync(optionsExpireAfterInDays, batchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .Verifiable();
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(cancellationTokenSource.Token);

        // Assert
        mockStorage.Verify();
        mockLogger.VerifyLog(x => x.LogInformation($"Ziggurat cleaner background service removed 10 messages."));
        await cancellationTokenSource.CancelAsync();
    }

    [Fact]
    public async Task StartAsync_WhenCancellationTokenIsCalled_ShouldStopCleaner()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        // We need to call DeleteMessagesHistoryOlderThanAsync twice to stop the cleaner
        var deletedCount = 0;
        mockStorage
            .Setup(x => x.DeleteMessagesHistoryOlderThanAsync(optionsExpireAfterInDays, batchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .Callback(() =>
            {
                deletedCount++;
                if (deletedCount == 2)
                {
                    cancellationTokenSource.Cancel();
                }
            });
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(cancellationTokenSource.Token);

        // Assert
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            await Task.Delay(100);
        }
        mockStorage.Verify();
        mockLogger.VerifyLog(x => x.LogInformation("Ziggurat cleaner background service is stopping."));
    }

    [Fact]
    public void StartAsync_WhenStorageInitializeThrowError_ShouldStopBackgroundService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = 1;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        services.AddSingleton(_ => mockLogger.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IHostedService>());

        // Assert
        Assert.Equal("Cannot create Ziggurat cleaner background service because IStorage is not registered.", exception.Message);
    }

    [Fact]
    public async Task Ctor_WhenStorageIsNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Throws<InvalidOperationException>();

        // Act
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(cancellationTokenSource.Token);

        // Assert
        mockLogger.VerifyLog(x => x.LogError("Could not initialize Ziggurat Cleaner properly. Cleaner is shutting up."));
    }
}