using Microsoft.Extensions.DependencyInjection;
using Ziggurat.Idempotency;
using Ziggurat.MongoDB;

namespace Ziggurat;

public static class MiddlewareOptionsExtensions
{
    public static void UseMongoDbIdempotency<TMessage>(this MiddlewareOptions<TMessage> options, string mongoDatabaseName)
        where TMessage : IMessage
    {

        ZigguratMongoDbOptions.MongoDatabaseName = mongoDatabaseName;

        static void IdempotencySetupAction(IServiceCollection services) =>
            services
                .AddScoped<IStorage, MongoDbStorage>()
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage>>();

        options.Extensions.Add(IdempotencySetupAction);
    }
}