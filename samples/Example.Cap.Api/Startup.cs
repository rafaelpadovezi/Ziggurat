using Example.Cap.Api.Consumers;
using Example.Cap.Api.Domain.Services;
using Example.Cap.Api.Dtos;
using Example.Cap.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newgrange;
using Newgrange.CapAdapter;
using Newgrange.Idempotency;

namespace Example.Cap.Api
{
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
                    });
                })
                .AddSubscribeFilter<BootstrapFilter>(); // Enrich the message with the required information

            services
                .AddScoped<OrderCreatedConsumer>()
                .AddConsumerService<OrderCreatedMessage, OrderCreatedConsumerService>()
                .AddIdempotencyMiddleware<OrderCreatedMessage, ExampleDbContext>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Example API using CAP"); });
                endpoints.MapControllers();
            });
        }
    }
}