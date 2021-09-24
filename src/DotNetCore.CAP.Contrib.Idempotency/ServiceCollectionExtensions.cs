using DotNetCore.CAP.Contrib.Idempotency.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsumerService<TMessage, TContext, TService, TValidator>(
            this IServiceCollection services)
            where TService : class, IConsumerService<TMessage>
            where TContext : DbContext
            where TMessage : IMessage
            where TValidator : AbstractValidator<TMessage>
        {
            return services
                .AddScoped<IStorageHelper, StorageHelperSqlServer>()
                .AddScoped<TService>()
                .AddSingleton<IValidator<TMessage>, TValidator>()
                .AddScoped<IConsumerService<TMessage>>(service =>
                    new IdempotencyService<TMessage, TContext>(
                        service.GetRequiredService<TContext>(),
                        service.GetRequiredService<TService>(),
                        service.GetRequiredService<IStorageHelper>(),
                        service.GetRequiredService<ILogger<IdempotencyService<TMessage, TContext>>>(),
                        service.GetRequiredService<IValidator<TMessage>>()
                        )
                );
        }
    }
}