# Blocks.Domain

## Purpose

Base types for a domain model:

- Entities: `Entity<TKey>` (equality by id, `IsNew`), `AggregateRoot<TKey>` (audit fields and a domain-event
  list), `EnumEntity<TEnum>`, `TenantEntity` and `AggregateTenantEntity` (`IMultitenancy`), and the
  `IAssociationEntity` / `IMetadataEntity` markers.
- Audit "who" as a type: `AggregateRoot<TKey, TUserId>` and `IAuditedEntity<TKey, TUserId>` record the creator and
  the last editor as `TUserId` (`LastModifiedById` is `TUserId?`). The who-type of the two-type forms is a reference
  type, such as string. They take no `Guid` or `int` who-type; for an `int` id keep the one-parameter forms, whose
  members are unchanged.
- Value objects: `ValueObject`, `StringValueObject`, `SingleValueObject<T>`.
- Domain events: `IDomainEvent` (both a MediatR notification and a FastEndpoints event) and
  `IDomainEventPublisher`, implemented by `Blocks.MediatR` and `Blocks.FastEndpoints`. Every aggregate carries its
  events through `IHasDomainEvents`, whatever its key and who-type, so `Blocks.EntityFrameworkCore` publishes them
  for all of them.
- `DomainException` for a broken domain rule.
- Commands that record who acted: `IAuditableAction` (an `int` id) and `IAuditableActionBy<TUserId>` (a reference
  who-type that implements `IParsable<TUserId>`, such as `string`), each with a form that adds an enum
  `ActionType`. Both extend the marker `IAuditableRequest`. `TryAssignCreatedBy` sets a who-typed command's id from
  the claim text.

Each step that stamps the caller's id keeps one rule, for `int` and who-typed commands alike:

| Step | Rule |
|---|---|
| `AssignUserIdFilter` (`Blocks.AspNetCore`) | sets the id only when the command has none and a claim is present |
| `AssignUserIdBehavior` (`Blocks.MediatR`) | replaces the caller's id whenever a claim is present |
| `AssignUserIdPreProcessor` (`Blocks.FastEndpoints`) | always replaces the caller's id; refuses the request when the claim is missing |

## Depends on

- Blocks: none.
- Packages: `MediatR.Contracts`, `FastEndpoints.Messaging.Core`.

## Registration

Nothing to register. Derive your entities and events from these types; register one
`IDomainEventPublisher` implementation from `Blocks.MediatR` or `Blocks.FastEndpoints`.
