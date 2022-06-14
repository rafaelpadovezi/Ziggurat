using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB;

public class MongoDbStorage : IStorage
{
    private readonly IMongoClient _client;

    public MongoDbStorage(IMongoClient client)
    {
        _client = client;
    }

    public bool IsMessageExistsError(Exception ex)
    {
        if (ex is not MongoWriteException mongoWriteException)
            return false;

        if (mongoWriteException.InnerException is not MongoBulkWriteException)
            return false;

        return mongoWriteException.WriteError.Category == ServerErrorCategory.DuplicateKey &&
               mongoWriteException.Message.Contains(ZigguratMongoDbOptions.ProcessedCollection);
    }

    public async Task<bool> HasProcessedAsync(IMessage message)
    {
        var collection = _client
            .GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName)
            .GetCollection<MessageTracking>(ZigguratMongoDbOptions.ProcessedCollection);
        var builder = Builders<MessageTracking>.Filter;
        var filter = builder.Eq(x => x.Id, MessageTracking.CreateId(message.MessageId, message.MessageGroup));

        return await collection.CountDocumentsAsync(filter) > 0;
    }
}

public static class IMongoClientExtensions
{
    public static IClientSessionHandle StartIdempotentTransaction(this IMongoClient client, IMessage message)
    {
        var clientSessionHandle = client.StartSession();
        clientSessionHandle.StartTransaction();

        client
            .GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName)
            .GetCollection<MessageTracking>(ZigguratMongoDbOptions.ProcessedCollection)
            .InsertOne(new MessageTracking(message.MessageId, message.MessageGroup));

        return clientSessionHandle;
    }
}