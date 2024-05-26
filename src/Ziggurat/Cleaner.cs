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
    public TimeSpan CleaningInterval { get; set; }
    /// <summary>
    /// The expiration time (in days) of Ziggurat message tracking
    /// </summary>
    public int ExpireAfterInDays { get; set; }
}

public static class ZigguratExtensions
{
    public static IServiceCollection AddZigguratCleaner(this IServiceCollection services)
    {
        return services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromHours(1);
            options.ExpireAfterInDays = 7;
        });
    }

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

    public Cleaner(
        IServiceProvider serviceProvider,
        IOptions<CleanerOptions> options,
        ILogger<Cleaner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _expireAfterInDays = options.Value.ExpireAfterInDays;
        _cleaningInterval = options.Value.CleaningInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ziggurat cleaner background service is starting.");
        var periodicTimer = new PeriodicTimer(_cleaningInterval);

        try
        {
            await DeleteOldMessages();
            while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
            {
                await DeleteOldMessages();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            periodicTimer.Dispose();
            _logger.LogInformation("Ziggurat cleaner background service is stopping.");
        }
    }

    private async Task DeleteOldMessages()
    {
        using var scope = _serviceProvider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        var removed = await storage.DeleteMessagesHistoryOlderThanAsync(_expireAfterInDays);
        _logger.LogInformation($"Ziggurat cleaner background service removed {removed} messages.");
    }
}