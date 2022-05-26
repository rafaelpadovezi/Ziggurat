using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Threading.Tasks;
using Ziggurat.Idempotency;
using Ziggurat.Internal;
using Ziggurat.Internal.Storage;

namespace Ziggurat.MongoDB
{
    public class IdempotencyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
        where TMessage : IMessage
    {
        private readonly ILogger<IdempotencyMiddleware<TMessage>> _logger;
        private readonly MongoClient _client;
        private readonly MiddlewareOptions<TMessage> _options;
        private readonly IStorageHelper _storageHelper;

        public IdempotencyMiddleware(
            MongoClient client,
            MiddlewareOptions<TMessage> options,
            ILogger<IdempotencyMiddleware<TMessage>> logger)
        {
            _client = client;
            _options = options;
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
            var collection = _client.GetDatabase(_options.MongoDatabaseName).GetCollection<MessageTracking>("cap.processed");
            var builder = Builders<MessageTracking>.Filter;
            var filter = builder.Eq(x => x.Id, message.MessageId) & builder.Eq(x => x.Type, message.MessageGroup);
                
            return await collection.CountDocumentsAsync(filter) > 0;
        }
    }
}