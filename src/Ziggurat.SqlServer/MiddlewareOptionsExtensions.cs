using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// <param name="deleteHistoryMessagesFromDays">numer of days to exclude older messages from tracking table ( 0 or negative value will not exclude )</param>
    /// <typeparam name="TMessage">Type of the message of the consumer</typeparam>
    /// <typeparam name="TContext">Type of the DbContext</typeparam>
    public static void UseEntityFrameworkIdempotency<TMessage, TContext>(this MiddlewareOptions<TMessage> options, int deleteHistoryMessagesFromDays = 0)
        where TContext : DbContext
        where TMessage : IMessage
    {
        options.Extensions.Add(IdempotencySetupAction);

        static void IdempotencySetupAction(IServiceCollection services)
        {
            services
                .TryAddScoped<IStorage, EntityFrameworkStorage<TContext>>();
            services
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage>>();
        }
    }

}