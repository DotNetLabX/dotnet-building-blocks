# Blocks.Messaging

## Purpose

MassTransit over RabbitMQ with the endpoint naming the blocks use:

- `AddMassTransitWithRabbitMQ` — registers every consumer in the given assembly, connects to RabbitMQ from
  `RabbitMqOptions`, and names endpoints in snake case with the service name as suffix
  (`SnakeCaseWithServiceSuffixNameFormatter`).
- `RabbitMqOptions` — `Host`, `UserName`, `Password`, `VirtualHost` (defaults: local broker, guest account).

## Depends on

- Blocks: `Blocks.Core`.
- Packages: `MassTransit.RabbitMQ` (MassTransit 8.x).

## Registration

```csharp
services.AddMassTransitWithRabbitMQ(configuration, typeof(Program).Assembly);
```

The configuration needs a `RabbitMqOptions` section; the service name is the first dot-separated part of the assembly name.
