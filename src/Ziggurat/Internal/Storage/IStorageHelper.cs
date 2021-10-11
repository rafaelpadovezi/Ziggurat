using Microsoft.EntityFrameworkCore;

namespace Ziggurat.Internal.Storage
{
    public interface IStorageHelper
    {
        bool IsMessageExistsError(DbUpdateException ex);
    }
}