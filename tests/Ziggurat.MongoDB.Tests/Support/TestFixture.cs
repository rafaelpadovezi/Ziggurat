using MongoDB.Driver;
using System;

namespace Ziggurat.MongoDB.Tests.Support;

public class TestFixture
{
    public TestFixture()
    {
        ZigguratMongoDbOptions.MongoDatabaseName = $"test{Guid.NewGuid()}";
        MongoClient = new MongoClient("mongodb://localhost:27017");
        MongoDatabase = MongoClient.GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName);
    }

    protected IMongoClient MongoClient { get; }
    protected IMongoDatabase MongoDatabase { get; }
}