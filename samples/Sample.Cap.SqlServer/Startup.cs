using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Cap.SqlServer.Consumers;
using Sample.Cap.SqlServer.Domain.Services;
using Sample.Cap.SqlServer.Dtos;
using Sample.Cap.SqlServer.Infrastructure;
using Sample.Cap.SqlServer.Infrastructure.Middlewares;
using Ziggurat;
using Ziggurat.CapAdapter;

namespace Sample.Cap.SqlServer;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddDbContext<ExampleDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DbContext")));

        services.AddCap(x =>
            {
                x.UseEntityFramework<ExampleDbContext>();

                x.UseRabbitMQ(o =>
                {
                    o.HostName = Configuration.GetValue<string>("RabbitMQ:HostName");
                    o.Port = Configuration.GetValue<int>("RabbitMQ:Port");
                    o.ExchangeName = Configuration.GetValue<string>("RabbitMQ:ExchangeName");
                    //set optionally the prefetch count for messages (how many will enqueue in memory at a time before ack)
                    o.BasicQosOptions = new DotNetCore.CAP.RabbitMQOptions.BasicQos(1);
                });
            })
            .AddSubscribeFilter<BootstrapFilter>(); // Enrich the message with the required information

        services
            .AddScoped<OrderCreatedConsumer>()
            .AddZigguratCleaner(options )
            .AddConsumerService<OrderCreatedMessage, OrderCreatedConsumerService>(
                options =>
                {
                    options.Use<LoggingMiddleware<OrderCreatedMessage>>();
                    options.UseEntityFrameworkIdempotency<OrderCreatedMessage, ExampleDbContext>();
                });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Example API using CAP"); });
            endpoints.MapControllers();
        });
    }
}