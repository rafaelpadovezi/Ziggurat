# Example API using [CAP](https://cap.dotnetcore.xyz/)

## Running API

Startup dependencies

```shell
docker compose up -d mongoclustersetup rabbit
```

Call the API

```
curl --location --request POST 'http://localhost:5136/'
```