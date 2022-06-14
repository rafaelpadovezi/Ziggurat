using FluentAssertions;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Ziggurat.MongoDB.Tests.Support;

namespace Ziggurat.MongoDB.Tests;

public class MongoDbStorageTests : TestFixture
{
    private readonly MongoDbStorage _storage;

    public MongoDbStorageTests()
    {
        _storage = new MongoDbStorage(MongoClient);
    }

    [Fact]
    public void IsMessageExistsError_TryInsertMessageTwice_ReturnTrue()
    {
        // Arrange
        var tracking = new TestMessage("1436814771495108608", "test.queue");
        using (var _ = MongoClient.StartIdempotentTransaction(tracking))
        {
        }

        Exception exception = null;

        // Act

        try
        {
            using var _ = MongoClient.StartIdempotentTransaction(tracking);
        }
        catch(Exception ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should().NotBeNull();
        _storage.IsMessageExistsError(exception).Should().BeTrue();
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
        catch(Exception ex)
        {
            exception = ex;
        }

        // Act & Assert
        exception.Should().NotBeNull();
        _storage.IsMessageExistsError(exception).Should().BeFalse();
    }

    [Fact]
    public void IsMessageExistsError_InvalidOperationException_ReturnFalse()
    {
        // Act & Assert
        _storage.IsMessageExistsError(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public async Task HasProcessedAsync_MessageHasDifferentQueue_ReturnFalse()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");
        using (var _ = MongoClient.StartIdempotentTransaction(testMessage)) { } // insert message

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage(testMessage.MessageId, "other-queue"));

        // Arrange
        result.Should().Be(false);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageHasDifferentId_ReturnFalse()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");
        using (var _ = MongoClient.StartIdempotentTransaction(testMessage)) { } // insert message

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage("other-id", testMessage.MessageGroup));

        // Arrange
        result.Should().Be(false);
    }
    
    [Fact]
    public async Task HasProcessedAsync_MessageIsRepeated_ReturnTrue()
    {
        // Arrange
        var testMessage = new TestMessage("1436814771495108608", "test.queue");
        using (var _ = MongoClient.StartIdempotentTransaction(testMessage)) { } // insert message

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage(testMessage.MessageId, testMessage.MessageGroup));

        // Arrange
        result.Should().Be(true);
    }
}