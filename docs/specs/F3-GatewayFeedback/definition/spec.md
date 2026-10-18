# F3 — The blocks' answer to knowledge-gateway's findings

**Traces to:** none (no product doc) — source is knowledge-gateway's findings of 2026-10-10, copied unedited to
`definition/source-feedback.md` (points 1–14 below refer to its numbered sections), the owner's rulings of
2026-10-10 in knowledge-gateway's question box (its F194 `questions.md` Q22–Q28), and the owner's rulings of
2026-10-11 in this session (§ The owner's rulings of 2026-10-11)
**Source:** Scratch (technical feature; architect-authored tech-spec)
**Dependencies:** F1-VersionOne (done)
**Status:** Ready — written 2026-10-11; round 1 of the review folded the same day, round 2 folded the same day (no
finding above MEDIUM; no third round; `delivery/review-critic.md`)
**Model:** claude-opus-5-5
**Plan:** `docs/specs/F3-GatewayFeedback/delivery/plan.md`
**Ideas:** `definition/ideas.md`
Rule list: not applicable — infrastructure fixes to existing blocks, each pinned by its own test; no business-rule
unit is created or changed

---

## Purpose

Knowledge-gateway read version one's blocks against its own services (streamed replies; `Guid`, text and composite
keys; several services in one host; a live service with existing tables) and found where a block cannot do what it
needs. Its own shared-base feature (`F201-SharedBase`, Ready, not built) planned to fix seven of them in its copy and
send them back. The owner ruled that **the fixes are made here first**: this repo is the blocks' one home, a change
sent back by the plain two-way copy reaches it unreviewed, and a fix made here reaches every app. Knowledge-gateway's
feature then takes the blocks forward instead of patching its copy.

## The owner's rulings of 2026-10-11

Given in this session's question box and replies; recorded here as their one file.

1. The seven block fixes are made in this repo first.
2. The gRPC client registration moves to its own block, as long as the move is clean (C9).
3. A failed validation's `errors` is a per-field map, with each writer's outer fields kept (C7).
4. A duplicate value at save answers already-exists (C12). First dropped on the claim that only a race reaches it;
   re-asked when the spec review found the blocks check only a numeric id, and added back.

Added 2026-10-13, at the plan review (question box). The plan review found that the endpoint library lowers only the
first letter of a nested field name (`address.StreetName`), so C7's "the same key conversion the endpoint-library
set-up applies" could not hold for every nested rule as written.

5. Both writers lower the first letter of every part of a nested field name: the endpoint library's
   `address.StreetName` becomes `address.streetName` (C7, C8).
6. The duplicate-value tests use each provider's own error type; the PostgreSQL library's licence is allowed in the
   licence test, for tests only (C12).

## What changes

Each change below is pinned by a test in this repo. Nothing else changes a block's shape or behaviour.

