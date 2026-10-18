# F3 — The blocks' answer to knowledge-gateway's findings

**Feature Spec:** `docs/specs/F3-GatewayFeedback/definition/spec.md`
**Model:** claude-opus-5-5
**Build slices:** 1–4 · 5–7 · 8–11; why: the aggregate and audit chain (the compatibility project first), then the
web-side replies and the gRPC move, then the data-access additions and the two repo-wide documents that need
everything built

## Context

Knowledge-gateway's findings against version one's blocks, answered here first (spec § The owner's rulings of
2026-10-11). Twelve changes, C1–C12 (spec § What changes); the spec's Answers section goes to knowledge-gateway by the
owner and is not this plan's work. No architecture doc, no KB, no conventions folder. The one earlier plan is
F1-VersionOne's (format followed). Grep of `GatewayFeedback`/`F3` across `docs/`: only this feature's own files and
its backlog row.

**The working tree is shared.** At plan time (2026-10-13) another session's uncommitted work sits in this checkout:
`.gitignore` (`.vs/`, two `.claude/` lines), `[Trait("Speed","Slow")]` lines in eight test files
(`Hygiene.Tests/PackageTests.cs`, `Portability.Tests/PortabilityTests.cs`, `SurfaceDump.Tests/SurfaceDumperTests.cs`,
six `Sync.Tests` files), `Hygiene.Tests/RepoFiles.cs` (skips `.vs`), `docs/specs/adhoc-DeclareVerifyRoles/`, and the
untracked `.claude/verify.json` that declares this repo's test roles. One step edits one of those files: step 9 adds a
licence to `PackageTests.cs` (owner, 2026-10-13). The developer leaves the other session's lines as they are; the
close stages F3's files by path, and for `PackageTests.cs` stages only F3's hunk (`git diff` of the file, cut to
that hunk, applied with `git apply --cached`) unless the other session has committed by then.

**Owner rulings of 2026-10-13** (plan review, question box): (1) both writers lower the first letter of every part
of a nested field name, so the endpoint library's `address.StreetName` becomes `address.streetName`; (2) the
duplicate-value tests use the PostgreSQL library's own error type, and its licence is allowed in the licence test.

Plan-time facts (greps and one probe run 2026-10-13):

- Events are selected by `.OfType<IAggregateRoot>()` at `src/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.DomainEvents.cs:13`;
  `IAggregateRoot : IAggregateRoot<int>` (`src/Blocks.Domain/Entities/AggregateRoot.cs:5`);
  `AggregateTenantEntity : AggregateRoot<int>` without the marker (`Entities/TenantEntity.cs:17`). Only
  `SavedChangesAsync` dispatches, in both interceptors; neither overrides the synchronous `SavedChanges`.
- Who-typed members today: `IAuditedEntity<TPrimaryKey>` (`Entities/IAuditedEntity.cs:9-12`),
  `AggregateRoot<TPrimaryKey>` (`AggregateRoot.cs:22-25`), `IAuditableAction.CreatedById` (`int`,
  `IAuditableAction.cs:5`), `IClaimsProvider.GetUserId/TryGetUserId` (`int`, `Core/Security/IClaimsProvider.cs:9-10`),
  `HttpContextProvider` (`AspNetCore/HttpContextProvider.cs:27-30`), the three steps
  (`AspNetCore/Filters/AssignUserIdFilter.cs`, `FastEndpoints/AssignUserIdPreProcessor.cs`,
  `MediatR/Behaviors/AssignUserIdBehavior.cs`, whose class carries `where TRequest : IAuditableAction`),
  `AuditedEntityConfiguration<T, TKey>` (`EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs:18-36`).
  `Blocks.Domain` references no other block.
- `string` implements `IParsable<string>` (.NET 7 and later), as do `int` and `Guid`.
- The error handler: `ArgumentException => HttpStatusCode.BadRequest` at
  `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs:22`; `HasStarted` is checked only in the
  cancellation branch (`:40-44`); the validation branch writes an array of `{propertyName, errorMessage}`
  (`:88-92`). The two tests C4 and C7 change on purpose: `tests/Blocks.AspNetCore.Tests/GlobalExceptionMiddlewareTests.cs:19`
  (`{ nameof(ArgumentException), 400 }`) and the `ValidationReply_UsesCamelCaseNames` test (`:183-194`, its array
  read at `:189`).
- **Probe (FastEndpoints 8.3.0, scratch app under `D:\temp\f3-probe`, 2026-10-13):** with the default serializer
  policy a validator's failures reach the reply builder named `firstName`, `address.StreetName` (from
  `RuleFor(x => x.Address.StreetName)`) and `items[0].streetName` (from `RuleForEach(...).ChildRules(...)`). The
  library lower-cases only the first letter of a nested path. (The probe's second endpoint never reached its
  `AddError` call — the validator refused the request first — so it shows nothing about `AddError`; review R1-09.)
- **The endpoint library's set-up is process-wide** (review R1-01, reproduced by the second reader): its options sit
  in static fields, so the first host to start in a test process fixes the route prefix, converters and reply
  builder for every later host in that process.
- The three steps' `int` rules today: the filter sets the id only when it is unset (`AssignUserIdFilter.cs:13`); the
  behaviour overwrites it whenever a claim is present (`AssignUserIdBehavior.cs:14-18`); the pre-processor always
  overwrites it and throws when the claim is missing (`AssignUserIdPreProcessor.cs:14`).
