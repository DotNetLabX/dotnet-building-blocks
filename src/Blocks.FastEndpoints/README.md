# Blocks.FastEndpoints

## Purpose

FastEndpoints plumbing:

- `DomainEventPublisher` — publishes an `IDomainEvent` by its runtime type, so the `IEventHandler<T>` for the
  concrete event runs.
- `AssignUserIdPreProcessor` — a global pre-processor that stamps the caller's id on `IAuditableAction` (an `int`
  id) and `IAuditableActionBy<TUserId>` requests. It always replaces the id the caller sent and refuses the request
  when the claim is missing, for both kinds alike.
- `UseCustomFastEndpoints()` — `UseFastEndpoints` with the `api` route prefix, then the two set-up methods below.
- `UseEnumsAsText()` — on the endpoint `Config`, writes and reads enums as text.
- `UseFieldMapValidationReply()` — on the endpoint `Config`, answers a failed validation with `statusCode`,
  `message` (the failure's own message, or `Validation failed for multiple fields.`) and `errors`, a map from field
  name to its messages. Field names go through the serializer's naming policy, read when the reply is written. A
  nested field name has every part lower-cased: Address.Street becomes address.street. The map is the one
  `Blocks.AspNetCore`'s error handler writes.

Use the two set-up methods on their own when the app wants its own route prefix:
`app.UseFastEndpoints(c => { c.Endpoints.RoutePrefix = "v1"; c.UseEnumsAsText(); c.UseFieldMapValidationReply(); })`.
FastEndpoints keeps these settings for the whole process: the first host to start in a process fixes them.

## Depends on

- Blocks: `Blocks.AspNetCore`, `Blocks.Domain`.
- Packages: `FastEndpoints` (8.x).

## Registration

```csharp
services.AddFastEndpoints();
services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

app.UseCustomFastEndpoints();
```

`AssignUserIdPreProcessor` resolves `IClaimsProvider`; register it as `Blocks.AspNetCore`'s read-me shows,
and add the pre-processor in the endpoint configuration (`c.Endpoints.Configurator`).
