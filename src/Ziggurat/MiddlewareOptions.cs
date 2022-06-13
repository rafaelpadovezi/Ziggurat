using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Ziggurat
{
    public class MiddlewareOptions<TMessage>
        where TMessage : IMessage
    {
        internal List<Action<IServiceCollection>> Extensions { get; } = new();

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