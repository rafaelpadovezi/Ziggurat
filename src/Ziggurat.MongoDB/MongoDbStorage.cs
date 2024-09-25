using MongoDB.Driver;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB;

public class MongoDbStorage : IStorage
{
    private readonly IMongoCollection<MessageTracking> _collection;

    public MongoDbStorage(IMongoClient client)
    {
        _collection = client
            .GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName)
            .GetCollection<MessageTracking>(ZigguratMongoDbOptions.ProcessedCollection);
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
        var builder = Builders<MessageTracking>.Filter;
        var filter = builder.Eq(x => x.Id, MessageTracking.CreateId(message.MessageId, message.MessageGroup));

        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    public async Task<int> DeleteMessagesHistoryOlderThanAsync(int days, int batchSize, CancellationToken cancellationToken)
    {
        var builder = Builders<MessageTracking>.Filter;
        var filter = builder.Lte(x => x.DateTime, DateTime.Now.AddDays(-days));

        var res = await _collection.DeleteManyAsync(filter, cancellationToken);

        return (int)res.DeletedCount;
    }

    public async Task InitializeAsync(CancellationToken stoppingToken)
    {
        await TryCreateIndexAsync(stoppingToken);
    }

    private async Task TryCreateIndexAsync(CancellationToken cancellationToken)
    {
        const string indexName = nameof(MessageTracking.DateTime);
        using (var cursor = await _collection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false))
        {
            var existingIndexes = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            var existingIndexNames = existingIndexes.Select(o => o["name"].AsString).ToArray();
            if (existingIndexNames.Contains(indexName))
                return;
        }

        var indexOptions = new CreateIndexOptions
        {
            Name = indexName,
            Background = true,
        };
        var indexKeysDefinition = Builders<MessageTracking>.IndexKeys.Ascending(x => x.DateTime);
        var dateTimeIndex = new CreateIndexModel<MessageTracking>(indexKeysDefinition, indexOptions);

        await _collection.Indexes.CreateOneAsync(dateTimeIndex, null, cancellationToken);
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
            .InsertOne(clientSessionHandle, new MessageTracking(message.MessageId, message.MessageGroup));

        return clientSessionHandle;
    }
}