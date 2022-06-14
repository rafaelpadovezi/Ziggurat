using MongoDB.Driver;
using System;
using Xunit;

namespace Ziggurat.MongoDB.Tests.Support;

[Collection("TextFixture Collection")]
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