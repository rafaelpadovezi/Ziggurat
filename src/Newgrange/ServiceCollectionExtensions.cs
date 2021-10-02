using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newgrange.Idempotency;
using Newgrange.Internal.Storage;

namespace Newgrange
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsumerService<TMessage, TService>(
            this IServiceCollection services)
            where TService : class, IConsumerService<TMessage>
            where TMessage : IMessage
        {
            return services
                .AddSingleton<IStorageHelper, StorageHelperSqlServer>()
                .AddScoped<TService>()
                .AddScoped<IConsumerService<TMessage>>(t =>
                    new PipelineHandler<TMessage>(
                        t,
                        t.GetRequiredService<TService>())
                );
        }
    }
}