- `HasJsonListConversion<T>(this ComplexTypePropertyBuilder<T>)` (`BuilderExtensions.cs:72-73`) passes a converter for
  `IReadOnlyList<T>` to a property of type `T`; EF refuses the model (`Converter for model type
  'IReadOnlyList<IReadOnlyList<string>>' cannot be used for … 'IReadOnlyList<string>'`, second reader's probe). It
  has never worked.
- `RepositoryBase<TContext, TEntity, TKey>.SaveChangesAsync` (`Repositories/Repository.cs:93-94`) saves any
  `DbContext`; the EF tests' repositories run on a plain `DbContext`.
- Licences: Microsoft.Data.SqlClient is MIT; Npgsql carries the `PostgreSQL` licence expression, which
  `tests/Blocks.Hygiene.Tests/PackageTests.cs` does not allow today (`AllowedExpressions`).
- The endpoint set-up: `src/Blocks.FastEndpoints/Extensions.cs` (`UseCustomFastEndpoints`: `api` prefix, `JsonStringEnumConverter`, the
  per-field reply builder). `tests/Blocks.FastEndpoints.Tests` has one file, `DomainEventPublisherTests.cs`; nothing
  tests the set-up.
- gRPC: `src/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs` (lines 1–122, one class, three methods) and
  `Grpc/GrpcServicesOptions.cs` (lines 1–24, three classes), namespace `Blocks.AspNetCore.Grpc`; the package lines
  `Grpc.Net.Client` and `protobuf-net.Grpc` at `src/Blocks.AspNetCore/Blocks.AspNetCore.csproj:14-15`; the pins
  `Blocks="Blocks.AspNetCore;Blocks.FastEndpoints"` at `Directory.Packages.props:43-44`. Nothing else in `src/` uses
  a `Grpc` or `ProtoBuf` namespace.
- Fixed block lists: `tests/Blocks.Hygiene.Tests/NamingTests.cs:11-16` (11 names),
  `tests/Blocks.Hygiene.Tests/ReadMeTests.cs:17` (`HaveCount(11)`), `tests/Blocks.Hygiene.Tests/Blocks.Hygiene.Tests.csproj`
  (11 project references). The portability check reads the `src/Blocks.*` folders
  (`tests/Blocks.Portability.Tests/SnapshotFixture.cs`, `Blocks()`), so it covers a new block with no edit.
- EF Core 10.0.12 has `PropertyBuilder.HasConversion(ValueConverter, ValueComparer)` and the
  `ComplexTypePropertyBuilder` form of the same (checked in the package's XML doc).
- The EF tests run on SQLite (`tests/Blocks.EntityFrameworkCore.Tests/TestDatabase.cs`, `TestDbContext : DbContext`);
  no test context derives from `ApplicationDbContext<T>`.

## Scope

In scope: spec § What changes C1–C12, § Rules 1–4, § Acceptance Criteria, § Plugin note owed at close.
Out of scope: spec § Out of Scope, unchanged; the spec's § Answers for knowledge-gateway (carried by the owner); any
edit to `.claude/verify.json` (Decisions).

Rule list: the spec carries `Rule list: not applicable`; there is no `spec-rules.md` to join.

**Binding names.** Every public type, member and namespace this plan names is binding (a rename is a deviation);
private and internal names, file splits inside a folder and test names are the developer's call. The wire texts
quoted in steps 5, 6 and 9 are binding.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | a test project of version-one consumer code that must keep compiling | gap: no skill for a source-compatibility project |
| 2 | domain-patterns; persistence-patterns | Follow | yes | `IHasDomainEvents`; dispatch selects it | — |
| 3 | domain-patterns; persistence-patterns | Follow | yes | two-type audited entity, aggregate and configuration | — |
| 4 | service-infra-conventions; add-pipeline-behavior | Follow | yes | text user id on the claims reader, the who-typed command, the three steps; new MediatR test project | — |
| 5 | error-handling | Follow | yes | C3 started replies, C4 argument errors, C10 `UseGlobalExceptionMiddleware` | — |
| 6 | error-handling | Follow | yes | C7 shared field-map conversion, C8 split set-up, one set-up per test process | — |
| 7 | create-building-blocks-package; central-package-management | Follow | yes | `Blocks.Grpc`, the move, the pins, the 12-block lists | — |
| 8 | persistence-patterns | Follow | yes | C5 design-time factory, C6 JSON helpers, C10 `MigrateAsync` | — |
| 9 | persistence-patterns; error-handling | Follow | yes | C12 duplicate value → conflict in the base context's and the repository's save; test-only provider packages | — |
| 10 | (none) | — | no | C11 read-mes | gap: no skill for a block read-me |
| 11 | (none) | — | no | Rule 2 public names and the surface comparison | gap: no skill for a public-API diff |
| 12 | improve-skills | Follow | no | the plugin note for nexus-dotnet's error-handling skill (Owner: close) | — |

The error-handling skill teaches an argument error as 400 and the validation `errors` as a flat list; this spec's
C4 and C7 replace both for these blocks. Steps 5–6 follow the skill for everything else, and step 12 files the
correction for the skill.

## Domain Model Changes

New in `Blocks.Domain` (namespace `Blocks.Entities` unless noted): `IHasDomainEvents`; `IAuditedEntity<TPrimaryKey,
TUserId>`, `IAggregateRoot<TPrimaryKey, TUserId>`, `AggregateRoot<TPrimaryKey, TUserId>`; in `Blocks.Domain`
namespace `IAuditableRequest` (a marker), `IAuditableActionBy<TUserId>`, `IAuditableActionBy<TUserId, TActionType>`
and `AuditableActionExtensions`. `IAuditableAction` gains the marker as a base. The one-parameter forms keep their
members and meaning exactly.

## Data Model Changes

None in this repo (no migrations live here). An app on a two-type aggregate maps its who columns with the new
three-type audit configuration.

## Implementation Steps

### Step 1 — Version-one consumer code that must keep compiling

Create `tests/Blocks.Compatibility.Tests/` (net10.0 test project, added to `Blocks.slnx` under `tests`, project
references to `Blocks.Domain`, `Blocks.Core`, `Blocks.EntityFrameworkCore`). It holds, written against the blocks
**as they are now** and never edited by a later step:
- a one-parameter aggregate `Order : AggregateRoot` and an `AggregateRoot<Guid>` one, each with an
  `AuditedEntityConfiguration<…>`, and code that sets `LastModifiedById = null` on both;
- a class implementing `IClaimsProvider` with only today's members;
- an enum-typed command implementing `IAuditableAction<TActionType>`, and one implementing `IAuditableAction` with
  its own `Action` text;
- a `DesignTimeFactoryBase<T>` subclass that overrides only `ConfigureProvider`;
- a class calling `BuildJsonListConvertor<T>` and `BuildJsonReadOnlyListConvertor<T>` as method groups
  (`Func<ValueConverter<…>> f = BuilderExtensions.BuildJsonListConvertor<List<int>>;`);
- a class that implements `IAggregateRoot<int>` itself (deriving `Entity<int>`), with `DomainEvents`,
  `AddDomainEvent` and `ClearDomainEvents` implemented **explicitly** as `IAggregateRoot<int>.…`;
- a context derived from `ApplicationDbContext<T>` and code calling `SaveChangesAsync(false)` and
  `SaveChangesAsync()` on it.

One test news up each type, so the project builds and runs in the suite. Its build is the pin for spec Rule 3.
Record the SHA-256 of every file the step creates in `implementation.md` under step 1.

Accept: `dotnet test tests/Blocks.Compatibility.Tests -warnaserror` passes now, before any other step, and after
every later step; after step 11 every file's SHA-256 equals the one recorded, and the folder holds no other file
(`bin`/`obj` aside).

### Step 2 — Domain events from every aggregate (C1)

Follow domain-patterns and persistence-patterns. Inputs:
- New `IHasDomainEvents` in `src/Blocks.Domain/Entities/` with `DomainEvents`, `AddDomainEvent`,
  `ClearDomainEvents`. `IAggregateRoot<TPrimaryKey>` extends it and **keeps its own three declarations** (as `new`
  members), implementing the `IHasDomainEvents` ones with default bodies that forward to its own — so a class that
  implements `IAggregateRoot<TKey>`'s members explicitly still compiles and is still selected.
- `DispatchDomainEventsAsync` selects `IHasDomainEvents` instead of `IAggregateRoot`
  (`src/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.DomainEvents.cs`). The synchronous save path stays
  without dispatch.

Tests in `tests/Blocks.EntityFrameworkCore.Tests/` (new file), through `DispatchDomainEventsInterceptor` and a
recording `IDomainEventPublisher` on SQLite: an `AggregateRoot<Guid>`, an `AggregateRoot<int>` without the marker, an
`AggregateTenantEntity` and a class implementing `IAggregateRoot<int>`'s event members explicitly each have their
pending event published by `SaveChangesAsync`; a synchronous `SaveChanges` publishes none.

Satisfies: spec Acceptance C1.

### Step 3 — The audit "who" type on entities (C2, first half)

Follow domain-patterns and persistence-patterns. Depends on step 2. Inputs:
- `IAuditedEntity<TPrimaryKey, TUserId>` (`where TPrimaryKey : struct where TUserId : class`): `TUserId CreatedById
  { get; init; }`, `DateTime CreatedOn { get; init; }`, `TUserId? LastModifiedById { get; set; }`, `DateTime?
  LastModifiedOn { get; set; }`.
- `IAggregateRoot<TPrimaryKey, TUserId> : IAuditedEntity<TPrimaryKey, TUserId>, IHasDomainEvents`, and
  `AggregateRoot<TPrimaryKey, TUserId> : Entity<TPrimaryKey>, IAggregateRoot<TPrimaryKey, TUserId>` with the same
  `CreatedOn` default and event list as `AggregateRoot<TPrimaryKey>`, and `CreatedById` initialised `= default!`
  (a non-nullable reference member, so the build has no CS8618). The event code may be shared through a non-public
  helper; no new public base class.
- The who-type is a reference type (text in practice); the two-type forms take no `Guid` or `int` who-type — the
  read-mes say so (step 10).
- `AuditedEntityConfiguration<T, TKey, TUserId>` beside the two-parameter one, the same four mappings
  (`CreatedById` required, `LastModifiedById` optional) and the same `HasConcurrencyToken` switch.
- The one-parameter forms (`IAuditedEntity<TPrimaryKey>`, `AggregateRoot<TPrimaryKey>`, `AggregateRoot`,
  `AuditedEntityConfiguration<T>` and `<T, TKey>`) keep their members, types and nullability exactly.

Tests: an `AggregateRoot<Guid, string>` with its configuration saves and reads back `CreatedById`, `CreatedOn`,
`LastModifiedById`, `LastModifiedOn` on SQLite; the model marks the who column required and the last-modified-by
column optional for both a two-type and a one-parameter aggregate (the one-parameter column stays nullable).

Satisfies: spec Acceptance C2 (entities).

### Step 4 — The audit "who" type on commands and the three steps (C2, second half)

Follow service-infra-conventions (the claims reader) and add-pipeline-behavior (the mediator step). Inputs:
- `src/Blocks.Domain/IAuditableAction.cs`: a marker `IAuditableRequest` (no members); `IAuditableAction` now extends
  it, its members unchanged. Add `IAuditableActionBy<TUserId> : IAuditableRequest` (`where TUserId : class,
  IParsable<TUserId>`): `TUserId? CreatedById { get; set; }`, `bool IsAuthenticated => CreatedById is not null`,
  `DateTime CreatedOn { get; }`, `string Action { get; }`, `string? Comment { get; }`; and
  `IAuditableActionBy<TUserId, TActionType> : IAuditableActionBy<TUserId>` (`where TActionType : Enum`) with
  `ActionType` and `Action => ActionType.ToString()`, as `IAuditableAction<TActionType>` does.
- `AuditableActionExtensions.TryAssignCreatedBy(this object request, string userId, bool keepExisting)` in
  `Blocks.Domain`: when the request implements `IAuditableActionBy<T>` for some `T` — and, with `keepExisting`, its
  `CreatedById` is null — sets it to `T.Parse(userId, CultureInfo.InvariantCulture)` and returns true; otherwise
  false. Per-type lookup cached.
- `IClaimsProvider`: add `string? TryGetUserIdText()` with the default body `TryGetUserId()?.ToString(CultureInfo.InvariantCulture)`
  and `string GetUserIdText()` with the default body `TryGetUserIdText() ?? throw new UnauthorizedAccessException(…)`;
  `HttpContextProvider` overrides `TryGetUserIdText` to return the raw `ClaimTypes.NameIdentifier` value.
- **Each step keeps its own rule, on both paths** (its `int` code unchanged):

  | Step | `int` command (today) | who-typed command (new) |
  |---|---|---|
  | `AssignUserIdFilter` | set only when unset and a claim is present | the same (`keepExisting: true`, `TryGetUserIdText`) |
  | `AssignUserIdBehavior` | overwrite whenever a claim is present | the same (`keepExisting: false`, `TryGetUserIdText`) |
  | `AssignUserIdPreProcessor` | always overwrite; throw when the claim is missing | the same (`keepExisting: false`, `GetUserIdText`) |

  The filter's keep-if-set rule predates F3 and stays (out of scope).
- `AssignUserIdBehavior<TRequest, TResponse>`'s constraint becomes `where TRequest : IAuditableRequest`, so the same
  open-generic registration (`config.AddOpenBehavior(typeof(AssignUserIdBehavior<,>))`) reaches who-typed requests
  and the container still skips every other request (no claims reader needed for them).
- New `tests/Blocks.MediatR.Tests/` (net10.0 test project in `Blocks.slnx`, references `Blocks.MediatR`, with
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for the container, as its peer test projects).

Tests — first the `int` path of each step as it is today (a pre-set id: kept by the filter, replaced by the
behaviour and the pre-processor), then for a who-typed `string` command the same three rules: the filter keeps a
pre-set id and fills an unset one; the behaviour and the pre-processor replace a forged pre-set id with the claim
text; the pre-processor throws when the claim is missing. Filter in `tests/Blocks.AspNetCore.Tests`; pre-processor in
`tests/Blocks.FastEndpoints.Tests`, called directly with a fake pre-processor context (no endpoint host, see step 6);
behaviour in `tests/Blocks.MediatR.Tests` through a real MediatR registration with `AddOpenBehavior`, plus one test
that a request implementing neither audit interface is handled with **no** claims reader registered. One test for
`HttpContextProvider.TryGetUserIdText` (a text claim comes back unchanged), and one for the default body (a claims
provider that implements only today's members gives the number as text).

Satisfies: spec Acceptance C2 (commands and steps).

### Step 5 — The error handler: started replies, argument errors, the `Use…` extension (C3, C4, C10)

Follow error-handling (C4 replaces its argument-error mapping). File:
`src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`.
- C3: when the reply has started, (a) a client cancellation (today's guard) is left alone — not logged, not aborted;
  (b) any other error is logged — `LogError` at a mapped status of 500 and above, `LogWarning` below — then
  `context.Abort()`, and nothing is written; (c) a reply not yet started is unchanged. A validation error after the
  start takes (b) at 400.
- C4: remove the `ArgumentException => HttpStatusCode.BadRequest` arm; an argument error (and its subclasses) takes
  the 500 path.
- C10: `ApplicationBuilderExtensions.UseGlobalExceptionMiddleware(this IApplicationBuilder app)` in
  `src/Blocks.AspNetCore/Middlewares/`, namespace `Blocks.AspNetCore.Middlewares`.

Tests in `tests/Blocks.AspNetCore.Tests/`: extend `ErrorMapperHost` with a request-lifetime feature that records
`Abort()` (and a not-cancelled variant), so a started reply reaches the client as written. (a) cancellation after
start: status and body as written, nothing logged by the handler, `Abort` not called; (b) a `NotFoundException`, an
`InvalidOperationException` and a FluentValidation `ValidationException` after start (the validation branch is its
own catch): `Warning`, `Error` and `Warning` respectively, `Abort` called once, status and body as written, the
handler does not throw; C4: update `GlobalExceptionMiddlewareTests.cs:19` to `500` and add a test that
an argument error answers 500, `message` is `An unexpected error occurred.` outside development and an `Error` entry
is logged; C10: a host built with `app.UseGlobalExceptionMiddleware()` maps a `NotFoundException` to 404. Every other
existing test passes unchanged.

Accept also: `grep -n "ArgumentException =>" src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`
returns nothing.

Satisfies: spec Acceptance C3, C4, C10 (extension).

### Step 6 — One shape for validation errors; the split endpoint set-up (C7, C8)

Follow error-handling (C7 replaces its flat list). Order inside the step: the C8 tests of today's set-up first.

**Test layout — one endpoint-library set-up per test process** (the library's set-up is process-wide, Context).
`tests/Blocks.FastEndpoints.Tests` starts endpoint hosts only through one shared fixture built with
`UseCustomFastEndpoints`; the existing `DomainEventPublisherTests` host moves onto that fixture (its request path
gains the `api/` prefix — its assertions do not change). The two split methods are tested in a new project,
`tests/Blocks.FastEndpoints.SetupTests` (in `Blocks.slnx`, references `Blocks.FastEndpoints`, starts no host), on a
fresh `Config` in one non-parallel collection, each test restoring what it set.

- **C8 tests first**, through the shared fixture against `UseCustomFastEndpoints` as it is now: the route has the
  `api/` prefix; an enum in the reply is text; a failing validator with flat field names (`Name` twice, `Email`)
  answers 400 with `statusCode`, `message` (the single message, or `Validation failed for multiple fields.`) and
  `errors` as a per-field map. They pass before and after the step.
- **Shared conversion:** `ValidationFailureExtensions.ToFieldMap(this IEnumerable<ValidationFailure> failures,
  JsonNamingPolicy? namingPolicy)` in `src/Blocks.AspNetCore/Extensions/` (namespace `Blocks.AspNetCore`), returning
  `Dictionary<string, string[]>`: failures grouped by key in first-seen order, messages in order; the key is the
  property path with the policy applied to each dot-separated name, indexers kept (`Address.Street` →
  `address.street`, `Items[0].StreetName` → `items[0].streetName`); a null policy leaves keys as they are.
- **C7:** the error handler writes `errors` with `ToFieldMap(…, JsonNamingPolicy.CamelCase)`; its outer fields and
  message stay as today (`statusCode`, `message` `One or more validation errors occurred.`, `traceId`, `errors`,
  `details`). Update `ValidationReply_UsesCamelCaseNames` (`GlobalExceptionMiddlewareTests.cs`, the test reading
  `errors` at `:189`) to the map shape.
- **C8:** in `src/Blocks.FastEndpoints/Extensions.cs` add `UseEnumsAsText(this Config config)` and
  `UseFieldMapValidationReply(this Config config)`; the reply builder uses `ToFieldMap` with the library's own
  serializer policy, read inside the builder at reply time (`config.Serializer.Options.PropertyNamingPolicy`).
  `UseCustomFastEndpoints` keeps its signature and calls both after setting the `api` prefix. Its one change of
  output is the owner's 2026-10-13 ruling: a nested field name now has every part lower-cased.

Tests: the C8 tests above, unchanged; in `Blocks.FastEndpoints.SetupTests`, `UseFieldMapValidationReply` alone leaves
the route prefix unset and its reply builder, called directly with failures, returns the map with `statusCode` and
`message`; `UseEnumsAsText` alone adds the enum converter and leaves the prefix unset; unit tests of `ToFieldMap` for
the two paths above, a flat name and a null policy; **C7 fixture** in `tests/Blocks.FastEndpoints.Tests/` —
failures `Name` (two messages), `Email`, `Address.Street` — sent through the error handler (a host with the handler
whose endpoint throws `ValidationException` with those failures; this host does not use the endpoint library) and
through a shared-fixture endpoint whose FluentValidation validator raises the same failures (`RuleFor(x =>
x.Address.Street)` for the nested one): both `errors` objects are equal, keys `name`, `email`, `address.street`,
messages in order, and each writer's outer fields are as before.

Satisfies: spec Acceptance C7, C8.

### Step 7 — The new block `Blocks.Grpc` (C9)

Follow create-building-blocks-package and central-package-management. Heavy step. Order inside the step:
1. **Test first, before the move**, in `tests/Blocks.AspNetCore.Tests/`: with a code-first service contract declared
   in the test, each of `AddCodeFirstGrpcClient`, `AddCodeFirstGrpcClientWithRetry` and
   `AddCodeFirstGrpcClientAsSingleton` resolves a client for a configured service key (no call is made), and each
   throws `InvalidOperationException` (`Missing GrpcService config for: …`) for a missing one. Green on the code as it
   is.
2. **Move.** `src/Blocks.Grpc/` (net10.0; package references `Grpc.Net.Client`, `protobuf-net.Grpc`,
   `Microsoft.Extensions.DependencyInjection.Abstractions`; no block reference) with a plain file move of the two
   files into its root (no `git mv` — the developer makes no git write), namespace `Blocks.Grpc`; the only edit to
   either file is its namespace line, the `#if DEBUG` accept-any-certificate setting and the settings classes'
   attributes included. Remove the two gRPC package lines from `src/Blocks.AspNetCore/Blocks.AspNetCore.csproj`.
   Add `src/Blocks.Grpc` and `tests/Blocks.Grpc.Tests` (with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
   for the container) to `Blocks.slnx`; move **item 1's** test there with only its namespace and `using` changed.
3. **Pins:** `Directory.Packages.props` protobuf-net and protobuf-net.Core take `Blocks="Blocks.Grpc"`.
4. **The 12-block lists:** add `Blocks.Grpc` to `NamingTests.BlockAssemblies`, `HaveCount(12)` in `ReadMeTests`, the
   project reference in `Blocks.Hygiene.Tests.csproj`. A `src/Blocks.Grpc/README.md` with the three headings (its
   content is step 10's).
5. New `tests/Blocks.Portability.Tests/PinReportTests.cs` (a new file, beside the other session's edit of
   `PortabilityTests.cs`): through `SyncCli forward --json` on the snapshot, as the portability test does, the
   `missing` list for `Blocks.Grpc` alone holds `protobuf-net` and `protobuf-net.Core`, and for `Blocks.AspNetCore`
   alone holds neither.

Accept: for each file, `git show HEAD:src/Blocks.AspNetCore/Grpc/{file} | diff - src/Blocks.Grpc/{file}` shows the
namespace line and nothing else; `grep -n "Grpc" src/Blocks.AspNetCore/Blocks.AspNetCore.csproj` returns nothing; no `src/Blocks.AspNetCore/Grpc`
folder; the moved test passes; `dotnet test tests/Blocks.Hygiene.Tests` and `dotnet test tests/Blocks.Portability.Tests`
pass with 12 blocks.

Satisfies: spec Acceptance C9.

### Step 8 — Design-time factory, stored-JSON helpers, async migrate (C5, C6, C10)

Follow persistence-patterns. Files: `src/Blocks.EntityFrameworkCore/DbContextDesignTimeFactory.cs`,
`Extensions/BuilderExtensions.cs`, `Extensions/DbContextExtensions.cs`.
- C5: `protected virtual TContext CreateContext(DbContextOptions<TContext> options)` on `DesignTimeFactoryBase<TContext>`,
  called by `CreateDbContext`. Its default looks the constructors up explicitly (not `Activator`, which throws its
  own error first): one taking the options alone, else one taking the options and an `IMemoryCache` (a new
  `MemoryCache`), else an `InvalidOperationException` naming both shapes.
- C6: an overload taking `JsonSerializerOptions options` beside each of `BuildJsonReadOnlyListConvertor<T>()`,
  `BuildJsonListConvertor<TCollection>()`, `HasJsonReadOnlyListConversion<T>`, `HasJsonCollectionConversion<T>` and
  both `HasJsonListConversion` forms; the existing signatures stay as they are (no optional parameter, so method
  groups compile). New `BuildJsonValueComparer<T>()` and `BuildJsonValueComparer<T>(JsonSerializerOptions options)`:
  equal when the serialized text is equal, hashed and snapshotted through the serialized text. Every `Has…Conversion`
  helper, old and new, installs that comparer with its converter. The calls without options keep today's JSON.
- The complex-type `HasJsonListConversion<T>(this ComplexTypePropertyBuilder<T>)` is repaired: its converter and
  comparer are built for `T` itself (as `HasJsonCollectionConversion<T>` does), so EF accepts the model. Signature
  unchanged; it never worked, so no caller depends on the old converter.
- C10: `MigrateAsync<TDbContext>(this IHost app, CancellationToken ct = default)` returning `Task<IHost>`, through
  an async scope, beside `Migrate<TDbContext>`.

Tests in `tests/Blocks.EntityFrameworkCore.Tests/`: C5 — a test context derived from `ApplicationDbContext<T>` is built
by a factory overriding only `ConfigureProvider` (SQLite), the connection string given as a command-line argument
(`--ConnectionStrings:Database=…`); C6 — with camelCase options the stored column text is camelCase and reads back;
without options the stored text equals `JsonSerializer.Serialize(value)`; replacing one element of a stored list and
changing a member of one element are each saved and read back through a fresh context; a complex-type property
mapped with the repaired helper, with and without options, builds its model and round-trips; C10 — a hand-written
migration applied by `MigrateAsync` on a SQLite host is listed by `GetAppliedMigrationsAsync`, and `Migrate` still
works.

Satisfies: spec Acceptance C5, C6, C10 (migrate).

### Step 9 — A duplicate value at save answers already-exists (C12)

Follow persistence-patterns and error-handling. Files: `src/Blocks.EntityFrameworkCore/ApplicationDbContext.cs`,
`Repositories/Repository.cs`, an internal recogniser in the block (a static class of extension methods is named
`…Extensions`, as the naming test requires), `Directory.Packages.props`, the EF test project, and
`tests/Blocks.Hygiene.Tests/PackageTests.cs`.
- **Two save paths translate**, through the one recogniser: `ApplicationDbContext<TDbContext>` overrides
  `SaveChanges(bool acceptAllChangesOnSuccess)` and `SaveChangesAsync(bool acceptAllChangesOnSuccess,
  CancellationToken cancellationToken = default)` (the token keeps its default); and
  `RepositoryBase<TContext, TEntity, TKey>.SaveChangesAsync` translates for a repository on any context. A
  `DbUpdateException` whose inner chain holds a duplicate-value error becomes
  `new ConflictException("A record with the same unique value already exists.", ex)`; every other exception passes
  unchanged. A context not built on the base and saved without a repository is unchanged.
- **Recognised**, with no package reference in the block: an exception whose type's full name is
  `Microsoft.Data.SqlClient.SqlException` or `System.Data.SqlClient.SqlException` with `Number` 2601 or 2627 (read
  by reflection), and one whose type's full name is `Npgsql.PostgresException` with `SqlState` `23505`. No other type
  is recognised, whatever its code.
- **Test-only packages** (owner, 2026-10-13): `Microsoft.Data.SqlClient` and `Npgsql`, latest stable at build time,
  versions in `Directory.Packages.props`, referenced only by `tests/Blocks.EntityFrameworkCore.Tests`. Add
  `"PostgreSQL"` to `PackageTests.AllowedExpressions` (the line beside the other session's uncommitted trait — see
  Context for staging).

Tests in `tests/Blocks.EntityFrameworkCore.Tests/` (SQLite), the save made to fail by a test interceptor throwing a
`DbUpdateException` that wraps the providers' **own** error types: a `Microsoft.Data.SqlClient.SqlException` built
through the library's non-public factory by reflection, with `Number` 2601 and with 2627; an
`Npgsql.PostgresException` from its public constructor with `SqlState` `23505`. Through a context derived from
`ApplicationDbContext<T>` (sync and async saves) and through a repository on the plain test context, each throws
`ConflictException` with the fixed message and no database text. Unchanged: `SqlException` `Number` 547 and an
`Npgsql.PostgresException` with another code still throw `DbUpdateException`; a `DbException` of another type
reporting `SqlState` `23505` still throws `DbUpdateException`; a real SQLite unique violation still throws
`DbUpdateException`. **The replies, end to end:** one test host (TestServer, the error handler added with
`UseGlobalExceptionMiddleware`; the EF test project takes a reference to `Blocks.AspNetCore`, the ASP.NET Core
framework reference and the test-host package) whose endpoint saves through the base context and through a
repository: a duplicate (`Number` 2627) answers 409 with `message` `A record with the same unique value already
exists.` and no provider text in the body; a `Number` 547 failure answers 500 with the fixed sentence. The licence test (`EveryResolvedPackage_CarriesAnAllowedLicence`, slow) passes with the two packages.

Satisfies: spec Acceptance C12.

### Step 10 — Read-mes (C11)

No skill (gap). Correct every read-me a change makes false, and write `src/Blocks.Grpc/README.md`. The spec's C11
row is the list: AspNetCore (argument errors are 500, the `errors` map with every part of a nested name
lower-cased, `UseGlobalExceptionMiddleware`, gRPC gone, the registration snippet without `AddCodeFirstGrpcClient`);
FastEndpoints (the two split methods; nested field names now lower-case every part; no protobuf-net pin);
Domain and MediatR (the two-type aggregate with a reference who-type only, `IAuditableActionBy<TUserId>`, each
step's rule on both paths); EntityFrameworkCore (the app adds its provider and design package; `MigrateAsync`; C12
through the base context and the repository, SQL Server and PostgreSQL only);
Core (only `AddAndValidateOptions` runs attribute checks at start-up; `ConfigureOptionsFromSection` and
`GetSectionByTypeName` bind without them but refuse a missing section; a nested class or a list is checked only when
its member carries `[ValidateObjectMembers]` or `[ValidateEnumeratedItems]`); the root read-me's block table gains
`Blocks.Grpc` and AspNetCore's row loses gRPC. `Blocks.Grpc`'s read-me reads its settings with
`GetSectionByTypeName<GrpcServicesOptions>()` for the client, and says plainly that the service URL and retry
bounds are not checked at start-up: their attributes sit on nested classes with no nested-check marker, and the move
leaves the classes as they are.

**Binding statements.** Each read-me carries, word for word (backticks as shown), the statement below; the
developer writes the rest of the text around it.

| Read-me | Must hold | Must no longer hold (today's text) |
|---|---|---|
| AspNetCore | `An argument error is a fault in the code and answers 500.` · `A nested field name has every part lower-cased: Address.Street becomes address.street.` · `UseGlobalExceptionMiddleware` | `400 for validation, argument,` · `AddCodeFirstGrpcClient` |
| FastEndpoints | `UseEnumsAsText` · `UseFieldMapValidationReply` · `A nested field name has every part lower-cased: Address.Street becomes address.street.` | `protobuf` |
| Domain | `The who-type of the two-type forms is a reference type, such as string.` · `IAuditableActionBy` | — |
| MediatR | `The behaviour replaces the caller's id whenever a claim is present, for int and who-typed commands alike.` | — |
| EntityFrameworkCore | `Add your own database provider and design package.` · `MigrateAsync` · `A duplicate unique value on SQL Server or PostgreSQL answers 409` | — |
| Core | `Only AddAndValidateOptions checks attributes at start-up.` · `ValidateObjectMembers` · `ValidateEnumeratedItems` | `Options binding that fails at start-up: \`AddAndValidateOptions<T>\`, \`ConfigureOptionsFromSection<T>\`` |
| Grpc | `GetSectionByTypeName` · `The service URL and the retry bounds are not checked at start-up.` | — |
| root | `` `Blocks.Grpc` `` | `gRPC client registration` |

Accept: a new `ReadMeTests` fact reads each read-me with line breaks and runs of spaces folded to one space (as
`TheRootReadMe_SaysHowToRunTheSyncTool` does) and asserts every "must hold" and every "must no longer hold" cell of
the table; `dotnet test tests/Blocks.Hygiene.Tests` passes.

Satisfies: spec Acceptance C11.

### Step 11 — Public names and the surface comparison (Rule 2)

No skill (gap). Build `Blocks.slnx` in Release, run `tools/Blocks.SurfaceDump` on the 12 block assemblies to a file
under the OS temp folder, diff it against `docs/specs/F1-VersionOne/delivery/surface-reference.txt` (never edited),
and update `docs/public-names.md`: a `## Blocks.Grpc` section; a row for every name C1–C12 adds, moves or changes
(new types and members, the four moved gRPC types, the overloads, the behaviour's changed constraint, the `IAuditableRequest` base on `IAuditableAction`, the two
`ApplicationDbContext` overrides, the factory hook, the claims-reader members); the header's two line counts from the
new diff; the error-reply-on-the-wire table (argument error 500, `errors` as a per-field map with every part of a
nested name lower-cased in both writers, 409 for a duplicate value).

Accept: every line of the new diff belongs to exactly one row (each row lists the dump lines it covers, as F1's
did); `git diff --quiet docs/specs/F1-VersionOne/delivery/surface-reference.txt` exits 0.

Satisfies: spec Rule 2 and its acceptance line.

### Step 12 — The plugin note (Owner: close)

Follow improve-skills (the feedback-file channel). At close, the main session appends an entry to
`docs/plugin-feedback/nexus-dotnet-{that plugin's version at close}-{date}.md` (2.0.3 at plan time) for the
`error-handling` skill: it teaches the flat validation list (`List<ValidationError>` of property and message) and an
argument error as 400; after C4 and C7 these blocks answer 500 and write the per-field map, with every part of a
nested name lower-cased. Committed by path with F3's files.

Satisfies: spec § Plugin note owed at close.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Version-one consumer code compiles: one-parameter aggregates and configurations, a claims-reader class, both audited-command forms, a provider-only factory, method-group converter calls, explicit event-member implementations, `SaveChangesAsync(false)` on a base-context type | the step-1 project; its files' SHA-256 recorded at step 1 and unchanged after step 11 | new-test (step 1) |
| Each step's `int` rule for the caller's id (filter keeps a set id; behaviour and pre-processor replace it) | step 4's `int`-path tests, written first | new-test (step 4) |
| A mediator request that is not auditable runs with no claims reader registered | step 4 test | new-test (step 4) |
| A one-parameter aggregate's last-modified-by stays nullable, member and column | step 3 model test; existing `AuditedEntityConfigurationTests` | new-test (step 3) |
| A synchronous save dispatches no events | step 2 test | new-test (step 2) |
| A reply not yet started answers as today, except the two intended changes | every existing `GlobalExceptionMiddlewareTests` case, `:19` and the `errors` test updated on purpose | pinned |
| A client cancellation is never logged and answers 499 when not started | existing `Cancellation_Answers499`, `CancellationWrappedInAnotherError_Answers499`; step 5 (a) | pinned |
| `UseCustomFastEndpoints` behaves as today for flat names (nested names change by the owner's 2026-10-13 ruling) | step 6 tests written before the split | new-test (step 6) |
| gRPC registration behaves as before the move, the debug certificate setting included | step 7 test written before the move; `git diff -M` shows namespace lines only | new-test (step 7) |
| The JSON helpers' calls without options write today's JSON | step 8 test | new-test (step 8) |
| Any save failure other than a recognised duplicate is unchanged, another database's 23505 included | step 9 tests (547, another PostgreSQL code, another type with 23505, SQLite) | new-test (step 9) |
| The reference surface baseline is untouched | `git diff --quiet` in step 11 | pinned |
| No app repo is edited | no step names a path outside this repo | accepted-gap (nothing to run; the close's `git status` shows this repo only) |

## Testing Strategy

Each step's tests are written first (TDD) except steps 1, 10, 11, 12; steps 6 and 7 write their characterization
tests before changing code. Step closes run the touched test projects with `-warnaserror`; the repo's `unit` role in
`.claude/verify.json` lists four test projects, so the new `Blocks.Compatibility.Tests`, `Blocks.MediatR.Tests`,
`Blocks.FastEndpoints.SetupTests` and `Blocks.Grpc.Tests` run by path (`verify-run.js --profile fast --files …`) and in the `full` role
(`dotnet test Blocks.slnx -warnaserror`). The handover run is the `full` role: zero warnings, every test green.

## KB Impact

None (no `docs/kb/`).

## Decisions

| Decision | Why | Rejected alternative | Status |
|---|---|---|---|
| Events are selected through a new `IHasDomainEvents` that the aggregate interfaces extend | one test covers every key type and the two-type aggregate | reflection over `IAggregateRoot<>`; a selection per key type | decided |
| `IAggregateRoot<TPrimaryKey>` keeps its three event members and forwards `IHasDomainEvents`'s to them by default bodies (review R1-05) | an explicit implementation of the old members still compiles and is still dispatched (spec Rule 3) | move the members up (breaks explicit implementations) | decided |
| The two-type forms take a reference who-type (`where TUserId : class`); the read-mes say so | `TUserId?` then means nullable for the last-modified-by member; a value who-type would silently become required | an unconstrained who-type; a second family for value who-types (additive later) | decided |
| The who-typed command interface is named `IAuditableActionBy<TUserId>` (and `<TUserId, TActionType>`) | `IAuditableAction<T>` already exists with the action type, so a who-typed `IAuditableAction<T>` cannot | `IUserAuditableAction<T>`; changing the existing interface | decided |
| The who-typed command requires `IParsable<TUserId>` and the steps parse the claim text | one registration of each step serves `int` and text commands; the mediator step stays an open generic of two parameters | a generic step per who-type; text-only commands | decided |
| The claims reader gains text members with default bodies built on today's `int` members | an app's own claims reader compiles and gives consistent answers | abstract members; a second claims interface | decided |
| The mediator step's constraint becomes a new marker, `IAuditableRequest`, that both audited-command families extend (review R1-03, R1-04) | the open-generic registration reaches who-typed requests, the container still skips every other request (no claims reader needed), and the constraint keeps `notnull` | no constraint (CS8714, and every request then needs a claims reader); a second behaviour class | decided |
| Each step applies its existing `int` rule to who-typed commands too (review R1-02) | a caller-supplied id is replaced where it is replaced today; the filter's keep-if-set rule is older than F3 | keep-if-set everywhere for the new path (lets a forged id through the behaviour and the pre-processor) | decided |
| Endpoint-library hosts in one test process share one set-up; the split methods are tested on a `Config` in their own project (review R1-01) | the library's set-up is process-wide, so two set-ups in one process make tests order-dependent | several hosts with different set-ups in one project | decided |
| A duplicate value is recognised in the base context's save and in the repository's save (review R1-06) | together they cover every save through a blocks context or a blocks repository, after every interceptor's failure handling, so the transaction rollback still runs; a plain context saved directly is unchanged | a failure interceptor (throwing there skips later interceptors, the rollback included); the base context alone (misses repositories on a plain context) | decided |
| The complex-type JSON helper is repaired rather than left out (review R1-11) | it has never built a model, so the repair breaks no caller and lets "every helper installs the comparer" hold | leave it broken and out of the comparer list | decided |
| `Blocks.Grpc`'s settings classes move unchanged, and its read-me says their nested values are not checked at start-up (review R1-15) | spec C9 makes the move a move; adding the nested-check markers changes start-up behaviour | add the markers during the move | decided |
| The duplicate recogniser is internal | the spec asks for the behaviour, not a helper; adding it later is additive | a public `DbUpdateException` extension | decided |
| `.claude/verify.json` is not edited; the new test projects run by path and in the full suite | the file is another session's uncommitted work in this checkout | add the four projects to its `unit` role | decided |

## Open Questions

None.

## Plan Review

Critic review, owner-picked (2026-10-13); reader pair Codex + Sonnet. Record: `delivery/review-critic.md` (plan
rounds) and `delivery/review-critic-codex.md`. Round 1: Codex NO-GO, Sonnet REVISE; 21 merged findings (7 HIGH, 8
MEDIUM, 6 LOW), all folded; two put to the owner first (spec rulings 5 and 6).
Round 2 (Codex, delta read): 19 of 21 closed, two MEDIUM partly closed (C12's replies end to end; the read-me
checks), both folded. No CRITICAL, so no round 3: the review is complete.
