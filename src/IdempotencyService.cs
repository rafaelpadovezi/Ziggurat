using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    public class IdempotencyService<TMessage, TContext> : IConsumerService<TMessage>
        where TContext : DbContext
        where TMessage : IMessage
    {
        private readonly DbSet<MessageTracking> _messages;
        private readonly IConsumerService<TMessage> _service;
        private readonly ILogger<IdempotencyService<TMessage, TContext>> _logger;

        public IdempotencyService(
            TContext context,
            IConsumerService<TMessage> service,
            ILogger<IdempotencyService<TMessage, TContext>> logger)
        {
            _messages = context.Set<MessageTracking>();
            _service = service;
            _logger = logger;
        }
        
        public async Task ProcessMessageAsync(TMessage message)
        {
            if (await TrackMessageAsync(message))
            {
                _logger.LogInformation("Message was processed already. Ignoring {MessageId}.", message.MessageId);
                return;
            }

            try
            {
                await _service.ProcessMessageAsync(message);                
            }
            catch (DbUpdateException ex) when (IsMessageExistsError(ex))
            {
                // If is unique constraint error it means that the message
                // was already processed and should do nothing
                _logger.LogInformation("Message was processed already. Ignoring {MessageId}.", message.MessageId);
            }
        }

        private static bool IsMessageExistsError(DbUpdateException ex)
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

        private async Task<bool> TrackMessageAsync(TMessage message)
        {
            // The performance of this must be taken in account.
            // Although the query is fast executing for each message
            // could affect the DB CPU consume.
            if (await _messages.AnyAsync(x => x.Id == message.MessageId))
                return true;

            _messages.Add(new MessageTracking { Id = message.MessageId, Type = message.MessageGroup});
            return false;
        }
    }
}