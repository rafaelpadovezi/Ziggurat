namespace Ziggurat.Idempotency;

public interface IStorage
{
    bool IsMessageExistsError(Exception ex);

    Task<bool> HasProcessedAsync(IMessage message);

    Task<int> DeleteMessagesHistoryOlderThanAsync(int days, int batchSize, CancellationToken cancellationToken);

    Task InitializeAsync(CancellationToken stoppingToken);
}