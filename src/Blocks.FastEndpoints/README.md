# Blocks.FastEndpoints

## Purpose

FastEndpoints plumbing:

- `DomainEventPublisher` — publishes an `IDomainEvent` by its runtime type, so the `IEventHandler<T>` for the
  concrete event runs.
- `AssignUserIdPreProcessor` — a global pre-processor that stamps the caller's id on `IAuditableAction`
  requests.
- `UseCustomFastEndpoints()` — `UseFastEndpoints` with the `api` route prefix, enums as strings, and a
  validation reply of `statusCode`, `message` and `errors`.

## Depends on

- Blocks: `Blocks.AspNetCore`, `Blocks.Domain`.
- Packages: `FastEndpoints` (8.x).
- Pinned in the central versions file for this block: `protobuf-net`, `protobuf-net.Core` (through
  `Blocks.AspNetCore`).

## Registration

```csharp
services.AddFastEndpoints();
services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

app.UseCustomFastEndpoints();
```

`AssignUserIdPreProcessor` resolves `IClaimsProvider`; register it as `Blocks.AspNetCore`'s read-me shows,
and add the pre-processor in the endpoint configuration (`c.Endpoints.Configurator`).
