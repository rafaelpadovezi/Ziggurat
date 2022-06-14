using FluentAssertions;
using System;
using Xunit;

namespace Ziggurat.MongoDB.Tests;

[Collection("TextFixture Collection")]
public class ZigguratMongoDbOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MongoDatabaseName_IsNullOrEmpty_ThrowsException(string input)
    {
        // Arrange
        ZigguratMongoDbOptions.MongoDatabaseName = input;
        
        // Act
        Action action = () => _ = ZigguratMongoDbOptions.MongoDatabaseName;
        
        // Assert
        action.Should()
            .Throw<InvalidOperationException>(
                "MongoDB database name must be set. Be sure you are calling `UseMongoDbIdempotency`.");
    }
}