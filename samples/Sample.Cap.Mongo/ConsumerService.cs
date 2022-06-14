using MongoDB.Driver;
using Ziggurat;
using Ziggurat.MongoDB;

namespace Sample.Cap.Mongo;

public class ConsumerService : IConsumerService<MyMessage>
{
    private readonly IMongoClient _client;
    private readonly ILogger<ConsumerService> _logger;

    public ConsumerService(ILogger<ConsumerService> logger, IMongoClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task ProcessMessageAsync(MyMessage message)
    {
        var databaseName = "test";
        _logger.LogInformation(message.Text);

        using var session = _client.StartIdempotentTransaction(message);
        var collection = _client.GetDatabase(databaseName).GetCollection<MyMessage>("test.collection");
        // save business object
        await collection.InsertOneAsync(session, message);
    }
}