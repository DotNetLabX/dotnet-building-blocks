# Blocks.MediatR

## Purpose

MediatR plumbing for command and query handlers:

- `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>` — request markers.
- Pipeline behaviors: `AssignUserIdBehavior` (stamps the caller's id on `IAuditableAction` and
  `IAuditableActionBy<TUserId>` requests), `ValidationBehavior` (runs FluentValidation validators),
  `LoggingBehavior`.
- `DomainEventPublisher` — publishes `IDomainEvent`s through `IMediator`.

The behaviour replaces the caller's id whenever a claim is present, for int and who-typed commands alike. With no
claim it leaves the id as sent. It runs only for requests that implement `IAuditableRequest` (both audited-command
families do), so any other request needs no `IClaimsProvider`.

## Depends on

- Blocks: `Blocks.Core`, `Blocks.Domain`.
- Packages: `MediatR` (12.5.0, the last Apache-2.0 release), `FluentValidation.DependencyInjectionExtensions`,
  `Microsoft.Extensions.Logging.Abstractions`.

## Registration

```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(AssignUserIdBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
services.AddValidatorsFromAssembly(typeof(Program).Assembly);
services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
```

Keep the behavior order: the user id is stamped before validation runs. `AssignUserIdBehavior` needs an
`IClaimsProvider` (from `Blocks.AspNetCore`); `LoggingBehavior` needs a scoped `RequestContext` (from `Blocks.Core`).
