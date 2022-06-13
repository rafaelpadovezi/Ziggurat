using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ziggurat.SqlServer;
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
                .AddSingleton<IStorageHelper, StorageHelperSqlServer>()
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage, TContext>>();

        options.Extensions.Add(IdempotencySetupAction);
    }
}