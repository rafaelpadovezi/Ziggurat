using MongoDB.Driver;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB
{
    public static class IdempotencyExtensions
    {
        public static IClientSessionHandle StartIdempotentTransaction(this IMongoClient client,
            IMessage message)
        {
            var clientSessionHandle = client.StartSession();
            clientSessionHandle.StartTransaction();

            // Don't have a solution for getting the database name yet
            client.GetDatabase("test").GetCollection<MessageTracking>("cap.processed").InsertOne(
                new MessageTracking(message.MessageId, message.MessageGroup));

            return clientSessionHandle;
        }
    }
}