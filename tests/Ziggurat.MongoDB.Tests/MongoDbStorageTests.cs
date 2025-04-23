using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Ziggurat.MongoDB.Tests.Support;

namespace Ziggurat.MongoDB.Tests;

public class MongoDbStorageTests : TestFixture
{
    private readonly MongoDbStorage _storage;

    public MongoDbStorageTests()
    {
        _storage = new(MongoClient);
    }

    [Fact]
    public void IsMessageExistsError_TryInsertMessageTwice_ReturnTrue()
    {
        // Arrange
        var tracking = new TestMessage("1436814771495108608", "test.queue");
        using (var session = MongoClient.StartIdempotentTransaction(tracking))
        {
            session.CommitTransaction();
        }

        Exception exception = null;

        // Act

        try
        {
            using var _ = MongoClient.StartIdempotentTransaction(tracking);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        Assert.NotNull(exception);
        Assert.True(_storage.IsMessageExistsError(exception));
    }

    [Fact]
    public void IsMessageExistsError_TryDuplicateKeyOtherEntity_ReturnFalse()
    {
        // Arrange
        var testCollection = MongoDatabase.GetCollection<BsonDocument>("test");
        testCollection.InsertOne(new BsonDocument(new Dictionary<string, object>
        {
            ["_id"] = "1"
        }));
        Exception exception = null;

        // Act
        try
        {
            testCollection.InsertOne(new BsonDocument(new Dictionary<string, object>
            {
                ["_id"] = "1"
            }));
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act & Assert
        Assert.NotNull(exception);
        Assert.False(_storage.IsMessageExistsError(exception));
    }

    [Fact]
    public void IsMessageExistsError_InvalidOperationException_ReturnFalse()
    {
        // Act & Assert
        Assert.False(_storage.IsMessageExistsError(new InvalidOperationException()));
    }

    [Fact]
    public async Task HasProcessedAsync_MessageHasDifferentQueue_ReturnFalse()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");
        using (var _ = MongoClient.StartIdempotentTransaction(testMessage)) { } // insert message

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage(testMessage.MessageId, "other-queue"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageHasDifferentId_ReturnFalse()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");
        using (var _ = MongoClient.StartIdempotentTransaction(testMessage)) { } // insert message

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage("other-id", testMessage.MessageGroup));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageIsRepeated_ReturnTrue()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");
        using (var session = MongoClient.StartIdempotentTransaction(testMessage))
        {
            await session.CommitTransactionAsync();
        } // insert message

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage(testMessage.MessageId, testMessage.MessageGroup));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StartIdempotentTransaction_ExceptionIsThrown_RollbackInsert()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");

        // Act
        try
        {
            using (var _ = MongoClient.StartIdempotentTransaction(testMessage))
            {
                throw new();
            }
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch (Exception)
        {
        }

        // Assert
        var result = await _storage.HasProcessedAsync(new TestMessage(testMessage.MessageId, testMessage.MessageGroup));
        Assert.False(result);
    }

    [Fact]
    public async Task IsDeleteHistoryMessages_OlderThan30Days_ShouldReturn3()
    {
        // Arrange
        var testCollection = MongoDatabase.GetCollection<MessageTracking>(ZigguratMongoDbOptions.ProcessedCollection);

        var tracking1 = new MessageTracking("1436814771495108601", "test.queue");
        var tracking2 = new MessageTracking("1436814771495108602", "test.queue");
        var tracking3 = new MessageTracking("1436814771495108603", "test.queue");
        var tracking4 = new MessageTracking("1436814771495108604", "test.queue");
        var tracking5 = new MessageTracking("1436814771495108605", "test.queue");
        var tracking6 = new MessageTracking("1436814771495108606", "test.queue");

        var messages = new List<MessageTracking>() { tracking1, tracking2, tracking3, tracking4, tracking5, tracking6 };

        await testCollection.InsertManyAsync(messages);

        await MarkDocumentAsOld(testCollection, "1436814771495108601_test.queue");
        await MarkDocumentAsOld(testCollection, "1436814771495108602_test.queue");
        await MarkDocumentAsOld(testCollection, "1436814771495108603_test.queue");

        // Act
        var result = await _storage.DeleteMessagesHistoryOlderThanAsync(30, 1000, default);

        // Assert
        Assert.Equal(3, result);
        var remainingMessages = await testCollection.Find(_ => true).ToListAsync();
        Assert.Collection(remainingMessages,
            x => Assert.Equal("1436814771495108604_test.queue", x.Id),
            x => Assert.Equal("1436814771495108605_test.queue", x.Id),
            x => Assert.Equal("1436814771495108606_test.queue", x.Id));
        await testCollection.DeleteManyAsync(_ => true);
    }

    private static async Task MarkDocumentAsOld(IMongoCollection<MessageTracking> testCollection, string id)
    {
        var filter = Builders<MessageTracking>.Filter
            .Eq(x => x.Id, id);
        var update = Builders<MessageTracking>.Update
            .Set(x => x.DateTime, DateTime.Now.AddDays(-50));

        await testCollection.UpdateOneAsync(filter, update);
    }

    [Fact]
    public async Task InitializeAsync_WhenCalled_ShouldCreateIndex()
    {
        // Arrange
        var testCollection = MongoDatabase.GetCollection<MessageTracking>(ZigguratMongoDbOptions.ProcessedCollection);

        // Act
        await _storage.InitializeAsync(CancellationToken.None);

        // Assert
        var indexes = await testCollection.Indexes.ListAsync();
        var indexNames = await indexes.ToListAsync();
        Assert.Contains(indexNames, x => x["name"].AsString == nameof(MessageTracking.DateTime));
    }
}