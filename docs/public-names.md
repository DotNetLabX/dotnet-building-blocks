# Public names that differ from the reference

Every difference between the public and protected surface of version one and the reference's blocks
(`dotnet-microservices`, commit `fda2eb7`, `src/BuildingBlocks/Blocks.*`, built as it is on net9). Both surfaces were
dumped with `tools/Blocks.SurfaceDump`; the reference dump is
`docs/specs/F1-VersionOne/delivery/surface-reference.txt`. The tool prints each nullable annotation as the source
declares it, unconstrained type parameters included (`T` versus `T?`). The comparison found 102 reference lines that
changed or went and 108 version-one lines that changed or are new. Each line belongs to exactly one row below.

Every public type may become internal in version two; until then every public name here is provisional.

## Blocks.AspNetCore

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.AspNetCore.GlobalExceptionMiddleware` | `Blocks.AspNetCore.Middlewares.GlobalExceptionMiddleware` | the folder's namespace (spec item 3) |
| namespace moved | `Blocks.AspNetCore.Middleware.RequestDiagnosticsMiddleware` | `Blocks.AspNetCore.Middlewares.RequestDiagnosticsMiddleware` | the folder's namespace (spec item 3) |
| removed | `IRouteProvider.GetArticleId()`, `HttpContextProvider.GetArticleId()` | — use `GetRouteValue(key)` | product wording removed (spec item 4) |
| annotation changed | `IRouteProvider.GetRouteValue(string key)` and `HttpContextProvider.GetRouteValue(string key)` return `string` | return `string?` | warning fix: they forward to an extension that returns `string?` |

The two middleware classes keep their constructors and `InvokeAsync`; only the namespace changed (3 lines each).

## Blocks.Core

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.Core.Extensions.AssemblyExtensions` | `Blocks.Core.AssemblyExtensions` | the folder's majority namespace (spec item 3) |
| namespace moved | `Blocks.Core.Extensions.TypeExtensions` | `Blocks.Core.TypeExtensions` | the folder's majority namespace (spec item 3) |
| renamed and moved | `Blocks.Core.Extensions.RegexExtension` | `Blocks.Core.RegexExtensions` | extension classes end in "Extensions" (spec item 5); the folder's namespace (item 3) |
| namespace moved | `Blocks.Core.IThreadSafeMemoryCache`, `Blocks.Core.ThreadSafeMemoryCache` | `Blocks.Core.Cache.IThreadSafeMemoryCache`, `Blocks.Core.Cache.ThreadSafeMemoryCache` | the folder path on a tie (spec item 3) |
| annotation changed | `IThreadSafeMemoryCache.TryGet<T>(string, out T value)` and `ThreadSafeMemoryCache.TryGet<T>` | `out T? value` | warning fix: the annotations match `IMemoryCache`; a miss leaves the value at its default |
| annotation changed | `IThreadSafeMemoryCache.GetOrCreateAsync<T>` and `ThreadSafeMemoryCache.GetOrCreateAsync<T>` return `Task<T>` | return `Task<T?>` | the same warning fix |
| annotation changed | `IThreadSafeMemoryCache.GetOrCreate<T>(string, Func<ICacheEntry, T>)` and the four `ThreadSafeMemoryCache.GetOrCreate<T>` overloads return `T` | return `T?` | the same warning fix |
| annotation changed | `JsonExtensions.DeserializeCaseInsensitive<T>(string)` returns `T` | returns `T?` | warning fix: `JsonSerializer.Deserialize` returns `T?` |
| renamed | `Blocks.Mapster.DependencyInjection` (`AddMapsterConfigsFromCurrentAssembly`, `AddMapsterConfigsFromAssemblyContaining<T>`) | `Blocks.Mapster.MapsterRegistrationExtensions` | extension classes end in "Extensions" (spec item 5) |
| annotation changed | `ObjectExtensions.PropertiesToDictionary` returns `Dictionary<string, object>` | returns `Dictionary<string, object?>` | warning fix: property values can be null |
| annotation changed | `ObjectExtensions.ToStringDictionary` returns `Dictionary<string, string>` | returns `Dictionary<string, string?>` | warning fix: property values can be null |
| annotation changed | `ReflectionExtensions.GetValue(this MemberInfo, object)` returns `object` | returns `object?` | warning fix: the reflected value can be null |
| annotation changed | `StringExtensions.ToInt(this string input)` | `ToInt(this string? input)` | warning fix: `int.TryParse` already handles null |

