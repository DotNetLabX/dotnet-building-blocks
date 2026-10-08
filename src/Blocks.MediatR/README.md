# Blocks.MediatR

## Purpose

MediatR plumbing for command and query handlers:

- `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>` — request markers.
- Pipeline behaviors: `AssignUserIdBehavior` (stamps the caller's id on `IAuditableAction` requests),
  `ValidationBehavior` (runs FluentValidation validators), `LoggingBehavior`.
- `DomainEventPublisher` — publishes `IDomainEvent`s through `IMediator`.

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
