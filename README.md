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

## Run tests

```shell
docker compose up -d sqlserver
dotnet test
```
