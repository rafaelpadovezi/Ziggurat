using System;
using System.Threading.Tasks;

namespace Ziggurat.Idempotency;

public interface IStorage
{
    bool IsMessageExistsError(Exception ex);

    Task<bool> HasProcessedAsync(IMessage message);
}