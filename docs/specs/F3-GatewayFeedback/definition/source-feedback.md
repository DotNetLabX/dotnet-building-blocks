<!-- Copied 2026-10-11 from D:/src/knowledge-gateway/docs/plugin-feedback/blocks-2026-10-10.md (knowledge-gateway commit b71bbf6d). The source of F3-GatewayFeedback; not edited. -->

# Findings against the copied blocks — for the blocks repo (2026-10-10, knowledge-gateway, F194-ReferenceFoundations)

**What happened.** Four definitions were written that give this repo's three services their base in the reference's
form (`docs/specs/F201-SharedBase` … `F204-StoreBase`). Three readers per definition (Codex, Sonnet, Fable) read
them against the copied blocks under `src/BuildingBlocks/`. Where a block cannot do, as copied, what the rules ask
of it for this repo's shapes, the finding is collected here, as the owner asked on 2026-10-10, so it can go back to
the blocks repo (`D:\src\dotnet-building-blocks`). Nothing in a block was changed. The readers' full text is in each
feature's `delivery/review-critic.md`.

This repo differs from the reference in four ways that matter below: it streams replies; its aggregates and tables
have `Guid`, text and composite keys; one host composes several services and modules; one service is live, with
tables and settings that already exist.

## 1. Domain events are dispatched only from `int`-keyed aggregates

- **Where:** `Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.DomainEvents.cs:13` selects
  `.OfType<IAggregateRoot>()`; `Blocks.Domain/Entities/AggregateRoot.cs:5` declares
  `IAggregateRoot : IAggregateRoot<int>`.
- **Effect here:** AgentStudio's four aggregates have a `Guid` id (kept by rule B9). On the blocks' base they are
  `AggregateRoot<Guid>` and are never selected, so `SkillPublished` is silently not sent on save.
- **Question:** should the dispatch select aggregates of any key type?

## 2. Entities, repositories and configurations accept value-type keys only; there is no base for a text or a composite key

- **Where:** `Blocks.Domain/Entities/Entity.cs:9-15` (`IEntity<TPrimaryKey> … where TPrimaryKey : struct`),
  `Blocks.EntityFrameworkCore/Repositories/IRepository.cs:8-10`, `Repository.cs:8-17`,
  `EntityConfigurations/EntityConfiguration.cs:18-24`; the `…OrThrow…` helpers in
  `Extensions/RepositoryExtensions.cs` are `int`-keyed.
- **Effect here:** 8 of the employee service's 14 tables have a text key or a composite one (`notes`, the five
  sign-in tables of the agents' tools, the two staged-result tables); AgentStudio's reference rows have composite
  keys with a text part. They cannot take the shared base, and a surrogate key would change live tables.
- **Question:** what is the reference's form for a table whose key is text or composite — a base without a key
  constraint, association rows owned by an aggregate (`IAssociationEntity`), or something else?

## 3. The aggregate base carries audit members the existing tables do not have

- **Where:** `Blocks.Domain/Entities/AggregateRoot.cs:22-25` (`CreatedById` of the key's type, `DateTime CreatedOn`,
  `LastModifiedById`, `LastModifiedOn`); `EntityConfigurations/AuditedEntityConfiguration.cs:28-31` maps the first
  two as required.
- **Effect here:** AgentStudio knows its users by e-mail, has no user ids, and keeps `DateTimeOffset` times in
  columns of other names. Deriving the base gives each aggregate four members with no column, or forces new
  required columns onto live tables.
- **Question:** is ignoring the audit members in a configuration the intended way for an aggregate without them, or
  should there be an aggregate base without audit?

## 4. The error handler writes into a reply that has already started

- **Where:** `Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs:106-112` sets the status code without
  checking `HasStarted` (only the 499 branch checks it).
- **Effect here:** both assistants stream their answers; an error after the first event makes the handler itself
  throw.
- **Asked:** leave a started reply alone and log the error. (This is the one block change `F201-SharedBase` makes in
  this repo's copy and hands over.)

## 5. The error handler logs nothing below 500 and puts an argument error's own text in a 400

- **Where:** same file, `:21-22` (`ArgumentException` → 400), `:67-71` (logs only `>= 500`), `:73-78` (the fixed
  sentence replaces the message only for a 500 outside development).
- **Effect here:** until a service's classes are rewritten, its argument checks are internal checks (about 150 in
  the store, 411 in the employee service). A fault that trips one is answered as the caller's mistake, is not logged
  by the handler, and carries the parameter name and sometimes the value in the body — on the live store too.
- **Question:** should an argument error keep its text out of the body outside development, and should the handler
  log what it maps from a framework error?

## 6. The handler's list of errors has one producer, and a service may not use it

