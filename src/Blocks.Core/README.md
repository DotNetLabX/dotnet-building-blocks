# Blocks.Core

## Purpose

General helpers with no web or database dependency:

- Options binding that fails at start-up: `AddAndValidateOptions<T>`, `ConfigureOptionsFromSection<T>`,
  `GetSectionByTypeName<T>`, `GetConnectionStringOrThrow` (the section is named after the type).
- Guards: `Guard` (`NotFound`, `AgainstNull`, `ThrowIfFalse`, …) and `OrThrowNotFound()`.
- Caching: `IThreadSafeMemoryCache` / `ThreadSafeMemoryCache`, `ICacheable`, type-keyed `IMemoryCache` helpers.
- Identity contract: `IClaimsProvider` (implemented by `Blocks.AspNetCore`), `JwtOptions`,
  `Base64UrlTokenGenerator`.
- `RequestContext` — the per-request correlation id, remote address and start time.
- Extensions for strings, enums, dates, JSON, reflection, assemblies and regular expressions;
  FluentValidation message helpers; Mapster set-up; `AsyncLock`; `MaxLength` constants.

## Depends on

- Blocks: `Blocks.Exceptions`.
- Packages: `FluentValidation`, `Mapster`, `Newtonsoft.Json`, `Microsoft.Extensions.Caching.Abstractions`,
  `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Configuration.Binder`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options`,
  `Microsoft.Extensions.Options.ConfigurationExtensions`, `Microsoft.Extensions.Options.DataAnnotations`.

## Registration

```csharp
services.AddAndValidateOptions<MyOptions>(configuration);   // section "MyOptions"
services.AddScoped<RequestContext>();                       // filled by RequestContextMiddleware
services.AddMemoryCache();                                  // the app's own IMemoryCache
services.AddSingleton<IThreadSafeMemoryCache, ThreadSafeMemoryCache>();
services.AddMapsterConfigsFromCurrentAssembly();
services.AddDerivedTypesOf(typeof(MyBase<>));               // registers every concrete subclass
```
