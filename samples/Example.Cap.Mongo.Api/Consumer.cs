using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MongoDB.Bson;
using MongoDB.Driver;
using Ziggurat;
using Ziggurat.MongoDB;

public class Consumer : ICapSubscribe
{
    private readonly IConsumerService<MyMessage> _service;

    public Consumer(IConsumerService<MyMessage> service)
    {
        _service = service;
    }

    [CapSubscribe("message", Group = "sample.rabbitmq.mongodb")]
    public async Task ConsumeMessage(MyMessage message)
    {
        await _service.ProcessMessageAsync(message);
    }
}

public class ConsumerService : IConsumerService<MyMessage>
{
    private readonly ILogger<ConsumerService> _logger;
    private readonly MongoClient _client;

    public ConsumerService(ILogger<ConsumerService> logger, MongoClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task ProcessMessageAsync(MyMessage message)
    {
        // IO
        _logger.LogInformation(message.Text);
        
        using var session = _client.StartIdempotentTransaction(message);
        var collection = _client.GetDatabase("test").GetCollection<MyMessage>("test.collection");

        await collection.InsertOneAsync(session, message);
    }
}