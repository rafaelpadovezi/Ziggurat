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

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _ = ZigguratMongoDbOptions.MongoDatabaseName);
        const string expectedMessage =
            "MongoDB database name must be set. Be sure you are calling `UseMongoDbIdempotency`.";
        Assert.Equal(expectedMessage, exception.Message);
    }
}