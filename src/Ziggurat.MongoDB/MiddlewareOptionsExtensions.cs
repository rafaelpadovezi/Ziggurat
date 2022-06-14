using Microsoft.Extensions.DependencyInjection;
using Ziggurat.Idempotency;
using Ziggurat.MongoDB;

// ReSharper disable once CheckNamespace
namespace Ziggurat;

public static class MiddlewareOptionsExtensions
{
    /// <summary>
    /// Register IdempotencyMiddleware to the Ziggurat pipeline and setup
    /// MongoDB dependencies.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="mongoDatabaseName">Application database name. It will be used to store the tracking of the messages.</param>
    /// <typeparam name="TMessage">Type of the message of the consumer</typeparam>
    public static void UseMongoDbIdempotency<TMessage>(this MiddlewareOptions<TMessage> options,
        string mongoDatabaseName)
        where TMessage : IMessage
    {
        ZigguratMongoDbOptions.MongoDatabaseName = mongoDatabaseName;

        static void IdempotencySetupAction(IServiceCollection services)
        {
            services
                .AddScoped<IStorage, MongoDbStorage>()
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage>>();
        }

        options.Extensions.Add(IdempotencySetupAction);
    }
}