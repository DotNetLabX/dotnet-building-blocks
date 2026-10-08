# Blocks.AspNetCore

## Purpose

ASP.NET Core plumbing shared by web services:

- `GlobalExceptionMiddleware` — turns exceptions into JSON error replies (`statusCode`, `message`, `traceId`,
  `details`; validation replies add `errors`). 400 for validation, argument, bad-request and domain errors;
  any `HttpException` its own status; 499 when the client aborts the request; 500 otherwise. Outside development
  only a 500 is replaced by the fixed text; every other status carries its exception's message, so write those
  messages for the client.
- `RequestContextMiddleware` and `RequestDiagnosticsMiddleware` — fill and log `Blocks.Core`'s `RequestContext`.
- `HttpContextProvider` — the one class that reads `HttpContext`; implements `IClaimsProvider` and
  `IRouteProvider`.
- `AssignUserIdFilter` — a minimal-API filter that stamps the caller's id on `IAuditableAction` requests.
- Code-first gRPC clients: `AddCodeFirstGrpcClient`, `…WithRetry`, `…AsSingleton`, configured by
  `GrpcServicesOptions`.
- `GenericModelBinder<T>`, form-file and request helpers (`BaseUrl`, `GetClientIpAddress`, `GetContentType`).

## Depends on

- Blocks: `Blocks.Core`, `Blocks.Domain`, `Blocks.Exceptions`.
- Packages: `Grpc.Net.Client`, `protobuf-net.Grpc`; the `Microsoft.AspNetCore.App` shared framework.
- Pinned in the central versions file for this block: `protobuf-net`, `protobuf-net.Core` (the sync script's
  package report lists them).

## Registration

```csharp
services.AddHttpContextAccessor();
services.AddScoped<HttpContextProvider>();
services.AddScoped<IClaimsProvider>(sp => sp.GetRequiredService<HttpContextProvider>());
services.AddScoped<IRouteProvider>(sp => sp.GetRequiredService<HttpContextProvider>());
services.AddScoped<RequestContext>();
services.AddCodeFirstGrpcClient<IMyService>(configuration.GetSectionByTypeName<GrpcServicesOptions>());

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestContextMiddleware>();
```