- **Where:** same file, `:45-47, 86-104`: `errors` is written only for `FluentValidation.ValidationException`, as
  `{propertyName, errorMessage}`. `DomainException` (`Blocks.Domain/DomainException.cs`) carries one message.
- **Effect here:** a refused publish in AgentStudio returns a list of findings, each a rule id and a message, which
  the web app shows. Rule A55 keeps framework error types out of services and says a service's error carries no
  extra data, so nothing a service may raise fills the list.
- **Question:** how does the reference return several rule findings for one refused request?

## 7. Two different forms for a failed validation, one of them bundled with a route prefix

- **Where:** `Blocks.FastEndpoints/Extensions.cs:9-35` — `UseCustomFastEndpoints` sets `RoutePrefix = "api"`, an enum
  converter and the validation reply (`{statusCode, message, errors{…}}`) in one call. The middleware's own
  validation branch writes a third shape (`errors` as an array) for an exception the endpoint library does not throw.
- **Effect here:** the hosts' endpoints declare full routes (`/api/…` in 26 files), so the call cannot be adopted
  whole; adopting only its reply builder means copying part of a block.
- **Question:** can the reply builder be set without the prefix, and which of the two shapes is the reference's?

## 8. The design-time factory cannot build a context on the blocks' own base context

- **Where:** `Blocks.EntityFrameworkCore/DbContextDesignTimeFactory.cs:21` calls
  `Activator.CreateInstance(typeof(TContext), builder.Options)`; `ApplicationDbContext.cs:7` takes
  `(DbContextOptions<TDbContext>, IMemoryCache)`.
- **Effect here:** every context this repo adds is on the base (rules Y18, A44), so the factory throws for it. In the
  reference the one factory serves a context that is not on the base.
- **Question:** is host-based discovery the intended way to create migrations for a context on the base?

## 9. The settings helper: an optional section, an empty section, nested classes, a second validator

- **Where:** `Blocks.Core/Configurations/ConfigurationExtensions.cs:8-23` — binds `typeof(T).Name`, refuses a section
  for which `Exists()` is false, then `ValidateDataAnnotations().ValidateOnStart()`. The same file holds two more
  forms (`ConfigureOptionsFromSection<T>`, which checks nothing; `GetSectionByTypeName<T>`), which the read-me lists
  together as "options binding that fails at start-up".
