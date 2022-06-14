using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ziggurat.Idempotency;
using Ziggurat.SqlServer.Internal.Storage;

namespace Ziggurat;

public static class MiddlewareOptionsExtensions
{
    public static void UseEntityFrameworkIdempotency<TMessage, TContext>(this MiddlewareOptions<TMessage> options)
        where TContext : DbContext
        where TMessage : IMessage
    {
        static void IdempotencySetupAction(IServiceCollection services) =>
            services
                .AddScoped<IStorage, EntityFrameworkStorage<TContext>>()
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage>>();

        options.Extensions.Add(IdempotencySetupAction);
    }
}