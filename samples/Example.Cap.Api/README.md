# Example API using [CAP](https://cap.dotnetcore.xyz/)

## Running API

Startup dependencies

```shell
docker compose up -d sqlserver rabbit
```

Run migrations

```shell
dotnet ef database update --project .\samples\Example.Cap.Api\ --context ExampleDbContext
```