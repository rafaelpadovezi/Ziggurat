using DotNetCore.CAP;
using MongoDB.Driver;
using Sample.Cap.Mongo;
using Ziggurat;
using Ziggurat.CapAdapter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Consumer>();
builder.Services.AddConsumerService<MyMessage, ConsumerService>(options =>
{
    options.MongoDatabaseName = "test";
});

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
        var message = new MyMessage { Text = "Hey there" };
        var collection = client.GetDatabase("test").GetCollection<MyMessage>("test.collection");
        await collection.InsertOneAsync(session, message);

        await capBus.PublishAsync("myapp.paymentCondition.created", message);
    }

    return Results.Ok();
});

app.Run();