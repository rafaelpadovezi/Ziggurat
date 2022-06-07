using MongoDB.Driver;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB
{
    public static class IdempotencyExtensions
    {
        public static IClientSessionHandle StartIdempotentTransaction(this IMongoClient client,
            IMessage message, string databaseName)
        {
            var clientSessionHandle = client.StartSession();
            clientSessionHandle.StartTransaction();

            // Don't have a nice solution for getting the database name yet
            client.GetDatabase(databaseName).GetCollection<MessageTracking>("cap.processed").InsertOne(
                new MessageTracking(message.MessageId, message.MessageGroup));

            return clientSessionHandle;
        }
    }
}