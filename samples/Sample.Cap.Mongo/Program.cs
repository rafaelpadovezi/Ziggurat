using DotNetCore.CAP;
using MongoDB.Driver;
using Sample.Cap.Mongo;
using System.Globalization;
using Ziggurat;
using Ziggurat.CapAdapter;
using Ziggurat.MongoDB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Consumer>();
builder.Services.AddConsumerService<MyMessage, ConsumerService>(
    options => options.UseMongoDbIdempotency("test"));

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));
builder.Services
    .AddCap(x =>
    {
        x.UseMongoDB(builder.Configuration.GetConnectionString("MongoDB"));
        x.UseRabbitMQ("");
    })
    .AddSubscribeFilter<BootstrapFilter>(); // Enrich the message with the required information;

builder.Services.AddZigguratCleaner(options =>
{
    options.CleaningInterval = new TimeSpan(0, 5, 0);
    options.UseMongoDbStorage();
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/", async (IMongoClient client, ICapPublisher capBus) =>
{
    using (var session = client.StartTransaction(capBus, true))
    {
        var message = new MyMessage { Text = DateTime.Now.ToString(CultureInfo.InvariantCulture) };
        var collection = client.GetDatabase("test").GetCollection<MyMessage>("test.collection");
        await collection.InsertOneAsync(session, message);

        await capBus.PublishAsync("mymessage.created", message);
    }

    return Results.Ok();
});

app.Run();