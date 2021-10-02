using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newgrange.Internal.Storage;

namespace Newgrange.Idempotency
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIdempotencyMiddleware<TMessage, TContext>(
            this IServiceCollection services)
            where TMessage : IMessage
            where TContext : DbContext
        {
            return services
                .AddScoped<IConsumerMiddleware<TMessage>, IdempotencyMiddleware<TMessage, TContext>>();
        }
    }
}