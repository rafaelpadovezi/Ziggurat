using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.Contrib.Idempotency.Storage
{
    internal interface IStorageHelper
    {
        bool IsMessageExistsError(DbUpdateException ex);
    }
}