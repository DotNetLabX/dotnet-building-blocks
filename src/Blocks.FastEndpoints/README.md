# Blocks.FastEndpoints

## Purpose

FastEndpoints plumbing:

- `DomainEventPublisher` — publishes an `IDomainEvent` by its runtime type, so the `IEventHandler<T>` for the
  concrete event runs.
- `AssignUserIdPreProcessor` — a global pre-processor that stamps the caller's id on `IAuditableAction` (an `int`
  id) and `IAuditableActionBy<TUserId>` requests. It always replaces the id the caller sent and refuses the request
  when the claim is missing, for both kinds alike.
- `UseCustomFastEndpoints(configure)` — `UseFastEndpoints` with the `api` route prefix, property names read
  case-insensitively from a request body, then the two set-up methods below, then the app's own `configure`
  delegate, if it passes one. The delegate runs last, so it sees the block's settings and can add to them or
  replace them: a global pre-processor (`c.Endpoints.Configurator`), a naming policy, another converter.
- `UseEnumsAsText()` — on the endpoint `Config`, writes and reads enums as text.
- `UseFieldMapValidationReply()` — on the endpoint `Config`, answers a failed validation with a
  `FieldMapValidationReply`: `statusCode`, `message` (the failure's own message, or `Validation failed for multiple
  fields.`) and `errors`, a map from field name to its messages. Those three names stay as written under any naming
  policy. Field names go through the serializer's naming policy, read when the reply is written. A nested field name
  has every part lower-cased: Address.Street becomes address.street. The map is the one `Blocks.AspNetCore`'s error
  handler writes. It also declares `FieldMapValidationReply` as the 400 reply of every endpoint with a validator, so
  the API document describes the reply the endpoint really sends rather than the library's own error type.

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
// or, with the app's own settings on top of the block's:
app.UseCustomFastEndpoints(c => c.Endpoints.Configurator = endpoint => endpoint.PreProcessor<MyPreProcessor>(Order.Before));
```

`AssignUserIdPreProcessor` resolves `IClaimsProvider`; register it as `Blocks.AspNetCore`'s read-me shows,
and add the pre-processor in the endpoint configuration (`c.Endpoints.Configurator`).
