# ![Ziggurat icon](./docs/icon.png) Ziggurat

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rafaelpadovezi_Ziggurat&metric=alert_status)](https://sonarcloud.io/dashboard?id=rafaelpadovezi_Ziggurat)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=rafaelpadovezi_Ziggurat&metric=coverage)](https://sonarcloud.io/dashboard?id=rafaelpadovezi_Ziggurat)

A .NET library to create message consumers.

Ziggurat implements functionalities to help solve common problems when dealing with messages:
- [Idempotency](https://microservices.io/patterns/communication-style/idempotent-consumer.html)

## How it works

The library uses the uses the [decorator pattern](https://refactoring.guru/design-patterns/decorator/csharp/example) to execute a middleware pipeline when calling the consumer services. This way is possible to extend the service code adding new functionality.

The Idempotency middleware wraps the service enforcing that the message in only being processed once by tracking the message processing on the database.

Also, it's possible to add custom middlewares to the pipeline.

## Requirements

Ziggurat has support only to MS SQL Server (as storage) and [CAP](https://cap.dotnetcore.xyz/) (as messaging library) for now. 

Besides, Ziggurat uses [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) to track the processed messages and the migrations functionality to create the table with the correct constraints. If you are not using migration on your project the table must be created manually. 

## Install

Ziggurat is shipped with two packages:

|                     |                                                                                                                                                     |
|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| Ziggurat            | [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Ziggurat)](https://www.nuget.org/packages/Ziggurat/1.0.0-beta)                      |
| Ziggurat.CapAdapter | [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Ziggurat.CapAdapter)](https://www.nuget.org/packages/Ziggurat.CapAdapter/1.0.0-beta) |

## Usage

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

    public OrderCreatedConsumerService(MyDbContext context)
    {
        _context = context;
    }

    public async Task ProcessMessageAsync(MyMessage message)
    {
        // Do something
        await _context.SaveChangesAsync();
    }
} 
```

The message type must implements the interface `IMessage`.

It's also required that the consumers are setup on the dependency injection configuration:


```c#
.AddConsumerService<MyMessage, MyConsumerService>(
    options =>
    {
        options.UseIdempotency<MyDbContext>();
    });
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

You can look at the samples folder to see more examples of usage.

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

## Run tests

```shell
docker compose up -d sqlserver
dotnet test
```
