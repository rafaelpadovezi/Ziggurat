using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsumerService<TMessage, TContext, TService>(
            this IServiceCollection services)
            where TService : class, IConsumerService<TMessage>
            where TContext : DbContext
            where TMessage : IMessage
        {
            return services
                .AddScoped<TService>()
                .AddScoped<IConsumerService<TMessage>>(t =>
                    new IdempotencyService<TMessage, TContext>(
                        t.GetRequiredService<TContext>(),
                        t.GetRequiredService<TService>(),
                        t.GetRequiredService<ILogger<IdempotencyService<TMessage, TContext>>>())
                );
        }
    }
}