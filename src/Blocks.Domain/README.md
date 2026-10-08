# Blocks.Domain

## Purpose

Base types for a domain model:

- Entities: `Entity<TKey>` (equality by id, `IsNew`), `AggregateRoot<TKey>` (audit fields and a domain-event
  list), `EnumEntity<TEnum>`, `TenantEntity` and `AggregateTenantEntity` (`IMultitenancy`), and the
  `IAssociationEntity` / `IMetadataEntity` markers.
- Value objects: `ValueObject`, `StringValueObject`, `SingleValueObject<T>`.
- Domain events: `IDomainEvent` (both a MediatR notification and a FastEndpoints event) and
  `IDomainEventPublisher`, implemented by `Blocks.MediatR` and `Blocks.FastEndpoints`.
- `DomainException` for a broken domain rule, and `IAuditableAction` for commands that record who acted.

## Depends on

- Blocks: none.
- Packages: `MediatR.Contracts`, `FastEndpoints.Messaging.Core`.

## Registration

Nothing to register. Derive your entities and events from these types; register one
`IDomainEventPublisher` implementation from `Blocks.MediatR` or `Blocks.FastEndpoints`.
