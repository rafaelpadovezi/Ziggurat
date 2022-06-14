using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Ziggurat;

public class MiddlewareOptions<TMessage>
    where TMessage : IMessage
{
    internal List<Action<IServiceCollection>> Extensions { get; } = new();

    /// <summary>
    /// Register a middleware to Ziggurat pipeline. Middlewares are executed
    /// following the order of registration. 
    /// </summary>
    /// <typeparam name="TMiddleware">Type of the middleware</typeparam>
    public void Use<TMiddleware>()
        where TMiddleware : class, IConsumerMiddleware<TMessage>
    {
        static void CustomerMiddlewareAction(IServiceCollection services)
        {
            services.AddScoped<
                IConsumerMiddleware<TMessage>,
                TMiddleware>();
        }

        Extensions.Add(CustomerMiddlewareAction);
    }
}