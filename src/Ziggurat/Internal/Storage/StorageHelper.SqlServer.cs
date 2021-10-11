using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ziggurat.Idempotency;

namespace Ziggurat.Internal.Storage
{
    internal class StorageHelperSqlServer : IStorageHelper
    {
        public bool IsMessageExistsError(DbUpdateException ex)
        {
            if (ex.InnerException is not SqlException sqlEx)
                return false;

            var entry = ex.Entries.FirstOrDefault(
                x => x.Entity.GetType() == typeof(MessageTracking));
            // SqlServer: Error 2627
            // Violation of PRIMARY KEY constraint Constraint Name.
            // Cannot insert duplicate key in object Table Name.
            return sqlEx.Number == 2627 && entry is not null;
        }
    }
}