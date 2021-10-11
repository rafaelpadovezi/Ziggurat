using System;
using Microsoft.Extensions.DependencyInjection;
using Ziggurat.Internal.Storage;

namespace Ziggurat
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsumerService<TMessage, TService>(
            this IServiceCollection services,
            Action<MiddlewareOptions<TMessage>> setupAction)
            where TService : class, IConsumerService<TMessage>
            where TMessage : IMessage
        {
            services
                .AddSingleton<IStorageHelper, StorageHelperSqlServer>()
                .AddScoped<TService>()
                .AddScoped<IConsumerService<TMessage>>(t =>
                    new PipelineHandler<TMessage>(
                        t,
                        t.GetRequiredService<TService>())
                );

            var options = new MiddlewareOptions<TMessage>();
            setupAction(options);
            foreach (var extension in options.Extensions)
            {
                extension(services);
            }

            return services;
        }
    }

    public interface IMiddlewareExtension
    {
        public void AddServices(IServiceCollection services);
    }
}