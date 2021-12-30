using DotNetCore.CAP;
using MongoDB.Bson;
using MongoDB.Driver;
using Ziggurat;
using Ziggurat.CapAdapter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Consumer>();
builder.Services.AddConsumerService<MyMessage, ConsumerService>(_ => { });

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));
builder.Services
    .AddCap(x =>
    {
        x.UseMongoDB(builder.Configuration.GetConnectionString("MongoDB"));
        x.UseRabbitMQ("");
    })
    .AddSubscribeFilter<BootstrapFilter>(); // Enrich the message with the required information;

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/", async (IMongoClient client, ICapPublisher capBus) =>
{
    using (var session = client.StartTransaction(capBus, autoCommit: true))
    {
        var collection = client.GetDatabase("test").GetCollection<MyMessage>("test.collection");
        await collection.InsertOneAsync(session, new MyMessage { Text = "Hey there"});

        await capBus.PublishAsync("sample.rabbitmq.mongodb", DateTime.Now);
    }

    return Results.Ok();
});

app.Run();