A caller that dereferences what these cache and JSON methods return now gets a nullable warning. The cache's
constructor, `Set<T>` and `Remove` differ only by namespace.

## Blocks.Domain

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.Domain.Entities.IAuditedEntity`, `Blocks.Domain.Entities.IAuditedEntity<TPrimaryKey>` (and its 4 properties) | `Blocks.Entities.IAuditedEntity`, `Blocks.Entities.IAuditedEntity<TPrimaryKey>` | the folder's majority namespace (spec item 3) |
| namespace moved | `Blocks.Entities.IDomainObject` | `Blocks.Domain.IDomainObject` | the folder's majority namespace (spec item 3) |
| base list changed | `AggregateRoot`, `AggregateRoot<TPrimaryKey>`, `AggregateTenantEntity`, `Entity`, `Entity<TPrimaryKey>`, `EnumEntity<TEnum>`, `IAggregateRoot`, `IAggregateRoot<TPrimaryKey>`, `IAssociationEntity`, `IEntity`, `IEntity<TPrimaryKey>`, `IMetadataEntity`, `IValueObject`, `SingleValueObject<T>`, `StringValueObject`, `TenantEntity`, `ValueObject` | the same types, same names and bases | their interface lists name `IAuditedEntity` and `IDomainObject` by the new namespaces; nothing else changed |

## Blocks.EntityFrameworkCore

| Change | Reference | Version one | Why |
|---|---|---|---|
| removed | protected virtual `AuditedEntityConfiguration<T, TKey>.DefaultDateSql` | — | the SQL Server-only created-on default is removed (D2) |
| renamed | `TransactionProvider.GetCurrentTransaction(CancellationToken)` | `GetCurrentTransactionAsync(CancellationToken)` | asynchronous methods end in "Async" (spec item 5) |
| namespace moved | `Blocks.EntityFrameworkCore.TransactionalDispatchDomainEventsInterceptor` | `Blocks.EntityFrameworkCore.Interceptors.TransactionalDispatchDomainEventsInterceptor` | the folder path on a tie (spec item 3); members unchanged |
| annotation changed | `TenantRepositoryBase<TContext, TEntity, TKey>.GetById(TKey, bool)` returns `TEntity` | returns `TEntity?` | warning fix: it returns null when not found and `throwNotFound` is false |
| annotation changed | `TenantRepositoryBase<TContext, TEntity, TKey>.GetAsync(TKey)` returns `Task<TEntity>` | returns `Task<TEntity?>` | warning fix: the lookup can find nothing |

## Blocks.Exceptions

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.Linq.Extensions` (`SingleOrThrow`) | `Blocks.Exceptions.Extensions` | the folder's majority namespace (spec item 3) |
| added | — | `ForbiddenException` (403): constructors `(string)` and `(string, Exception)` | A1 |
| added | — | `ConflictException` (409): constructors `(string)` and `(string, Exception)` | A1 |
| added | — | `BadGatewayException` (502): constructors `(string)` and `(string, Exception)` | A1 |

## Blocks.FastEndpoints

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.FastEnpoints.AssignUserIdPreProcessor` | `Blocks.FastEndpoints.AssignUserIdPreProcessor` | the misspelled namespace fixed (spec item 3) |

## Blocks.Hasura

| Change | Reference | Version one | Why |
|---|---|---|---|
| renamed | `HasuraMetadataService.TrackObjectRelationship` | `TrackObjectRelationshipAsync` | asynchronous methods end in "Async" (spec item 5) |
| renamed | `HasuraMetadataService.TrackArrayRelationship` | `TrackArrayRelationshipAsync` | asynchronous methods end in "Async" (spec item 5) |
| renamed | `HasuraRegistration` (`AddHasuraGraphQL`, `AddHasuraMetadata`) | `HasuraRegistrationExtensions` | extension classes end in "Extensions" (spec item 5) |

## Blocks.MediatR

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.MediatR.Behaviours.AssignUserIdBehavior<,>`, `LoggingBehavior<,>`, `ValidationBehavior<,>` | `Blocks.MediatR.Behaviors.…` | the folder's spelling (spec item 3); members unchanged |

## Blocks.Messaging

| Change | Reference | Version one | Why |
|---|---|---|---|
| renamed | `Blocks.Messaging.MassTransit.DependencyInjection` (`AddMassTransitWithRabbitMQ`) | `Blocks.Messaging.MassTransit.MassTransitRegistrationExtensions` | extension classes end in "Extensions" (spec item 5) |

## Blocks.Redis

