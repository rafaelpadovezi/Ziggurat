using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ziggurat.Idempotency;

namespace Ziggurat.SqlServer;

public static class CleanerOptionsExtensions
{
    public static CleanerOptions UseEntityFrameworkStorage<TContext>(this CleanerOptions options) where TContext : DbContext
    {
        options.RegisterStorage = services =>
        {
            services.TryAddScoped<IStorage, EntityFrameworkStorage<TContext>>();
        };
        return options;
    }
}