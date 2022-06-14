using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ziggurat.Idempotency;
using Ziggurat.SqlServer;

// ReSharper disable once CheckNamespace
namespace Ziggurat;

public static class MiddlewareOptionsExtensions
{
    /// <summary>
    /// Register IdempotencyMiddleware to the Ziggurat pipeline and setup
    /// Entity Framework dependencies.
    /// </summary>
    /// <param name="options"></param>
    /// <typeparam name="TMessage">Type of the message of the consumer</typeparam>
    /// <typeparam name="TContext">Type of the DbContext</typeparam>
    public static void UseEntityFrameworkIdempotency<TMessage, TContext>(this MiddlewareOptions<TMessage> options)
        where TContext : DbContext
        where TMessage : IMessage
    {
        static void IdempotencySetupAction(IServiceCollection services)
        {
            services
                .AddScoped<IStorage, EntityFrameworkStorage<TContext>>()
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage>>();
        }

        options.Extensions.Add(IdempotencySetupAction);
    }
}