| Change | Reference | Version one | Why |
|---|---|---|---|
| renamed | `Repository<T>.Exists(int)` | `ExistsAsync(int)` | asynchronous methods end in "Async" (spec item 5) |
| renamed | `Repository<T>.GenerateNewId()`, `Repository<T>.GenerateNewId<TOther>()` | `GenerateNewIdAsync()`, `GenerateNewIdAsync<TOther>()` | the same |
| renamed | `Extensions.GenerateNewId<TEntity>(this IDatabase)` | `GenerateNewIdAsync<TEntity>` | the same |
| renamed | `Extensions.SetSequenceSeed<TEntity>(this IDatabase, int)` | `SetSequenceSeedAsync<TEntity>` | the same |
| renamed | `Extensions.SeedFromJson<TEntity>(this RedisConnectionProvider, IDatabase, string)` | `SeedFromJsonAsync<TEntity>` | the same |

## No change

`Blocks.Http.Abstractions`: no difference.

## The error reply on the wire

The error mapper (`Blocks.AspNetCore.Middlewares.GlobalExceptionMiddleware`) writes its reply with camelCase names
(D1). The reference wrote the C# names as they are.

| Reply | Reference | Version one |
|---|---|---|
| every error | `StatusCode`, `Message`, `TraceId`, `Details` | `statusCode`, `message`, `traceId`, `details` |
| validation error, each item of `errors` | `Errors` with `PropertyName`, `ErrorMessage` | `errors` with `propertyName`, `errorMessage` |

## The spec's table

Every row of the spec's § Public names table, and where it is above:

| Spec row | Here |
|---|---|
| FastEndpoints: namespace `Blocks.FastEnpoints` → `Blocks.FastEndpoints` | Blocks.FastEndpoints |
| MediatR: `Blocks.MediatR.Behaviours` → `Blocks.MediatR.Behaviors` | Blocks.MediatR |
| AspNetCore: both middlewares → `Blocks.AspNetCore.Middlewares` | Blocks.AspNetCore, first two rows |
| Core: `AssemblyExtensions`, `RegexExtension`, `TypeExtensions` → `Blocks.Core` | Blocks.Core, first three rows |
| Core: `IThreadSafeMemoryCache`, `ThreadSafeMemoryCache` → `Blocks.Core.Cache` | Blocks.Core, fourth row |
| Domain: `IAuditedEntity` → `Blocks.Entities` | Blocks.Domain, first row |
| Domain: `IDomainObject` → `Blocks.Domain` | Blocks.Domain, second row |
| Exceptions: `Extensions` (`SingleOrThrow`) → `Blocks.Exceptions` | Blocks.Exceptions, first row |
| EntityFrameworkCore: `TransactionalDispatchDomainEventsInterceptor` → `Blocks.EntityFrameworkCore.Interceptors` | Blocks.EntityFrameworkCore, third row |
| EntityFrameworkCore: `DefaultDateSql` removed (D2) | Blocks.EntityFrameworkCore, first row |
| AspNetCore: `GetArticleId()` removed, use `GetRouteValue(key)` | Blocks.AspNetCore, third row; `GetRouteValue` now returns `string?` (fourth row) |
| AspNetCore: error reply names in camelCase (D1) | The error reply on the wire |
| Exceptions: new `ForbiddenException`, `ConflictException`, `BadGatewayException` | Blocks.Exceptions, rows 2–4 |
| Core: `RegexExtension` → `RegexExtensions` | Blocks.Core, third row |
| Redis: `Exists` → `ExistsAsync` | Blocks.Redis, first row |
| Redis: `GenerateNewId` (repository and extension) → `GenerateNewIdAsync` | Blocks.Redis, rows 2–3 |
| Redis: `SetSequenceSeed` → `SetSequenceSeedAsync` | Blocks.Redis, fourth row |
| Redis: `SeedFromJson` → `SeedFromJsonAsync` | Blocks.Redis, fifth row |
| EntityFrameworkCore: `GetCurrentTransaction` → `GetCurrentTransactionAsync` | Blocks.EntityFrameworkCore, second row |
| Hasura: `TrackObjectRelationship`, `TrackArrayRelationship` → `…Async` | Blocks.Hasura |

Rows the comparison added beyond the spec's table: the nullable annotations in Blocks.AspNetCore, Blocks.Core and
Blocks.EntityFrameworkCore, all from the warning fixes recorded in step 2 of
`docs/specs/F1-VersionOne/delivery/implementation.md`; and the three registration classes renamed to end in
"Extensions" (Blocks.Core, Blocks.Hasura, Blocks.Messaging), under the naming rule of spec item 5.
