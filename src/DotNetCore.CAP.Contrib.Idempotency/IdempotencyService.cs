using DotNetCore.CAP.Contrib.Idempotency.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IStorageHelper _storageHelper;
        private readonly ILogger<IdempotencyService<TMessage, TContext>> _logger;

        internal IdempotencyService(
            TContext context,
            IConsumerService<TMessage> service,
            IStorageHelper storageHelper,
            ILogger<IdempotencyService<TMessage, TContext>> logger)
        {
            CheckIfDbSetExists(context);

            _messages = context.Set<MessageTracking>();
            _service = service;
            _storageHelper = storageHelper;
            _logger = logger;
        }

        public async Task ProcessMessageAsync(TMessage message)
        {
            if (await TrackMessageAsync(message))
            {
                _logger.LogMessageExists(message);
                return;
            }

            try
            {
                await _service.ProcessMessageAsync(message);
            }
            catch (DbUpdateException ex) when (_storageHelper.IsMessageExistsError(ex))
            {
                // If is unique constraint error it means that the message
                // was already processed and should do nothing
                _logger.LogMessageExists(message);
            }
        }

        private async Task<bool> TrackMessageAsync(TMessage message)
        {
            var messageExists = await _messages
                .Where(x => x.Id == message.MessageId)
                .Where(x => x.Type == message.MessageGroup)
                .AnyAsync();
            if (messageExists)
                return true;

            _messages.Add(new MessageTracking(message.MessageId, message.MessageGroup));
            return false;
        }

        private static void CheckIfDbSetExists(TContext context)
        {
            var metaData = context.Model.FindEntityType(typeof(MessageTracking));
            if (metaData == null)
                throw new InvalidOperationException(
                    "Cannot create IdempotencyService because a DbSet for 'MessageTracking' is not included in the model for the context.");
        }
    }
}