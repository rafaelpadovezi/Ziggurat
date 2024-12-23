# Example API using [CAP](https://cap.dotnetcore.xyz/)

## Running API

Startup dependencies

```shell
docker compose up -d sqlserver rabbit
```

Run migrations

```shell
dotnet ef database update --project samples/Sample.Cap.SqlServer/ --context ExampleDbContext
```

Call the API

```
curl --location --request POST 'http://localhost:5000/order' \
--header 'Content-Type: application/json' \
--data-raw '{
    "Number": "123"
}'
```