| # | Point | Change | Ruled by |
|---|---|---|---|
| C1 | 1 | Domain events are dispatched on an asynchronous save from every aggregate, whatever its key type — `Guid`, `int` through the generic base, and the tenant aggregate included. Today only aggregates that carry the non-generic `int` marker are selected, so a `Guid`-keyed aggregate's events are silently never sent. A synchronous save keeps today's behaviour: it dispatches nothing. | answered from the code |
| C2 | 3 | The audit fields' "who" has a type of its own, chosen by the application, where today it has the type of the row's own id. The audited bases take the who-type as a second type parameter; their one-parameter forms keep today's meaning **exactly** — the who-type equals the key type, and the last-modified-by member stays nullable with a nullable column. It reaches every place that carries a user's id as a number today: the audited-entity interface and the aggregate base, the audited-command interface, the claims reader and its web implementation, the three steps that put the caller's id on a command (endpoint filter, endpoint-library pre-processor, mediator behaviour), and the audit configuration. An app class that implements the claims reader, and an enum-typed audited command, compile unchanged. | owner, 2026-10-10 (knowledge-gateway Q22–Q28) |
| C3 | 4 | **A reply that has already started is never written to.** Three cases: (a) the client cancelled the request — left alone and not logged, as today (version one's ruling D1); (b) any other error — logged (Error at 500 and above, Warning below 500), then the connection is aborted, so the client sees a cut stream and never a clean end; (c) a reply not yet started — unchanged. Today only case (a) checks, and any other error after the first streamed byte makes the handler itself throw. | answered from the code; (b)'s abort, architect (§ Decisions) |
| C4 | 5 | An argument error is a fault in the code: answered 500, with the fixed sentence outside development, and logged. The line that maps it to 400 is removed. | owner, 2026-10-10 (knowledge-gateway Q22–Q28) |
| C5 | 8 | The design-time factory can build a context derived from the blocks' own base context, whose constructor also takes the memory cache. A factory that today overrides only the provider set-up compiles and works unchanged. | answered from the code |
| C6 | 10 | The stored-JSON helpers — the two converter builders and the conversion helpers that install them — take the serializer's options, and the conversion helpers install a value comparer, so a replaced element and a changed member of an element are both saved. Calls written as today, a method group included, compile and serialize as today. | answered from the code |
| C7 | 7 | **One shape for a failed validation's `errors`.** The error handler writes `errors` as a per-field map, `{ "field": ["message", …] }`, with the **same key conversion the endpoint-library set-up applies** (camelCase under the default JSON policy, nested names included), so the same failure gives the same keys from both writers. Several failures on one field share one array, in order. The outer fields and the message stay as each writer writes them today. | owner, 2026-10-11 |
| C8 | 7 | The endpoint-library set-up is split: the validation reply builder and the enum-as-text converter can each be set on their own, without the forced `api` route prefix. `UseCustomFastEndpoints` stays and combines them with the prefix, behaving as today except that a nested field name has every part lower-cased (ruling 5). | answered from the code; ruling 5 |
| C9 | 12 | **A new block, `Blocks.Grpc`.** The two files of the gRPC client registration — four public types: the registration extensions and the three settings classes — move out of `Blocks.AspNetCore` into it, under the namespace `Blocks.Grpc`. `Blocks.AspNetCore` no longer references the two gRPC packages. The protobuf-net pins in the central package file are marked for `Blocks.Grpc` alone, so the package report gives them to an app that takes it. The registration behaves as before the move, the setting that accepts any server certificate in a debug build included. | owner, 2026-10-11 |
| C10 | 12 | An extension that adds the error handler to a host (`app.Use…`), and an asynchronous form of the migrate helper beside the synchronous one. | answered from the code |
| C11 | 9, 12 | **Every read-me a change makes false is corrected**, and `Blocks.Grpc` gets its own: AspNetCore (argument errors, the validation `errors` shape, the `Use…` extension, gRPC gone), FastEndpoints (the split set-up; the pins now come with `Blocks.Grpc`), Domain and MediatR (the who-type), EntityFrameworkCore (the app adds its database provider and design package; the async migrate; C12), Core (only the helper that runs attribute checks is listed as checking at start-up; the other two bind without attribute checks but still refuse a missing section; a nested settings class or a list is checked only when the member carries `[ValidateObjectMembers]` or `[ValidateEnumeratedItems]`), and the root read-me's block table. The `Blocks.Grpc` read-me shows its settings read through the checking helper. | answered from the code and the 2026-10-10 lookup |
| C12 | 11 | **A duplicate value at save answers already-exists.** When a save fails because the database refused a duplicate unique value — SQL Server errors 2601 and 2627, PostgreSQL code 23505 — the blocks raise the conflict error, answered 409 with a fixed message that does not carry the database's text. The blocks recognise the error without referencing either database's library. Any other database, and any other save failure, keeps today's behaviour. | owner, 2026-10-11 |

## Rules

1. Every change C1–C12 has a test that fails without it. A change to existing behaviour that has no test today gets
   a test of today's behaviour first (C8's combined set-up, C9's registration).
2. Every public name added, moved or changed is a row in `docs/public-names.md`; its header counts and its table of
   the error reply on the wire are updated. The reference's surface dump
   (`docs/specs/F1-VersionOne/delivery/surface-reference.txt`) is the comparison baseline and stays untouched: a fresh
   dump of the blocks is compared with it, and every difference belongs to exactly one row.
3. Code written against version one keeps compiling from source, except where the change is the point: C7 changes the
   validation reply's `errors` shape, and C9 moves four types to a new block and namespace. C3 (b), C4 and C12 change
   what a reply says, as listed.
4. No app repo is edited (knowledge-gateway, the reference, sprint-rituals): what they need is written in
   § Answers for knowledge-gateway.

## Acceptance Criteria

- [ ] The solution builds on .NET 10 with zero warnings and no warning suppressions, and every test passes.
- [ ] C1: on an asynchronous save, a test publishes the pending event of a `Guid`-keyed aggregate, of an
      `AggregateRoot<int>` without the non-generic marker, and of a tenant aggregate; a synchronous save publishes
      none.
- [ ] C2: a test aggregate with a `Guid` id and a text who-type saves its audit fields; a one-parameter aggregate
      written as in version one compiles, saves, and keeps a nullable last-modified-by member and column; each of the
      three steps puts a text caller id on a command in a test. A compile-only test project holds, unchanged,
      version-one consumer code: a one-parameter aggregate and its audit configuration, a class implementing the
      claims reader, an enum-typed audited command, and a design-time factory overriding only the provider set-up.
- [ ] C3: tests throw after the reply has started — (a) a client cancellation: nothing logged, the handler does not
      throw; (b) a not-found error and an unexpected error: each logged at its level, the connection aborted, the
      status and body already sent unchanged; (c) the existing not-started tests pass unchanged, except the two assertions C4 and C7 change on purpose (an
      argument error answers 500, not 400; validation `errors` is a per-field map, not an array), which are updated.
- [ ] C4: grep finds no `ArgumentException =>` mapping in `GlobalExceptionMiddleware.cs`; a test gets 500, the
      fixed sentence outside development, and a logged error for an argument error.
- [ ] C5: a test builds, through the design-time factory, a context derived from the blocks' base context.
- [ ] C6: through the public conversion helpers, a test writes and reads camelCase JSON given camelCase options, and
      default calls still write today's JSON; a test replaces an element and another changes a member of an element
      of a stored list, and each change is read back through a fresh context.
- [ ] C7: one fixture of failures — two on one field, one on another, one on a nested name — goes through both
      writers; both give identical `errors` maps with camelCase keys and the messages in order, and each writer's
      outer fields and message are as before.
- [ ] C8: tests of today's combined set-up (route prefix, enum as text, validation reply with flat field names) are written before the
      split and pass unchanged after it; separate tests set the reply builder alone on a set-up with no `api`
      prefix, and the enum converter alone (the endpoint library's set-up is process-wide, so these run apart from the hosted tests).
- [ ] C9: `src/Blocks.Grpc` exists with a read-me; grep finds no `Grpc` package reference in
      `src/Blocks.AspNetCore/Blocks.AspNetCore.csproj` and no `Grpc` folder under `src/Blocks.AspNetCore`; a test of the
      registration (written before the move, passing after it) resolves a client for a configured service and refuses
      a missing one; the package report for `Blocks.Grpc` alone lists both protobuf-net pins, and for
      `Blocks.AspNetCore` alone lists neither; the portability check and the hygiene tests cover 12 blocks.
- [ ] C10: a test sends a failing request through a host that added the error handler with the new extension and
      gets the mapped reply; a test applies a migration with the asynchronous helper and finds it in the migrations
      history; the synchronous helper stays.
- [ ] C11: each read-me listed in C11 states the changed behaviour; grep finds no "argument" in the 400 line of the
      AspNetCore read-me and no `Blocks.AspNetCore` in the FastEndpoints read-me's pin line.
- [ ] C12: a test against SQL Server and one against PostgreSQL — or, where a database cannot run in the test
      suite, against a constructed error of that provider's type and code — gets the conflict error from a
      duplicate save, and another save failure still gives a 500.
- [ ] `docs/public-names.md` has a row for every public name C1–C12 adds, moves or changes; a fresh surface dump
      compared with the baseline leaves no line without a row; the baseline file is unchanged.

## Answers for knowledge-gateway

Written here; carried over by the owner. Point numbers are those of `definition/source-feedback.md`.

- **Where the fixes are made (its F201 § What changes, item 1):** here first (owner, 2026-10-11). F201 takes the
  blocks forward with the sync tool instead of fixing its copy; its spec line "Fixed in this repo's copy" changes
  in knowledge-gateway.
- **Points 1, 3, 4, 5, 8, 10, 11:** made here as C1, C2, C3, C4, C5, C6, C12, in the form F201 states, except
  C3 (b): an error after the reply started is logged and the connection is aborted.
- **Point 4, and F201's "a request the client drops half-way":** a client-cancelled request already answers 499
  when the reply has not started, and is left alone and unlogged when it has (version one's ruling D1; C3 (a)).
- **Point 7:** the reply builder can be set without the prefix (C8). The reference writes both shapes; the per-field
  map the endpoint-library set-up writes becomes the one shape for `errors`, and the error handler writes it with
  the same keys (C7).
- **Points 2, 6, 9:** no block change, as ruled on 2026-10-10. Nested settings checks need the framework's markers
  on the member (C11). The repository's `…OrThrow…` helpers stay `int`-keyed; a `Guid`-keyed aggregate has none —
  not asked for, so not changed.
- **Point 12:** gRPC is now its own block (C9) — a host that takes `Blocks.AspNetCore` no longer pulls it in. No
  database provider and no design package in the blocks, and the seeding helpers stay SQL Server-only: version
  one's ruling D2 (2026-10-08), a block names no database. The `Use…` extension and the asynchronous migrate are
  added (C10). The settings name clash is a naming-rule question, not a block one.
- **Point 13:** this repo is the master copy (F1's purpose, 2026-10-08). The `//insight` markers and the
  created-date default were removed on purpose in version one (F1 acceptance criteria; ruling D2).
- **Point 14, the reference's own audit and consumer findings:** not this repo's; they stay with the reference.
- **For every app, on its next take-forward:** an argument error now answers 500 (C4) — this includes the blocks'
  own guard helpers in Core, so an app that uses them to check user input answers 500 until it raises a business
  error; a validation reply's `errors` changes shape where the error handler wrote it, and a nested field name has every part lower-cased in both writers (C7, ruling 5); a duplicate unique value
  answers 409 (C12).
- **For the reference project in particular:** its three gRPC services add `Blocks.Grpc` and change their `using`
  to `Blocks.Grpc` (C9); its domain guards that throw an argument error for user input (about 20) answer 500 until
  they raise a business error (C4).

## Plugin note owed at close

The `nexus-dotnet` error-handling skill teaches the flat validation list (`List<ValidationError>` of property and
message) and maps an argument error to 400. After C4 and C7 the blocks answer 500 and write the per-field map. Filed
at close as a plugin-feedback entry in `docs/plugin-feedback/`.

## Decisions

| Decision | Why | Rejected alternative | Status |
|---|---|---|---|
| Errors below 500 stay unlogged when the reply has not started; logged at Warning once it has, a client cancellation excepted (C3 (a)) | not started, they are the caller's mistakes and the caller is told; started, the caller cannot be told, so the log is the only trace | log every mapped 4xx; log nothing below 500 | decided |
| An error after the reply started, other than a client cancellation, aborts the connection after logging | a clean end would make a cut-off answer look complete; the framework's own error handlers end the same way | end the reply normally; rethrow | decided |
| A synchronous save keeps dispatching no events (C1) | today's behaviour; the blocks' interceptors are asynchronous, and the gap reported is the key type | dispatch on synchronous saves too | decided |
| The debug-build accept-any-certificate setting moves to `Blocks.Grpc` unchanged | C9 is a move; changing what it does is a separate decision | fix it during the move | decided |
| C7 changes only `errors`; each writer keeps its outer fields and message | the owner picked the per-field map with today's outer fields | one shared reply object for both writers | decided |
| C12's message is fixed and does not carry the database's text | the database's message names tables and constraints | pass the database's message through | decided |

## Out of Scope

- **BUG-1 and BUG-2** — their own backlog rows.
- **A reviewed path for changes from apps** — F2-ReviewedChangeRequests.
- **A base for a stand-alone table with a text key, an aggregate base without audit, several findings in one
  business error, a switchable settings section** — ruled no block change (2026-10-10).
- **Duplicate-key recognition for databases other than SQL Server and PostgreSQL.**
- **`Guid`-keyed forms of the repository's `…OrThrow…` helpers.**
- **Editing knowledge-gateway, the reference or sprint-rituals.**

## Handed to the plan

- C1: how aggregates of any key type are selected (today's filter is the non-generic `int` marker; the generic
  aggregate interface has a value-type constraint and C2 adds a second type parameter).
- C2: names and signatures. Making the non-generic audited-command interface generic in the who-type collides with
  the existing `IAuditableAction<TActionType>`, so the plan settles the name. The who-type's nullability for the
  last-modified-by member; default implementations for any member added to the claims reader; how each of the three
  steps is registered with a who-type (the mediator behaviour is registered as an open generic of two parameters).
- C3: the abort call and how the started-reply tests observe it.
- C5: the mechanism (a virtual creation hook, or construction through the service provider).
- C6: the comparer, and how options reach the converter builders without breaking a method-group use (overloads
  rather than optional parameters).
- C7 and C8: how both writers share one key conversion.
- C10: the extension's name; the asynchronous migrate's name, its cancellation token and its scope handling.
- C12: where in the save path the error is recognised (the save interceptor, the repository's save, or the
  transaction helper), and how the provider's error type and code are read without a package reference.
- The hygiene tests' fixed block lists and the hygiene test project's project references (the portability check
  already reads the block folders).
- A test project for `Blocks.MediatR` (none exists; C2's mediator step needs one) and the compile-only consumer
  project (C2).
- Each new or moved public name — a `docs/public-names.md` row.
