using System;
using System.Threading.Tasks;

namespace Ziggurat.Idempotency;

public interface IStorage
{
    bool IsMessageExistsError(Exception ex);

    Task<bool> HasProcessedAsync(IMessage message);

    Task<int> DeleteMessagesHistoryOlderThanAsync(int days, int maxMessagesToDelete = 0);
}