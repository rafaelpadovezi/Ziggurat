using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ziggurat.Idempotency;

namespace Ziggurat.SqlServer;

public class EntityFrameworkStorage<TContext> : IStorage
    where TContext : DbContext
{
    private readonly TContext _context;
    private const int SqlServerViolationConstraintErrorCode = 2627;
    private readonly DbSet<MessageTracking> _messages;

    public EntityFrameworkStorage(TContext context)
    {
        _context = context;
        CheckIfDbSetExists(context);

        _messages = context.Set<MessageTracking>();
    }

    public bool IsMessageExistsError(Exception ex)
    {
        if (ex is not DbUpdateException dbUpdateException)
            return false;

        if (dbUpdateException.InnerException is not SqlException sqlEx)
            return false;

        var entry = dbUpdateException.Entries.FirstOrDefault(
            x => x.Entity.GetType() == typeof(MessageTracking));

        return sqlEx.Number == SqlServerViolationConstraintErrorCode &&
               entry is not null;
    }

    public async Task<bool> HasProcessedAsync(IMessage message)
    {
        var messageExists = await _messages
            .Where(x => x.Id == message.MessageId)
            .Where(x => x.Type == message.MessageGroup)
            .AnyAsync();

        _messages.Add(new MessageTracking(message.MessageId, message.MessageGroup));

        return messageExists;
    }

    private static void CheckIfDbSetExists(TContext context)
    {
        var metaData = context.Model.FindEntityType(typeof(MessageTracking));
        if (metaData == null)
            throw new InvalidOperationException(
                "Cannot create IdempotencyService because a DbSet for 'MessageTracking' is not included in the model for the context.");
    }

    public async Task<int> DeleteMessagesHistoryOlderThanAsync(int days, int batchSize,
        CancellationToken cancellationToken)
    {
        // get the table name from EF metadata
        var tableName = _context.Model.FindEntityType(typeof(MessageTracking))!.GetTableName();
        var tableSchema = _context.Model.FindEntityType(typeof(MessageTracking))!.GetSchema();
        var tableFullName = tableSchema is null ? $"[{tableName}]" : $"[{tableSchema}].[{tableName}]";

        var deleted = await _context.Database.ExecuteSqlRawAsync(
            $"DELETE TOP ({{0}}) FROM {tableFullName} WITH (READPAST) WHERE DateTime < {{1}}",
            new object[] { batchSize, DateTime.Now.AddDays(-days) },
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        return deleted;
    }

    public Task InitializeAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}