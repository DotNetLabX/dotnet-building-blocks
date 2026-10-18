# Public names that differ from the reference

Every difference between the public and protected surface of the blocks as they are now (version one, then F3,
`docs/specs/F3-GatewayFeedback`) and the reference's blocks
(`dotnet-microservices`, commit `fda2eb7`, `src/BuildingBlocks/Blocks.*`, built as it is on net9). Both surfaces were
dumped with `tools/Blocks.SurfaceDump`; the reference dump is
`docs/specs/F1-VersionOne/delivery/surface-reference.txt`. The tool prints each nullable annotation as the source
declares it, unconstrained type parameters included (`T` versus `T?`). The comparison found 102 reference lines that
changed or went and 108 version-one lines that changed or are new. After F3 (12 blocks, `Blocks.Grpc` added) it finds
120 reference lines that changed or went and 181 current lines that changed or are new; F3 itself removed 24 lines
and added 79. Each line belongs to exactly one row below; a row marked F3 names the change of the F3 spec's § What
changes (C1–C12) and the dump lines it covers.

Every public type may become internal in version two; until then every public name here is provisional.

## Blocks.AspNetCore

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.AspNetCore.GlobalExceptionMiddleware` | `Blocks.AspNetCore.Middlewares.GlobalExceptionMiddleware` | the folder's namespace (spec item 3) |
| namespace moved | `Blocks.AspNetCore.Middleware.RequestDiagnosticsMiddleware` | `Blocks.AspNetCore.Middlewares.RequestDiagnosticsMiddleware` | the folder's namespace (spec item 3) |
| removed | `IRouteProvider.GetArticleId()`, `HttpContextProvider.GetArticleId()` | — use `GetRouteValue(key)` | product wording removed (spec item 4) |
| annotation changed | `IRouteProvider.GetRouteValue(string key)` and `HttpContextProvider.GetRouteValue(string key)` return `string` | return `string?` | warning fix: they forward to an extension that returns `string?` |
| added | — | `HttpContextProvider.TryGetUserIdText()` returns `string?` | F3 C2: the caller's id claim as written; 1 line |
| added | — | static class `ValidationFailureExtensions` with `ToFieldMap(this IEnumerable<ValidationFailure>, JsonNamingPolicy?)` returning `Dictionary<string, string[]>` | F3 C7: the one field-map conversion both writers use; 2 lines |
| added | — | static class `Blocks.AspNetCore.Middlewares.ApplicationBuilderExtensions` with `UseGlobalExceptionMiddleware(this IApplicationBuilder)` | F3 C10; 2 lines |

The two middleware classes keep their constructors and `InvokeAsync`; only the namespace changed (3 lines each).
The four gRPC types left this block for `Blocks.Grpc` (its section below).

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
| added | — | `IClaimsProvider.TryGetUserIdText()` returns `string?`, `IClaimsProvider.GetUserIdText()` returns `string`, both with default bodies built on `TryGetUserId()` | F3 C2: a text user id for who-typed commands; an implementer with today's members still compiles; 2 lines |

A caller that dereferences what these cache and JSON methods return now gets a nullable warning. The cache's
constructor, `Set<T>` and `Remove` differ only by namespace.

## Blocks.Domain

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.Domain.Entities.IAuditedEntity`, `Blocks.Domain.Entities.IAuditedEntity<TPrimaryKey>` (and its 4 properties) | `Blocks.Entities.IAuditedEntity`, `Blocks.Entities.IAuditedEntity<TPrimaryKey>` | the folder's majority namespace (spec item 3) |
| namespace moved | `Blocks.Entities.IDomainObject` | `Blocks.Domain.IDomainObject` | the folder's majority namespace (spec item 3) |
| base list changed | `AggregateRoot`, `AggregateRoot<TPrimaryKey>`, `AggregateTenantEntity`, `Entity`, `Entity<TPrimaryKey>`, `EnumEntity<TEnum>`, `IAggregateRoot`, `IAggregateRoot<TPrimaryKey>`, `IAssociationEntity`, `IEntity`, `IEntity<TPrimaryKey>`, `IMetadataEntity`, `IValueObject`, `SingleValueObject<T>`, `StringValueObject`, `TenantEntity`, `ValueObject` | the same types, same names and bases | their interface lists name `IAuditedEntity` and `IDomainObject` by the new namespaces; nothing else changed (for the first five types the current line is the next row's) |
| base list changed | `AggregateRoot`, `AggregateRoot<TPrimaryKey>`, `AggregateTenantEntity`, `IAggregateRoot`, `IAggregateRoot<TPrimaryKey>` (version one's lines) | the same, with `IHasDomainEvents` added to the interface list | F3 C1: every aggregate's events are dispatched through one interface; `IAggregateRoot<TPrimaryKey>` keeps its three event members; 5 lines |
| added | — | interface `Blocks.Entities.IHasDomainEvents`: `DomainEvents`, `AddDomainEvent(IDomainEvent)`, `ClearDomainEvents()` | F3 C1; 4 lines |
| added | — | interface `Blocks.Entities.IAuditedEntity<TPrimaryKey, TUserId>` (`where TPrimaryKey : struct where TUserId : class`): `TUserId CreatedById { get; init; }`, `CreatedOn`, `TUserId? LastModifiedById`, `LastModifiedOn` | F3 C2: the audit "who" as a type; 5 lines |
| added | — | interface `Blocks.Entities.IAggregateRoot<TPrimaryKey, TUserId>` : `IAuditedEntity<TPrimaryKey, TUserId>`, `IHasDomainEvents` | F3 C2; 1 line |
| added | — | class `Blocks.Entities.AggregateRoot<TPrimaryKey, TUserId>`: protected constructor, the four audit properties, `DomainEvents`, `AddDomainEvent`, `ClearDomainEvents` | F3 C2; 9 lines |
| base list changed | `IAuditableAction`, `IAuditableAction<TActionType>` | the same, extending the marker `IAuditableRequest` | F3 C2: the mediator step's constraint reaches both audited-command families; members unchanged; 2 + 2 lines |
| added | — | marker interface `Blocks.Domain.IAuditableRequest` | F3 C2; 1 line |
| added | — | interface `Blocks.Domain.IAuditableActionBy<TUserId>` (`where TUserId : class, IParsable<TUserId>`): `TUserId? CreatedById { get; set; }`, `IsAuthenticated`, `CreatedOn`, `Action`, `Comment` | F3 C2: the who-typed command, named so because `IAuditableAction<T>` already takes the action type; 6 lines |
| added | — | interface `Blocks.Domain.IAuditableActionBy<TUserId, TActionType>` (`where TActionType : Enum`): `ActionType` | F3 C2; 2 lines |
| added | — | static class `Blocks.Domain.AuditableActionExtensions` with `TryAssignCreatedBy(this object request, string userId, bool keepExisting)` | F3 C2: the three steps assign a who-typed id through it; 2 lines |

## Blocks.EntityFrameworkCore

| Change | Reference | Version one | Why |
|---|---|---|---|
| removed | protected virtual `AuditedEntityConfiguration<T, TKey>.DefaultDateSql` | — | the SQL Server-only created-on default is removed (D2) |
| renamed | `TransactionProvider.GetCurrentTransaction(CancellationToken)` | `GetCurrentTransactionAsync(CancellationToken)` | asynchronous methods end in "Async" (spec item 5) |
| namespace moved | `Blocks.EntityFrameworkCore.TransactionalDispatchDomainEventsInterceptor` | `Blocks.EntityFrameworkCore.Interceptors.TransactionalDispatchDomainEventsInterceptor` | the folder path on a tie (spec item 3); members unchanged |
| annotation changed | `TenantRepositoryBase<TContext, TEntity, TKey>.GetById(TKey, bool)` returns `TEntity` | returns `TEntity?` | warning fix: it returns null when not found and `throwNotFound` is false |
| annotation changed | `TenantRepositoryBase<TContext, TEntity, TKey>.GetAsync(TKey)` returns `Task<TEntity>` | returns `Task<TEntity?>` | warning fix: the lookup can find nothing |
| added | — | overrides `ApplicationDbContext<TDbContext>.SaveChanges(bool acceptAllChangesOnSuccess)` and `SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)` | F3 C12: a duplicate unique value becomes a 409 `ConflictException`; 2 lines |
| added | — | class `AuditedEntityConfiguration<T, TKey, TUserId>` (`where T : class, IAggregateRoot<TKey, TUserId>`): protected constructor, `Configure`, `HasConcurrencyToken` | F3 C2; 4 lines |
| added | — | a `JsonSerializerOptions options` overload of `BuildJsonReadOnlyListConvertor<T>`, `BuildJsonListConvertor<TCollection>`, `HasJsonReadOnlyListConversion<T>`, `HasJsonCollectionConversion<T>`, `HasJsonListConversion<T>` (complex type) and `HasJsonListConversion<TCollection, T>` | F3 C6: overloads, not optional parameters, so method groups still compile; 6 lines |
| added | — | `BuilderExtensions.BuildJsonValueComparer<T>()` and `BuildJsonValueComparer<T>(JsonSerializerOptions options)` | F3 C6: every conversion helper installs it; 2 lines |
| added | — | `DbContextExtensions.MigrateAsync<TDbContext>(this IHost, CancellationToken ct = default)` returning `Task<IHost>` | F3 C10; 1 line |
| added | — | protected virtual `DesignTimeFactoryBase<TContext>.CreateContext(DbContextOptions<TContext>)` | F3 C5: builds an options-only or an options-and-cache context; 1 line |

F3 also repaired the complex-type `HasJsonListConversion<T>()`: its signature is unchanged (no dump line), but it now
builds its converter for `T` itself, so EF accepts the model; before, it never built one.

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
| added | — | `Extensions.UseEnumsAsText(this Config)` and `Extensions.UseFieldMapValidationReply(this Config)` | F3 C8: the two halves of `UseCustomFastEndpoints`, usable alone; 2 lines |

## Blocks.Grpc

A new block in F3 (C9). It has no reference counterpart of its own; its types came from `Blocks.AspNetCore`.

| Change | Reference | Now | Why |
|---|---|---|---|
| moved to a new block | `Blocks.AspNetCore.Grpc.GrpcClientRegistrationExtensions` (`AddCodeFirstGrpcClient<TClient>`, `AddCodeFirstGrpcClientWithRetry<TClient>`, `AddCodeFirstGrpcClientAsSingleton<TClient>`), `GrpcServicesOptions`, `GrpcServiceSettings`, `GrpcRetrySettings`, in `Blocks.AspNetCore` | `Blocks.Grpc.…`, the same members, in `Blocks.Grpc` | F3 C9: an app that wants gRPC clients no longer takes the web block, and the web block no longer carries the gRPC packages; 16 + 16 lines |

## Blocks.Hasura

| Change | Reference | Version one | Why |
|---|---|---|---|
| renamed | `HasuraMetadataService.TrackObjectRelationship` | `TrackObjectRelationshipAsync` | asynchronous methods end in "Async" (spec item 5) |
| renamed | `HasuraMetadataService.TrackArrayRelationship` | `TrackArrayRelationshipAsync` | asynchronous methods end in "Async" (spec item 5) |
| renamed | `HasuraRegistration` (`AddHasuraGraphQL`, `AddHasuraMetadata`) | `HasuraRegistrationExtensions` | extension classes end in "Extensions" (spec item 5) |

## Blocks.MediatR

| Change | Reference | Version one | Why |
|---|---|---|---|
| namespace moved | `Blocks.MediatR.Behaviours.AssignUserIdBehavior<,>`, `LoggingBehavior<,>`, `ValidationBehavior<,>` | `Blocks.MediatR.Behaviors.…` | the folder's spelling (spec item 3); members unchanged (for `AssignUserIdBehavior<,>`'s type line the current line is the next row's) |
| constraint changed | `AssignUserIdBehavior<TRequest, TResponse>` `where TRequest : IAuditableAction` (version one's line) | `where TRequest : IAuditableRequest` | F3 C2: the same open-generic registration reaches who-typed requests, and every other request still skips it; 1 line |

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

| Reply | Reference | Version one | Since F3 |
|---|---|---|---|
| every error | `StatusCode`, `Message`, `TraceId`, `Details` | `statusCode`, `message`, `traceId`, `details` | unchanged |
| validation error, `errors` | `Errors`, a list of `PropertyName`, `ErrorMessage` | `errors`, a list of `propertyName`, `errorMessage` | `errors`, a map from field name to its messages, in order (C7); every part of a nested name lower-cased (`Address.Street` → `address.street`), in this error mapper and in `Blocks.FastEndpoints`' validation reply alike |
| argument error (`ArgumentException` and its subclasses) | not compared | 400 | 500, with the fixed text outside development (C4) |
| a duplicate unique value at save (SQL Server 2601/2627, PostgreSQL 23505), through `ApplicationDbContext<T>` or a repository | not compared | 500 | 409, `message` `A record with the same unique value already exists.` (C12) |
| an error after the reply has started | not compared | the handler itself threw, setting a status on a started reply | logged, the connection aborted, nothing written (C3) |

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
