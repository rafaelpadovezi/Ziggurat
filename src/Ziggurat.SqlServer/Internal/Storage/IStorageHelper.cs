using Microsoft.EntityFrameworkCore;

namespace Ziggurat.SqlServer.Internal.Storage
{
    public interface IStorageHelper
    {
        bool IsMessageExistsError(DbUpdateException ex);
    }
}