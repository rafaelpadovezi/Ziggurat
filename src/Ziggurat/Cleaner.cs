using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ziggurat.Idempotency;

namespace Ziggurat;

public class CleanerOptions
{
    /// <summary>
    /// The interval of the cleaner deletes expired messages
    /// </summary>
    public TimeSpan CleaningInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The expiration time (in days) of Ziggurat message tracking
    /// </summary>
    public int ExpireAfterInDays { get; set; } = 7;

    /// <summary>
    /// The batch size of delete command
    /// </summary>
    public int BatchSize { get; set; } = 100_000;
}

public static class ZigguratExtensions
{
    /// <summary>
    /// Adds the Ziggurat cleaner background service to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns></returns>
    public static IServiceCollection AddZigguratCleaner(this IServiceCollection services)
    {
        return services.AddZigguratCleaner(_ => { });
    }

    /// <summary>
    /// Adds the Ziggurat cleaner background service to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="optionsAction">An action to configure the <see cref="T:Ziggurat.CleanerOptions" /></param>
    /// <returns></returns>
    public static IServiceCollection AddZigguratCleaner(this IServiceCollection services, Action<CleanerOptions> optionsAction)
    {
        services.Configure(optionsAction);
        return services.AddHostedService<Cleaner>();
    }
}

internal sealed class Cleaner : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Cleaner> _logger;
    private readonly TimeSpan _cleaningInterval;
    private readonly int _expireAfterInDays;
    private readonly int _batchSize;

    public Cleaner(
        IServiceProvider serviceProvider,
        IOptions<CleanerOptions> options,
        ILogger<Cleaner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _expireAfterInDays = options.Value.ExpireAfterInDays;
        _cleaningInterval = options.Value.CleaningInterval;
        _batchSize = options.Value.BatchSize;
        CheckDependencies();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ziggurat cleaner background service is starting.");
        var periodicTimer = new PeriodicTimer(_cleaningInterval);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
            await storage.InitializeAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not initialize Ziggurat Cleaner properly. Cleaner is shutting up.");
            return;
        }

        try
        {
            do
            {
                try
                {
                    await DeleteOldMessages(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Ziggurat cleaner found an error occurred while deleting old messages.");
                }
            } while (await periodicTimer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            //expected
        }
        finally
        {
            periodicTimer.Dispose();
            _logger.LogInformation("Ziggurat cleaner background service is stopping.");
        }
    }

    private async Task DeleteOldMessages(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        var removed = await storage.DeleteMessagesHistoryOlderThanAsync(_expireAfterInDays, _batchSize, stoppingToken);

#pragma warning disable CA2254
        _logger.LogInformation($"Ziggurat cleaner background service removed {removed} messages.");
#pragma warning restore CA2254
    }

    private void CheckDependencies()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetService<IStorage>();
        if (service is null)
        {
            throw new InvalidOperationException("Cannot create Ziggurat cleaner background service because IStorage is not registered.");
        }
    }
}