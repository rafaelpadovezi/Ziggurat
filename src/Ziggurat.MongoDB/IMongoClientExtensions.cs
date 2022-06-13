using MongoDB.Driver;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB
{
    public static class IMongoClientExtensions
    {
        public static IClientSessionHandle StartIdempotentTransaction(this IMongoClient client, IMessage message)
        {
            var clientSessionHandle = client.StartSession();
            clientSessionHandle.StartTransaction();

            client
                .GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName)
                .GetCollection<MessageTracking>("cap.processed")
                .InsertOne(new MessageTracking(message.MessageId, message.MessageGroup));

            return clientSessionHandle;
        }
    }
}