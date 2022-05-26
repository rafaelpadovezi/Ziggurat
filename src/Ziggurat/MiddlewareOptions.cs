using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ziggurat.Idempotency;

namespace Ziggurat
{
    public class MiddlewareOptions<TMessage>
        where TMessage : IMessage
    {
        internal List<Action<IServiceCollection>> Extensions { get; } = new();
        public string MongoDatabaseName { get; set; }

        public void UseIdempotency<TContext>() where TContext : DbContext
        {
            static void IdempotencySetupAction(IServiceCollection services) =>
                services.AddScoped<
                    IConsumerMiddleware<TMessage>,
                    IdempotencyMiddleware<TMessage, TContext>>();

            Extensions.Add(IdempotencySetupAction);
        }

        public void Use<TMiddleware>()
            where TMiddleware : class, IConsumerMiddleware<TMessage>
        {
            static void CustomerMiddlewareAction(IServiceCollection services) =>
                services.AddScoped<
                    IConsumerMiddleware<TMessage>,
                    TMiddleware>();

            Extensions.Add(CustomerMiddlewareAction);
        }
    }
}