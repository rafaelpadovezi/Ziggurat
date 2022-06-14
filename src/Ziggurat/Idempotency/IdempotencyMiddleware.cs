using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Ziggurat.Internal;

namespace Ziggurat.Idempotency
{
    internal class IdempotencyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
        where TMessage : IMessage
    {
        private readonly ILogger<IdempotencyMiddleware<TMessage>> _logger;
        private readonly IStorage _storage;

        public IdempotencyMiddleware(
            ILogger<IdempotencyMiddleware<TMessage>> logger,
            IStorage storage)
        {
            _logger = logger;
            _storage = storage;
        }

        public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next)
        {
            if (await _storage.HasProcessedAsync(message))
            {
                _logger.LogMessageExists(message);
                return;
            }

            try
            {
                await next(message);
            }
            catch (Exception ex) when(_storage.IsMessageExistsError(ex))
            {
                // If is unique key error it means that the message
                // was already processed and should do nothing
                _logger.LogMessageExists(message);
            }
        }
    }
}