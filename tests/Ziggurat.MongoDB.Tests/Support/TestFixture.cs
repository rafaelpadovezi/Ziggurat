using MongoDB.Driver;
using System;
using Xunit;

namespace Ziggurat.MongoDB.Tests.Support;

[Collection("TextFixture Collection")]
public class TestFixture
{
    public TestFixture()
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb");
        if (string.IsNullOrWhiteSpace(mongoConnectionString))
            mongoConnectionString = "mongodb://localhost:27017";
        ZigguratMongoDbOptions.MongoDatabaseName = $"test{Guid.NewGuid()}";
        MongoClient = new MongoClient(mongoConnectionString);
        MongoDatabase = MongoClient.GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName);
    }

    protected IMongoClient MongoClient { get; }
    protected IMongoDatabase MongoDatabase { get; }
}