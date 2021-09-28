using Microsoft.EntityFrameworkCore;

namespace Newgrange.Internal.Storage
{
    public interface IStorageHelper
    {
        bool IsMessageExistsError(DbUpdateException ex);
    }
}