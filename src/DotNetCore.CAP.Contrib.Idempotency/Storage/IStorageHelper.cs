using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.Contrib.Idempotency.Storage
{
    public interface IStorageHelper
    {
        bool IsMessageExistsError(DbUpdateException ex);
    }
}