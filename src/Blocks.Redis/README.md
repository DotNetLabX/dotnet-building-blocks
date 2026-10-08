# Blocks.Redis

## Purpose

A thin repository over Redis OM for entities with an integer id:

- `Entity` — the base type (`Id`).
- `Repository<T>` — get, exists, add, update, replace and delete; new ids come from a per-type sequence key
  (`{Type}:Id:Sequence`) through `GenerateNewIdAsync`.
- Extensions: `GetByIdAsync`, `GetByIdOrThrowAsync` (throws `NotFoundException`), `GenerateNewIdAsync`,
  `SetSequenceSeedAsync`, and `SeedFromJsonAsync` (loads `Data/Test/{Type}.json` into an empty collection).

## Depends on

- Blocks: `Blocks.Exceptions`.
- Packages: `Redis.OM` (brings `StackExchange.Redis`).

## Registration

```csharp
var multiplexer = ConnectionMultiplexer.Connect(configuration.GetConnectionStringOrThrow("Redis"));
services.AddSingleton<IConnectionMultiplexer>(multiplexer);
services.AddSingleton(new RedisConnectionProvider(multiplexer));
services.AddScoped<Repository<MyEntity>>();
```

Create the Redis OM index for each entity type at start-up (`provider.Connection.CreateIndex(typeof(MyEntity))`).
