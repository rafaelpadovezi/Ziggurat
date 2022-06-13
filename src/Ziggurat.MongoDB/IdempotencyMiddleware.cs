using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Threading.Tasks;
using Ziggurat.Idempotency;
using Ziggurat.Internal;

namespace Ziggurat.MongoDB
{
    internal class IdempotencyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
        where TMessage : IMessage
    {
        private readonly ILogger<IdempotencyMiddleware<TMessage>> _logger;
        private readonly IMongoClient _client;

        public IdempotencyMiddleware(
            IMongoClient client,
            ILogger<IdempotencyMiddleware<TMessage>> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next)
        {
            if (await HasProcessedAsync(message))
            {
                _logger.LogMessageExists(message);
                return;
            }

            await next(message); // TODO - handle duplicated message exception
        }

        private async Task<bool> HasProcessedAsync(TMessage message)
        {
            var collection = _client
                .GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName)
                .GetCollection<MessageTracking>("cap.processed");
            var builder = Builders<MessageTracking>.Filter;
            var filter = builder.Eq(x => x.Id, message.MessageId) & builder.Eq(x => x.Type, message.MessageGroup);
                
            return await collection.CountDocumentsAsync(filter) > 0;
        }
    }
}