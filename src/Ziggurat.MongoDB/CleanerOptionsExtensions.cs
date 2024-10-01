using Microsoft.Extensions.DependencyInjection.Extensions;
using Ziggurat.Idempotency;

namespace Ziggurat.MongoDB;

public static class CleanerOptionsExtensions
{
    public static CleanerOptions UseMongoDbStorage(this CleanerOptions options)
    {
        options.RegisterStorage = services =>
        {
            services.TryAddScoped<IStorage, MongoDbStorage>();
        };
        return options;
    }
}