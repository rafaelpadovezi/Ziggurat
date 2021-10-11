using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ziggurat.Internal;
using Ziggurat.Internal.Storage;

namespace Ziggurat.Idempotency
{
    public class IdempotencyMiddleware<TMessage, TContext> : IConsumerMiddleware<TMessage>
        where TContext : DbContext
        where TMessage : IMessage
    {
        private readonly ILogger<IdempotencyMiddleware<TMessage, TContext>> _logger;
        private readonly DbSet<MessageTracking> _messages;
        private readonly IStorageHelper _storageHelper;

        public IdempotencyMiddleware(
            TContext context,
            IStorageHelper storageHelper,
            ILogger<IdempotencyMiddleware<TMessage, TContext>> logger)
        {
            CheckIfDbSetExists(context);

            _messages = context.Set<MessageTracking>();
            _storageHelper = storageHelper;
            _logger = logger;
        }

        public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next)
        {
            if (await TrackMessageAsync(message))
            {
                _logger.LogMessageExists(message);
                return;
            }

            try
            {
                await next(message);
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