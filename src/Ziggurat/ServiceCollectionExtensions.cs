using System;
using Ziggurat;
using Ziggurat.Cleaner;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumerService<TMessage, TService>(
        this IServiceCollection services,
        Action<MiddlewareOptions<TMessage>> setupAction)
        where TService : class, IConsumerService<TMessage>
        where TMessage : IMessage
    {
        services
            .AddScoped<TService>()
            .AddScoped<IConsumerService<TMessage>>(t =>
                new PipelineHandler<TMessage>(
                    t,
                    t.GetRequiredService<TService>())
            );

        var options = new MiddlewareOptions<TMessage>();
        setupAction(options);

        foreach (var extension in options.Extensions)
            extension(services);

        return services;
    }

    /// <summary>
    /// Extension Method for implementing Middleware to clean Messages history
    /// </summary>
    /// <param name="app">the IApplicationBuilder</param>
    /// <param name="deleteOltherThanDays">The number of days max history allowed so that cleans older than those, deafults to 15 days</param>
    /// <returns>Returns the Middleware implementation</returns>
    public static IApplicationBuilder UseZigguratCleaner(this IApplicationBuilder app, int deleteOltherThanDays = 15) =>
        app.UseMiddleware<StorageCleanerMiddleware>(deleteOltherThanDays);
}