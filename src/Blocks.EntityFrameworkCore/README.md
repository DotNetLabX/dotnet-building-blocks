# Blocks.EntityFrameworkCore

## Purpose

EF Core building blocks:

- Repositories: `IRepository<TEntity, TKey>`, `RepositoryBase<TContext, TEntity>` (query, add, upsert,
  delete by id, save), `CachedRepository`, `TenantRepositoryBase`, and helpers such as
  `FindByIdOrThrowAsync` and `EnsureNotExistsOrThrowAsync` (throws `ConflictException`, answered 409).
- Entity configurations: `EntityConfiguration<T>`, `AuditedEntityConfiguration<T>` (audit columns, optional
  row version), `EnumEntityConfiguration`, `MetadataConfiguration`, `TenantEntityConfiguration`, and
  value-conversion helpers (enums, CSV, JSON).
- Domain events: `DispatchDomainEventsInterceptor` publishes after `SaveChanges`;
  `TransactionalDispatchDomainEventsInterceptor` saves and dispatches in one transaction
  (`TransactionProvider`, `TransactionOptions`).
- `ApplicationDbContext<T>` (cached reference-data reads), `ModelBuilder` naming helpers, `Migrate<TDbContext>()`.
- Seeding: `HasData` from `Data/Master/{Type}.json`; `SeedFromJsonFile<T>()` and `SeedTestData` at runtime.

**SQL Server only:** two seeding helpers run SQL Server statements and work only on SQL Server — the
manual-id insert scope (`UseManualGenerateId` / `ManualGenerateIdScope`, `SET IDENTITY_INSERT`) and the
table reseed (`TryReseedTable`, `DBCC CHECKIDENT`). Everything else works with any relational provider.

## Depends on

- Blocks: `Blocks.Core`, `Blocks.Domain`, `Blocks.Exceptions`.
- Packages: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`,
  `EFCore.NamingConventions`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Hosting.Abstractions`,
  `Microsoft.Extensions.Logging.Abstractions`, `Newtonsoft.Json`. Add your own database provider.

## Registration

```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
services.AddDbContext<MyDbContext>((sp, options) => options
    .UseSqlServer(configuration.GetConnectionStringOrThrow("Database"))
    .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));
services.AddScoped(typeof(Repository<>));   // your RepositoryBase<MyDbContext, T> subclass

app.Migrate<MyDbContext>();
```
