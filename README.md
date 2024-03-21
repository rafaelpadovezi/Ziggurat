# ![Ziggurat icon](./docs/icon.png) Ziggurat

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rafaelpadovezi_Ziggurat&metric=alert_status)](https://sonarcloud.io/dashboard?id=rafaelpadovezi_Ziggurat)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=rafaelpadovezi_Ziggurat&metric=coverage)](https://sonarcloud.io/dashboard?id=rafaelpadovezi_Ziggurat)

A .NET library to create message consumers.

Ziggurat implements functionalities to help solve common problems when dealing with messages:
- [Idempotency](https://microservices.io/patterns/communication-style/idempotent-consumer.html)
- Middleware: allows to create middlewares to consumers to handle logging, validation and whatever is needed. 

## How it works

The library uses the [decorator pattern](https://refactoring.guru/design-patterns/decorator/csharp/example) to execute a middleware pipeline when calling the consumer services. This way is possible to extend the service code adding new functionality.

The Idempotency middleware wraps the service enforcing that the message in only being processed once by tracking the message processing on the database.

Also, it's possible to add custom middlewares to the pipeline.

## Support

Ziggurat has support to:
- Storage:
  - MS SQL Server
  - MongoDB
- Messaging Library
  - [CAP](https://cap.dotnetcore.xyz/)

## Install

|                     |                                                                                                              |
|---------------------|--------------------------------------------------------------------------------------------------------------|
| Ziggurat            | [![Nuget](https://img.shields.io/nuget/v/Ziggurat)](https://www.nuget.org/packages/Ziggurat)                 |
| Ziggurat.CapAdapter | [![Nuget](https://img.shields.io/nuget/v/Ziggurat.CapAdapter)](https://www.nuget.org/packages/Ziggurat.CapAdapter) |
| Ziggurat.SqlServer  | [![Nuget](https://img.shields.io/nuget/v/Ziggurat.SqlServer)](https://www.nuget.org/packages/Ziggurat.SqlServer) |
| Ziggurat.MongoDB    | [![Nuget](https://img.shields.io/nuget/v/Ziggurat.MongoDB)](https://www.nuget.org/packages/Ziggurat.MongoDB) |

## Usage

Ziggurat works with middlewares. Registering middlewares adds functionality to the message consumer. Important to note that multiple middlewares can be registered to the same consumer. They are executed following the order of the registration.

### SQL Server with Entity Framework

Ziggurat integrates with the application [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) to track the processed messages and ensures that each message is processed only once. Also, the EF Core migrations are used to create the message tracking table with the correct constraints. If you are not using migration in your project the table must be created manually.

To use Ziggurat is necessary to create a message and a consumer service type:

```c#
public class MyMessage : IMessage
{
    public string Content { get; set; }
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}

public class MyMessageConsumerService : IConsumerService<MyMessage>
{
    private readonly MyDbContext _context;

    public MyMessageConsumerService(MyDbContext context)
    {
        _context = context;
    }

    public async Task ProcessMessageAsync(MyMessage message)
    {
        // Change the application bussiness objects tracked by EF Core
        _context.SomeEntity.Add(x);
        await _context.SaveChangesAsync();
    }
} 
```

Ziggurat.SqlServer ensures that the processed messages are tracked by the EF Core `DbContext`. Calling `SaveChangesAsync` will save the changes made to the business objects and the processed message to the DB.

The message type must implements the interface `IMessage`.

It's also required that the consumers are setup on the dependency injection configuration. Besides, it's necessary to add the CAP filter that enriches the message with the required information.


```c#
services
    .AddConsumerService<MyMessage, MyConsumerService>(
        options =>
        {
            options.UseEntityFrameworkIdempotency<MyMessage, MyDbContext>();
        });
services.
    .AddCap(x => ...)
    .AddSubscribeFilter<BootstrapFilter>();
```

And finally, the the message tracking DbSet must be added to the DbContext:

```c#
public class MyDbContext : DbContext
{
    public DbSet<MessageTracking> Messages { get; set; }
    ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.MapMessageTracker();
    }
}
```

### MongoDB

Using Ziggurat with MongoDB has some differences compared to SQL Server. The dependency injection registration must call the method `UseMongoDbIdempotency`:

```c#
builder.Services.AddConsumerService<MyMessage, ConsumerService>(
    options => options.UseMongoDbIdempotency("databaseName"));
```

To keep the consumer operation atomic, is necessary to use the method `StartIdempotentTransaction``:

```c#

public class MyMessageConsumerService : IConsumerService<MyMessage>
{
    private readonly IMongoClient _client;

    public MyMessageConsumerService(IMongoClient client)
    {
        _client = client;
    }

    public async Task ProcessMessageAsync(MyMessage message)
    {
        using var session = _client.StartIdempotentTransaction(message);
        // save business object
        var collection = _client.GetDatabase("databaseName").GetCollection<SomeEntity>("someEntity");
        await collection.InsertOneAsync(session, x);
        // must commit transaction
        await session.CommitTransactionAsync();
    }
}
```
### Logging middleware

Since version 8.0.0, Ziggurat has a built-in middleware to log the message processing. It's possible to use it by calling the method `UseLoggingMiddleware`:

```c#
services
    .AddConsumerService<MyMessage, MyConsumerService>(
        options =>
        {
            options.UseLoggingMiddleware<MyMessage>();
        });
```

### Custom middleware

It's possible to create custom middleware for the consumers.

```c#
public class MyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
    where TMessage : IMessage
{
   public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next)
    {
        // Do something before
        await next(message);
        // Do something after
    }
}
```

Also, it's required to register the middleware on the dependency injection configuration.

```c#
.AddConsumerService<MyMessage, MyMessageConsumerService>(
    options =>
    {
        options.Use<LoggingMiddleware<MyMessage>>();
    });
```

There is a new simple Middleware that tries to run at startup of the app a cleaner for the storage of Messaging control (Mongo or SQLServer).
It has a simple parameter that optionally can be passed (if not defined will default to 15 days), to clean stored messages, older than previous defined days.

```c#
app.UseZigguratCleaner(xx); ///xx number of days to clean older than history stored
```

Important to note that multiple middlewares can be registered to the same consumer. They are executed following the order of the registration.

You can look at the samples folder to see more examples of usage.

## Run tests

```shell
docker compose up -d mongoclustersetup sqlserver
dotnet test
```
