# Blocks.AspNetCore

## Purpose

ASP.NET Core plumbing shared by web services:

- `GlobalExceptionMiddleware` — turns exceptions into JSON error replies (`statusCode`, `message`, `traceId`,
  `details`; validation replies add `errors`). Add it with `app.UseGlobalExceptionMiddleware()`. 400 for validation,
  bad-request and domain errors; 404, 401, 403, 409 and 502 for the matching `Blocks.Exceptions` types; any other
  `HttpException` its own status; 499 when the client aborts the request; 500 otherwise. An argument error is a fault
  in the code and answers 500. Outside development only a 500 is replaced by the fixed text
  `An unexpected error occurred.`; every other status carries its exception's message, so write those messages for
  the client. An error after the reply has started is logged (an error at 500 and above, a warning below) and the
  connection is aborted, since nothing more can be written; a client cancellation after the start is left alone.
- Validation replies: `errors` is a map from field name to its messages, in order. A nested field name has every
  part lower-cased: Address.Street becomes address.street. An indexer is kept (`Items[0].StreetName` becomes
  `items[0].streetName`). `ValidationFailureExtensions.ToFieldMap` builds that map, and `Blocks.FastEndpoints` uses
  the same conversion, so both write the same `errors`.
- `RequestContextMiddleware` and `RequestDiagnosticsMiddleware` — fill and log `Blocks.Core`'s `RequestContext`.
  Add `RequestContextMiddleware` first: the diagnostics middleware reads what it fills. The diagnostics middleware
  warns about a request slower than `RequestDiagnosticsOptions.SlowRequestMs` (default 1000), or
  `SlowFileTransferMs` (default 3000) for an upload or a download. A streamed reply (content type
  `text/event-stream`) is never warned about, since it stays open for as long as it streams. The block does not
  bind the settings: unbound, the defaults apply. To change them, bind the `RequestDiagnosticsOptions` section with
  `services.AddAndValidateOptions<RequestDiagnosticsOptions>(configuration)`; once bound, the section is required
  and each limit must be at least 1.
- Correlation id: a client's `X-Correlation-ID` is kept only when it is sent once, holds at most 128 characters and
  only ASCII letters, digits, `-`, `_`, `.` and `:`. Otherwise the current trace id is used, or the server's request
  id when no trace is running. The id lands in `RequestContext.CorrelationId` and on the reply's `X-Correlation-ID`.
- `HttpContextProvider` — the one class that reads `HttpContext`; implements `IClaimsProvider` (its
  `TryGetUserIdText` returns the caller's id claim as written) and `IRouteProvider`.
- `AssignUserIdFilter` — a minimal-API filter that stamps the caller's id on `IAuditableAction` (an `int` id) and
  `IAuditableActionBy<TUserId>` requests. It sets the id only when the request has none and a claim is present, for
  both kinds alike.
- `GenericModelBinder<T>`, form-file and request helpers (`BaseUrl`, `GetClientIpAddress`, `GetContentType`).

The code-first gRPC client registration moved to `Blocks.Grpc`.

## Depends on

- Blocks: `Blocks.Core`, `Blocks.Domain`, `Blocks.Exceptions`.
- Packages: the `Microsoft.AspNetCore.App` shared framework.

## Registration

```csharp
services.AddHttpContextAccessor();
services.AddScoped<HttpContextProvider>();
services.AddScoped<IClaimsProvider>(sp => sp.GetRequiredService<HttpContextProvider>());
services.AddScoped<IRouteProvider>(sp => sp.GetRequiredService<HttpContextProvider>());
services.AddScoped<RequestContext>();
services.AddAndValidateOptions<RequestDiagnosticsOptions>(configuration); // optional: only to change the limits

app.UseGlobalExceptionMiddleware();
app.UseMiddleware<RequestContextMiddleware>();
app.UseMiddleware<RequestDiagnosticsMiddleware>();
```
