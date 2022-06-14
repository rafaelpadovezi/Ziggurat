using System;

namespace Ziggurat.MongoDB;

internal static class ZigguratMongoDbOptions
{
    public const string ProcessedCollection = "ziggurat.processed";
    private static string _mongoDatabaseName;

    public static string MongoDatabaseName
    {
        get
        {
            if (string.IsNullOrEmpty(_mongoDatabaseName))
                throw new InvalidOperationException(
                    "MongoDB database name must be set. Be sure you are calling `UseMongoDbIdempotency`.");

            return _mongoDatabaseName;
        }
        set => _mongoDatabaseName = value;
    }
}