- **Effects here:**
  - Three subsystems are switched on by their section's presence and off by its absence, with tests and rule rows
    that say so (`AI:Failover`; the Fabric data pool; the indexer's data pool). "A missing section stops the
    start-up" has no form for them.
  - A service whose every value has a default (the store) must still carry the section, and an empty object does
    not count as present.
  - Attribute validation does not reach nested classes or list items, and the helper registers no validator class,
    so a rule on a nested class would silently never run.
- **Questions:** what is the reference's form for a subsystem that may be absent — an always-present section with a
  switch? And how is a rule on a nested settings class, or across two values, checked through the helper?

## 10. The stored-JSON converters read with default, case-sensitive options and have no value comparer

- **Where:** `Blocks.EntityFrameworkCore/Extensions/BuilderExtensions.cs` (`BuildJson…Convertor`).
- **Effect here:** AgentStudio's stored documents are camelCase; read with these converters their members come back
  empty, and the next write changes the case. An in-place change to a list may not be saved.
- **Question:** should the converters take the serializer options, and carry a comparer?

## 11. Nothing translates a unique-key violation into the conflict error

- **Where:** `Extensions/RepositoryExtensions.cs:40` raises the conflict error from an explicit check
  (`EnsureNotExistsOrThrowAsync`); a `DbUpdateException` from the database is not mapped.
- **Effect here:** "a second version of the same number" is decided by the database under a race; without a
  translation it answers 500.

## 12. Smaller ones

- `Blocks.AspNetCore` brings `Grpc.Net.Client` and `protobuf-net.Grpc` into any host that takes it for the error
  handler alone.
- `Blocks.EntityFrameworkCore` has no PostgreSQL provider and no design package in its references; its seed helpers
  are for SQL Server only; `Migrate` is synchronous.
- The read-me of `Blocks.Core` and the handler have no `Use…` extension for the middleware; the form is
  `app.UseMiddleware<GlobalExceptionMiddleware>()`.
- A settings class named like a library's own type collides in the files that use both: the store's root, by the
  short-name rule, would be `ChatOptions`, which is also `Microsoft.Extensions.AI.ChatOptions` (five store files).
  This is a question for the naming rule, not for a block.

## 13. What a lookup in the reference project showed, the same day

A read-only lookup in `D:\src\dotnet-microservices` and `D:\src\dotnet-building-blocks` on 2026-10-10, made to
find the form the reference already has for points 2, 3, 5, 6 and 9 above. The first four items below were
re-checked by hand; the rest are as the lookup reported them.

- **The two copies of the blocks differ.** The reference's own copy (`D:\src\dotnet-microservices\src\BuildingBlocks`)
  has 12 `//insight` comments; the blocks repo has none. The reference's error handler writes the error's message
  in every environment (`GlobalExceptionMiddleware.cs:58`); the blocks repo's replaces a 500's text outside
  development. Which copy is the master for a form, and where the insight comments live, is not written anywhere
  this repo could find.
- **`ConfigureOptionsFromSection<T>` has no caller** in the reference. It binds without checking anything, and the
  read-me lists it beside the form that checks.
- **A settings class read only by `GetSectionByTypeName<T>` is never checked.** `GrpcServicesOptions` is read that
  way in three services, so its `[Range]` and `[Required]` never run; `Review.API/appsettings.json:75` holds the key
  `Count1` where the class has `Count`, and nothing notices.
- **A rule on a nested settings class never runs** (`EmailOptions.Smtp` and its `[Required]` members): the helper
  calls `ValidateDataAnnotations()` and no member is marked. A scratch program on .NET 10 showed that the same call
  does check a nested class marked `[ValidateObjectMembers]` and a list marked `[ValidateEnumeratedItems]`, so the
  helper needs no change for it — the settings classes need the markers.
- **Text and composite keys have a form, for rows an aggregate owns:** no id base, the marker `IAssociationEntity`,
  a plain configuration with `HasKey`, loaded through the owner's navigation, no repository of its own. A table
  with a text key that stands alone (this repo's notes, sign-in codes, staged results) has no form there.
- **Every aggregate has the audit columns;** a type without them derives `Entity`, and no configuration ignores
  an audit member. An aggregate that cannot fill them (no user ids) has no form there.
- **No business rule answers with more than one message;** where several things are wrong they are joined into
  one (`Submission.Domain/Behaviors/Article.cs:101`). A list comes back only from input validation.
- **No subsystem is switched by a missing section.** Sections are always present; e-mail is chosen in code by
  which registration is called; the one on-off value (`TransactionOptions.UseSingleTransaction`) is a member of an
  always-present section.
- **Argument errors are thrown only in Domain code,** as guards worded for the caller (about 20 in all). One of
  them passes its sentence as the parameter name (`Production.Domain/Assets/ValueObjects/AssetNumber.cs:15`).

## 14. The owner's rulings on points 2, 3, 5, 6 and 9, and what the audit lookup showed (2026-10-10)

Ruled in this repo's question box the same day (`docs/specs/F194-ReferenceFoundations/delivery/questions.md`, Q22 to
Q28). Two of them change a block; `F201-SharedBase` makes the change in this repo's copy, with a test, and it goes
back with the sync tool.

- **Point 2, text keys — no block change.** A table found by a text value keeps its key and is a plain class with
  its own key configuration. The bases are not widened.
- **Point 3, audit — a block change.** The audit's "who" gets a type of its own, chosen by the application. Found
  in both copies of the blocks: `IAuditedEntity<TPrimaryKey>` declares `TPrimaryKey CreatedById`, so "who" has the
  type of the row's own id; `IAuditableAction` hard-codes `int CreatedById`; the three steps that put the caller's
  id on a command (`AssignUserIdBehavior`, `AssignUserIdFilter`, `AssignUserIdPreProcessor`) read the claim as an
  `int`. It works in the reference only because rows and users both have `int` ids. In this repo a caller's id is
  text (an e-mail, or `platform:{number}`), and rows have `Guid` ids.
- **Point 5, argument errors — a block change.** The handler's line `ArgumentException => BadRequest` is removed:
  an argument exception is a fault in the code, answered 500 with the fixed sentence and logged. Consequence for
  the reference, not edited from here: its domain guards that throw an argument exception for user input (about
  20) would answer 500 until they raise a business error.
- **Point 6, several findings — no block change.** A refusal with several findings is a failed validation.
- **Point 9, a part that may be off — no block change.** An always-present section with an `Enabled` member; a
  value needed only when on is checked by a validator class.
- **Point 9, a nested check — no block change.** The framework's markers on the nested member.

Other things the audit lookup showed in the reference, for whoever owns it (reported by a read-only lookup, not
re-checked line by line):
- Nothing in the data layer fills the audit fields; the domain copies them from the command.
- A message consumer builds its command by hand: `Review…ArticleApprovedForReviewConsumer.cs:46` sets the creator
  and leaves the time at its default; `Production…ArticleAcceptedForProductionConsumer.cs:45` falls back to 0.
- Two places put a person's id where a user's id belongs, one with a comment that says so
  (`Review…AcceptInvitationCommandHandler.cs:58`, `Review.Domain/Assets/Behaviors/Asset.cs:67`).
- A row made with no user has 0 as its creator; there is no named system value.
- The blocks repo's copy of the audit configuration no longer sets the database default for the created time.
