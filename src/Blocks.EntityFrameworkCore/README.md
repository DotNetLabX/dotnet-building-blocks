# Blocks.EntityFrameworkCore

## Purpose

EF Core building blocks:

- Repositories: `IRepository<TEntity, TKey>`, `RepositoryBase<TContext, TEntity>` (query, add, upsert,
  delete by id, save), `CachedRepository`, `TenantRepositoryBase`, and helpers such as
  `FindByIdOrThrowAsync` and `EnsureNotExistsOrThrowAsync` (throws `ConflictException`, answered 409).
- Entity configurations: `EntityConfiguration<T>`, `AuditedEntityConfiguration<T>` (audit columns, optional
  row version) and `AuditedEntityConfiguration<T, TKey, TUserId>` for an aggregate with a who-type,
  `EnumEntityConfiguration`, `MetadataConfiguration`, `TenantEntityConfiguration`, and value-conversion helpers
  (enums, CSV, JSON).
- Stored JSON: `HasJsonCollectionConversion`, `HasJsonReadOnlyListConversion`, both `HasJsonListConversion` forms
  and the two converter builders each take `JsonSerializerOptions` in an overload; without them they write the
  JSON they always wrote. Every conversion helper also installs a value comparer (`BuildJsonValueComparer<T>`) that
  compares the stored text, so a replaced element and a changed member of an element are both saved.
- Domain events: `DispatchDomainEventsInterceptor` publishes after `SaveChangesAsync`, for every aggregate
  (`IHasDomainEvents`); `TransactionalDispatchDomainEventsInterceptor` saves and dispatches in one transaction
  (`TransactionProvider`, `TransactionOptions`). A synchronous `SaveChanges` publishes nothing.
- `ApplicationDbContext<T>` (cached reference-data reads), `ModelBuilder` naming helpers, `Migrate<TDbContext>()`
  and `MigrateAsync<TDbContext>()`.
- `DesignTimeFactoryBase<TContext>` for the migrations tool: override `ConfigureProvider`; it builds a context whose
  constructor takes the options, or the options and an `IMemoryCache` (as `ApplicationDbContext<T>` does).
  Override `CreateContext` for any other constructor.
- A duplicate unique value on SQL Server or PostgreSQL answers 409: a save through `ApplicationDbContext<T>` or
  through a repository turns that database error into a `ConflictException` whose message is
  `A record with the same unique value already exists.` Only SQL Server errors 2601 and 2627 and PostgreSQL's
  23505 count; every other save failure, and every other database, is left as it is. A context not built on
  `ApplicationDbContext<T>` and saved without a repository is not translated.
- Seeding: `HasData` from `Data/Master/{Type}.json`; `SeedFromJsonFile<T>()` and `SeedTestData` at runtime.

**SQL Server only:** two seeding helpers run SQL Server statements and work only on SQL Server — the
manual-id insert scope (`UseManualGenerateId` / `ManualGenerateIdScope`, `SET IDENTITY_INSERT`) and the
table reseed (`TryReseedTable`, `DBCC CHECKIDENT`). Everything else works with any relational provider.

## Depends on

- Blocks: `Blocks.Core`, `Blocks.Domain`, `Blocks.Exceptions`.
- Packages: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`,
  `EFCore.NamingConventions`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Hosting.Abstractions`,
  `Microsoft.Extensions.Logging.Abstractions`, `Newtonsoft.Json`. Add your own database provider and design package.
  For example `Microsoft.EntityFrameworkCore.SqlServer` or `Npgsql.EntityFrameworkCore.PostgreSQL`, and
  `Microsoft.EntityFrameworkCore.Design` for the migrations tool.

## Registration

```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
services.AddDbContext<MyDbContext>((sp, options) => options
    .UseSqlServer(configuration.GetConnectionStringOrThrow("Database"))
    .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));
services.AddScoped(typeof(Repository<>));   // your RepositoryBase<MyDbContext, T> subclass

await app.MigrateAsync<MyDbContext>();   // or app.Migrate<MyDbContext>()
```
