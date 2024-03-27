﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ziggurat.Idempotency;

namespace Ziggurat.SqlServer;

public class EntityFrameworkStorage<TContext> : IStorage
    where TContext : DbContext
{
    private const int SqlServerViolationConstraintErrorCode = 2627;
    private readonly DbSet<MessageTracking> _messages;
    private readonly TContext _context;

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

    public async Task<int> DeleteMessagesHistoryOlderThanAsync(int days, int maxMessagesToDelete = 0)
    {
        var messagesToDeleteInDb = _messages
           .Where(x => x.DateTime <= DateTime.Now.AddDays(-days));
        if (maxMessagesToDelete > 0)
            messagesToDeleteInDb.Take(maxMessagesToDelete);

        var messagesToDelete = await messagesToDeleteInDb.ToListAsync();
        _messages.RemoveRange(messagesToDelete);

        var count = messagesToDelete != null ? messagesToDelete.Count : 0;

        await _context.SaveChangesAsync();

        return count;
    }
}