# F1 — Version One of the generic building blocks

**Feature Spec:** `docs/specs/F1-VersionOne/definition/spec.md`
**Model:** claude-opus-5-5
**Build slices:** 1–4 · 5–9 · 10–13; why: the move and clean-up (with the surface tool), then the behaviour fixes with their tests, then the sync tool and the repo-wide checks that use it

## Context

New, empty public repo (`origin` = `https://github.com/DotNetLabX/dotnet-building-blocks`; one commit: README,
LICENSE, .gitignore). It becomes the one home of the generic blocks. Source: the reference
`D:\src\dotnet-microservices\src\BuildingBlocks` at commit `fda2eb7`. The reference, `D:\src\sprint-rituals` and
`D:\src\knowledge-gateway` are read-only: never build, write or run anything inside them, `bin`/`obj` included —
copy files out first. No architecture doc, no KB, no other plan; `docs/backlog.md` has F1 (this) and F2
(follow-up). Machine: .NET SDK 10.0.401; PowerShell 7 is not installed, so every tool is C#.

Plan-time facts (greps and trial builds run 2026-10-08; the trial builds by the plan's second reviewer, on scratch
copies):

| Block | .cs files | lines | Blocks it references |
|---|---|---|---|
| Exceptions | 5 | 61 | — |
| Domain | 15 | 248 | — |
| Http.Abstractions | 1 | 19 | — |
| Core | 32 | 773 | Exceptions |
| Redis | 3 | 114 | Exceptions |
| AspNetCore | 13 | 584 | Domain, Exceptions, Core |
| EntityFrameworkCore | 26 | 992 | Domain, Exceptions, Core |
| MediatR | 6 | 113 | Core, Domain |
| Messaging | 3 | 78 | Core |
| Hasura | 9 | 484 | Core |
| FastEndpoints | 3 | 68 | AspNetCore, Domain |

- Byte-order mark: 115 of the 116 `.cs` files in the 11 blocks, and every project file.
- Comment markers: `//insight` 10 lines in 8 files, `//talk` 2, `//todo` 1, `///` 30 lines in 8 files; commented-out
  code at least at EF `Repositories/Repository.cs:66–67`, `Core/Security/JwtOptions.cs:14`,
  `Hasura/HasuraRegistration.cs:53`.
- The reference builds on net9 with 22 nullable warnings in 8 files (Core `ThreadSafeMemoryCache.cs` 8,
  `EnumExtensions.cs` 3, `ReflectionExtensions.cs` 3, `ObjectExtensions.cs` 2, `JsonExtensions.cs` 1; AspNetCore
  `HttpContextProvider.cs` 2, `ModelBinding/GenericModelBinderProvider.cs` 1; EF `TenantRepositoryBase.cs` 2). On
  net10 (EF 10.0.8, FastEndpoints 8.1.0, MediatR 12.5.0, MassTransit 8.5.10) it compiles with 0 errors and adds
  `CS0618` at `EntityTypeBuilderExtensions.cs:35` and `EF1002` at `Seeding/ManualGenerateIdScope.cs:24,33`.
- With transitive pinning off, restore resolves protobuf-net 2.4.8 and System.ServiceModel.Primitives 4.5.3; the
  reference pins protobuf-net 3.2.56, protobuf-net.Core 3.2.56, System.ServiceModel.Primitives 8.1.2.
- `Microsoft.AspNetCore.Http.Abstractions` has no 10.x release (last 2.3.0).

## Scope

In scope: the spec § What version one contains, § The sync script, § Rules 1–11, § Acceptance Criteria,
§ Public names. Out of scope: the spec's § Out of Scope, unchanged.

Rule list: the spec carries `Rule list: not landed` (owner skipped it); no `spec-rules.md` to join.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | central-package-management | Follow | no | root central file with pinning on, solution, editor and git attributes, foreign-repo snapshot | — |
| 2 | framework-currency; central-package-management | Follow | no | 11 blocks to `src/`, net10.0, version lines, warning fixes, surface tool and reference baseline | — |
| 3 | (none) | — | no | the rename table in step 3 | gap: no skill for a namespace/public-rename pass |
| 4 | (none) | — | no | comment rules A11, B1, B2 | gap: no skill for a comment-convention sweep |
| 5 | error-handling | Follow | yes | 403/409/502 types, mapper changes D1 | — |
| 6 | persistence-patterns | Follow | yes | D2, D3, A4, the conflict helper | — |
| 7 | domain-patterns | Follow | yes | D4 runtime-type publish on FastEndpoints 8.x | — |
| 8 | service-infra-conventions | Follow | yes | A2 claims provider fix | — |
| 9 | (none) | — | no | 11 block read-mes | gap: no skill for a block read-me |
| 10 | (none) | — | yes | the sync tool, spec § The sync script | gap: no skill for a file-sync tool |
| 11 | (none) | — | no | portability check | gap: no skill for a copy-and-build check |
| 12 | (none) | — | no | public-surface comparison and the names list | gap: no skill for a public-API diff |
| 13 | (none) | — | no | repo-wide hygiene tests | gap: no skill for repo hygiene gates |

`create-building-blocks-package` is not mapped: it scaffolds a *new* block; here all 11 already exist and are moved.

## Domain Model Changes

None to shapes (the 16 kept conflicts stay as the reference has them). Behaviour fixes only: D3 upsert, A4
delete-by-id (step 6), D4 publish (step 7).

## Data Model Changes

None. D2 removes a SQL default from the audited-entity table set-up; no migrations live in this repo.

## Implementation Steps

### Step 1 — Repo skeleton and the foreign-repo snapshot

Follow central-package-management. Create in `D:\src\dotnet-building-blocks\`:
- `Blocks.slnx` (solution folders `src`, `tests`, `tools`).
- `Directory.Packages.props` — central versions, `CentralPackageTransitivePinningEnabled` **true** (as the
  reference). Each pin of a package no block references directly carries the item metadata `Blocks="{Block};…"`
  naming the blocks whose dependency graph needs it (the sync tool reads it, step 10). No `NoWarn` anywhere.
- `Directory.Build.props` — this repo's build only: `TreatWarningsAsErrors` true, `Deterministic` true. Nothing a
  block needs to build lives here (spec item 2).
- `.editorconfig` (`charset = utf-8`, `end_of_line = lf`), `.gitattributes` (`* text=auto eol=lf`).
- `.gitignore`: add `.claude/.current-agent`, `.claude/audit/`, `.claude/.pipeline-state`.
- `docs/specs/F1-VersionOne/delivery/foreign-repos-before.txt`: for the reference, sprint-rituals and
  knowledge-gateway, `git -C {repo} rev-parse HEAD`; the full output of
  `git --no-optional-locks -C {repo} status --porcelain --ignored`; the SHA-256 of
  `git --no-optional-locks -C {repo} diff HEAD --binary` (content of every changed tracked file); the SHA-256 of
  each untracked file's content; and, for each ignored entry, its last-write time. Written before any other step.

Accept: the snapshot file exists with three sections; the props files are evaluated by step 2's build.

### Step 2 — Move the 11 blocks onto .NET 10; the surface tool and the reference baseline

Follow framework-currency and central-package-management. Heavy step.

1. **Surface tool first.** `tools/Blocks.SurfaceDump/` (net10.0 console): given assemblies, prints every public and
   protected type and member — namespace, kind, modifiers (`sealed`, `abstract`, `static`, `virtual`), full
   signature with generic constraints and nullable annotations — sorted, one per line. Fixture tests in
   `tests/Blocks.SurfaceDump.Tests/`: a renamed method, a removed member, an added member, a moved namespace, a
   `string` → `string?` return, and a class made `sealed` each change the dump.
2. **Reference baseline.** Copy the reference's tracked block files (`git -C D:\src\dotnet-microservices ls-files
   src/BuildingBlocks/Blocks.*` plus `src/Directory.Packages.props`) into a new folder under the OS temp directory,
   build it there as it is (net9), run the surface tool on its output, and save
   `docs/specs/F1-VersionOne/delivery/surface-reference.txt`. Nothing runs inside the reference repo.
3. **Move.** Copy the same files to `src/Blocks.{Name}/`, add each to `Blocks.slnx`; strip every leading byte-order
   mark; target `net10.0`; versions only in the central file.
   - `Microsoft.*` and EF Core → latest 10.0.x; FastEndpoints, FastEndpoints.Messaging.Core → 8.x; MediatR →
     **12.5.0** exactly; MediatR.Contracts 2.0.1; MassTransit.RabbitMQ → latest 8.x; FluentValidation (and
     DependencyInjectionExtensions) → latest 12.x; EFCore.NamingConventions → 10.x; every other third-party package
     → the reference's major line, newer only where .NET 10 restore requires it. Pins carried over from the
     reference (protobuf-net, protobuf-net.Core, System.ServiceModel.Primitives) on their reference major lines.
   - `Blocks.Http.Abstractions`: replace the `Microsoft.AspNetCore.Http.Abstractions` 2.3.0 package with
     `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (no 10.x package exists).
   - Drop package references no block code uses (Decisions): `Microsoft.AspNetCore.Authentication.JwtBearer`,
     `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore` (AspNetCore), `FastEndpoints.Swagger`
     (FastEndpoints). Re-grep each before dropping; keep any the build proves used.
4. **Warning fixes — the only other edits allowed in this step**, each listed in `implementation.md` with file and
   line: framework moves (FastEndpoints 7→8 `Send.*` per framework-currency; EF 9→10, the `CS0618` at
   `EntityTypeBuilderExtensions.cs:35` by the non-obsolete call); nullable warnings by a correct annotation or a
   null check — an annotation change on a public or protected member is allowed and reaches the names list in step
   12; `EF1002` in `ManualGenerateIdScope.cs` by building the statement from the model's delimited table name
   (`ISqlGenerationHelper.DelimitIdentifier`) into a plain string passed to the raw-SQL call — no interpolated
   string, no suppression.

Accept: `dotnet build Blocks.slnx` with zero warnings; `dotnet test tests/Blocks.SurfaceDump.Tests` green;
`surface-reference.txt` exists; `dotnet list Blocks.slnx package --include-transitive` shows MediatR 12.5.0,
MassTransit 8.x and protobuf-net 3.x.

### Step 3 — Names: namespaces, convention renames, product wording

No skill (gap). Namespace rule (spec item 3): each folder takes the namespace most of its files use, the folder
path on a tie. Apply exactly this table (from the plan-time per-folder scan) and update every reference in the repo:

| Where | Old | New |
|---|---|---|
| `src/Blocks.FastEndpoints/AssignUserIdPreProcessor.cs` | namespace `Blocks.FastEnpoints` | `Blocks.FastEndpoints` |
| `src/Blocks.MediatR/Behaviors/*.cs` (3 files) | `Blocks.MediatR.Behaviours` | `Blocks.MediatR.Behaviors` |
| `src/Blocks.AspNetCore/Middlewares/` GlobalException, RequestDiagnostics (a 1–1–1 tie) | `Blocks.AspNetCore`, `Blocks.AspNetCore.Middleware` | `Blocks.AspNetCore.Middlewares` |
| `src/Blocks.EntityFrameworkCore/Interceptors/TransactionalDispatchDomainEventsInterceptor.cs` (tie) | `Blocks.EntityFrameworkCore` | `Blocks.EntityFrameworkCore.Interceptors` |
| `src/Blocks.Core/Extensions/` Assembly, Regex, Type (8 siblings in `Blocks.Core`) | `Blocks.Core.Extensions` | `Blocks.Core` |
| `src/Blocks.Core/Cache/` IThreadSafeMemoryCache, ThreadSafeMemoryCache (a 2–2 tie) | `Blocks.Core` | `Blocks.Core.Cache` |
| `src/Blocks.Domain/Entities/IAuditedEntity.cs` (6 siblings in `Blocks.Entities`) | `Blocks.Domain.Entities` | `Blocks.Entities` |
| `src/Blocks.Domain/IDomainObject.cs` (4 siblings in `Blocks.Domain`) | `Blocks.Entities` | `Blocks.Domain` |
| `src/Blocks.Exceptions/Extensions.cs` (4 siblings in `Blocks.Exceptions`) | `Blocks.Linq` | `Blocks.Exceptions` |
| `src/Blocks.Core/Extensions/RegexExtension.cs` | class `RegexExtension` | `RegexExtensions` (file renamed too) |
| `src/Blocks.EntityFrameworkCore/Transactions/TransactionProvider.cs:9` | `GetCurrentTransaction` | `GetCurrentTransactionAsync` |
| `src/Blocks.Hasura/HasuraMetadataService.cs:114,133` | `TrackObjectRelationship`, `TrackArrayRelationship` | `…Async` |
| `src/Blocks.Redis/Extensions.cs:21,24,28` | `GenerateNewId`, `SetSequenceSeed`, `SeedFromJson` | `…Async` |
| `src/Blocks.Redis/Repository.cs:26,52,53` | `Exists`, `GenerateNewId` (2 overloads) | `ExistsAsync`, `GenerateNewIdAsync` |
| `src/Blocks.AspNetCore/HttpContextProvider.cs:12,48–49` | `IRouteProvider.GetArticleId()` and its implementation | removed |
| `src/Blocks.FastEndpoints/AssignUserIdPreProcessor.cs:11,14` | local `articleCommand` | `command` |
| `src/Blocks.Redis/Repository.cs:45` | comment "Sections in Journal" | reworded without product words |

The Async rows are the complete result of the plan-time scan of task-returning methods (12 hits: these 9 plus 3
framework-fixed `Handle`); no private or internal method needs a rename. The gates are step 13's hygiene tests
(namespace per folder, Async names, product words), run at the end of this step too.

### Step 4 — Comments to the conventions

No skill (gap). In every `src/` file: remove `//insight`, `//talk`, `//todo` markers and commented-out code (B1, B2);
remove `///` documentation except where a member's behaviour cannot be read from its signature (A11); where an
`//insight` carried a reason the code does not show, keep the reason as one plain line. No code change.

Accept: before and after the step, build `src/` with `Deterministic=true` and `DebugType=none`; every block's DLL is
byte-identical (a comment-only edit cannot change IL). Every remaining `///` block is listed in `implementation.md`
with its reason. Step 13's marker and commented-out-code tests pass.

### Step 5 — Error types and the error mapper (D1, A1)

Follow error-handling. TDD.
- `src/Blocks.Exceptions/`: `ForbiddenException` (403), `ConflictException` (409), `BadGatewayException` (502), each in
  the reference's subclass shape (`HttpException` with `HttpStatusCode`, inner-exception constructor).
- `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`: change **only** D1's listed behaviours — the
  three statuses; any other `HttpException` → its own `StatusCode`; a 500 outside development → the fixed text
  "An unexpected error occurred."; camelCase reply names; 499 only while `!Response.HasStarted`, also when an inner
  exception is the cancellation. Keep the reference's class shape (`sealed`, `IWebHostEnvironment`), its log
  threshold (500 and up) and its development `Details` (stack trace) on every reply, validation included.
  Sprint-rituals' `Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` is a pattern for the inner-cause
  check only (`IsCausedByCancellation`).
- Tests in `tests/Blocks.AspNetCore.Tests/` (xUnit v3 + AwesomeAssertions, `Microsoft.AspNetCore.TestHost`; the test
  project setup as sprint-rituals' tests): status per type (400 validation, argument, bad-request and domain; 401;
  403; 404; 409; 499; 500; 502); an unlisted `HttpException` subclass → its own code; wrapped cancellation → 499;
  cancellation after the response started → status unchanged, no second exception; a 500 outside development has
  the fixed text and not the thrown message; in development a reply carries `details`; reply names `statusCode`,
  `message`, `traceId`, `details`, and in a validation reply `errors[].propertyName`, `errors[].errorMessage`.

Satisfies: spec AC "Error mapper tests" (except the conflict helper, step 6).

### Step 6 — EF Core fixes (D2, D3, A4) and the conflict helper

Follow persistence-patterns. TDD, each test red on the old code first. Tests in
`tests/Blocks.EntityFrameworkCore.Tests/` on SQLite in-memory (one open connection per test).
- Conflict helper: `src/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` `EnsureNotExistsOrThrowAsync`
  throws `ConflictException`. Test: it throws that type for an existing id.
- D2: `EntityConfigurations/AuditedEntityConfiguration.cs` — remove the `GETUTCDATE()` default and the protected
  virtual `DefaultDateSql`. Test: the model of an audited test entity has no default SQL on `CreatedOn`.
- D3: `Repositories/Repository.cs` `UpsertAsync` copies `Entry(entity).CurrentValues` (pattern: sprint-rituals
  `Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs` `UpsertAsync`, that line only). Test: an existing
  entity with a property mapped to a private field (`PropertyAccessMode.Field`) is updated through upsert; a fresh
  context reads the new value.
- A4: `DeleteByIdAsync` builds the statement from the model's delimited table and key column names
  (`ISqlGenerationHelper.DelimitIdentifier`) into a plain string and passes the id as a parameter to the raw-SQL
  call — no interpolated table name, no suppression. Keep its return value and signature. Test: delete an entity
  by id; it is gone and the call returns true.

Satisfies: spec AC "updates an existing entity…", "deletes an entity by id", "no database-specific default", and
the error-mapper bullet's conflict-helper case.

### Step 7 — FastEndpoints publisher (D4)

Follow domain-patterns. TDD. `src/Blocks.FastEndpoints/DomainEventPublisher.cs`: publish through the non-generic
`IEvent` overload so dispatch uses the runtime type (pattern: sprint-rituals `Blocks.FastEndpoints/DomainEventPublisher.cs`,
the cast only); keep the reference's class shape and single-event contract. Test in `tests/Blocks.FastEndpoints.Tests/`:
a concrete event published through `IDomainEventPublisher` reaches its `IEventHandler<TConcrete>`; red first.

Satisfies: spec AC "A FastEndpoints test…".

### Step 8 — Claims provider fix (A2)

Follow service-infra-conventions. TDD. `src/Blocks.AspNetCore/HttpContextProvider.cs` `GetClaimValues(claimName)`
returns the values of `claimName`. Test in `tests/Blocks.AspNetCore.Tests/`: a principal with two role claims and two
claims of another type; each query returns its own values; red first.

Satisfies: spec AC "A claims test…".

### Step 9 — Block read-mes

No skill (gap). `src/Blocks.{Name}/README.md` for all 11, each with the headings `## Purpose`, `## Depends on`
(blocks and packages) and `## Registration`, under ~40 lines. EntityFrameworkCore's names both the manual-id
insert scope and the table reseed as needing SQL Server. Gated by step 13.

### Step 10 — The sync tool and the root read-me

No skill (gap). TDD. `tools/Blocks.Sync/` (net10.0 console):
`dotnet run --project tools/Blocks.Sync -- {forward|back|status} --app <path> [--source <blocks-repo-path>] [--adopt] [--json]`.
`--source` defaults to the checkout the tool runs from. The entry point is a public `SyncCli.Run(args, stdout,
stderr)` returning the exit code; tests call it in-process. Tests in `tests/Blocks.Sync.Tests/` on temporary git
repositories (a fake blocks repo and a fake app per test, under the OS temp directory).

Binding contract (spec § The sync script, § Rules 1–11):
- **Files.** Manifest `blocks.json` at the app root: `source`, `commit`, `blocksFolder`, `packagesFile`, `blocks[]`.
  Lock `blocks.lock.json` beside it: per block, `commit` and `files` (relative path → fingerprint). Package report:
  human text on stdout; with `--json`, one JSON object `{ "missing": [{id, version}], "different": [{id, ours,
  theirs}] }`.
- **File set and fingerprint.** Every file under the block folder except the folders `bin`, `obj`, `.vs`, `.vscode`,
  `.idea` and the files `*.user`, `*.suo`, `.DS_Store`. Fingerprint: SHA-256 of the content with a leading byte-order
  mark removed and CRLF → LF.
- **Sides.** Forward: this repo's side is the manifest commit (`git ls-tree`/`git show`). Back: this repo's working
  tree. Status: the states against the manifest commit, plus two separate marks per block — "newer here" when the
  source's HEAD holds a different version than the manifest commit, and "uncommitted here" when the working tree
  differs from the source's HEAD.
  The dependency closure comes from the block projects' `ProjectReference` lines at the commit read. The package
  report covers the copied blocks' `PackageReference` versions and every pin whose `Blocks` metadata names one of
  them.
- **Containment (Rule 4) — refuse before any write.** A block name must match `^Blocks\.[A-Za-z0-9]+(\.[A-Za-z0-9]+)*$`
  and exist under the source's `src/`. Every path from the manifest or lock is relative, has no `..` segment and no
  root, and resolves inside the app root (lock, blocks folder) or inside the source's `src/` (send-back target). Any
  existing directory on a write path that is a symbolic link or junction is refused. Writes go only to: the app's
  `{blocksFolder}/{block}/`, the app's `blocks.lock.json`, and (back) the source's `src/{block}/`. A file is never
  overwritten in place: it is written to a new temporary file in the same folder and moved over the old name, so a
  hard-linked destination keeps its other names' content unchanged.
- **First take (Rule 7).** An existing block folder with no lock entry is replaced only with `--adopt`, and only if
  `git --no-optional-locks -C <app> status --porcelain --ignored -- <folder>` lists nothing for files in the file set
  (tracked, committed, nothing ignored). The files to remove are printed first.
- **Everything else** as the spec states: the three-way rule, both refusals with block and files, the way-out text,
  dropped blocks, unreadable lock, origin check against `source` (`git remote get-url origin`), "fetch first" on a
  missing commit. Exit codes: 0 done, 1 refused, 2 usage or environment error. Forward writes this repo's bytes as
  stored (LF); back writes without a leading byte-order mark. The tool runs only read-only git commands, always
  with `--no-optional-locks`.
- **Root `README.md`**: what this repo is, the block list, how to write `blocks.json`, the three commands, `--adopt`,
  and the way out of a "changed on both sides" refusal.

Tests, one named test per item, listed in `implementation.md`: each Flow (1–3) and each status state; Rules 1–11; both
refusals and the way out followed to the end; first take refused, then adopted, then refused because of an ignored
file; a file added in the app; each excluded folder and file kind ignored; line endings and a byte-order mark ignored;
a dropped block; an unreadable lock; a block name with `..`, a rooted `blocksFolder`, a lock path escaping the app,
and a junction on a write path — each refused with nothing written; a destination file hard-linked to a file outside
the allowed folders — the outside file's content unchanged after a forward run (a before/after listing of the temp tree outside
the allowed folders is equal); the package report including an annotated pin; `--json` output parsed.

Satisfies: spec AC "Sync tests…", the root read-me part of "Every block has a read-me…".

### Step 11 — Portability check

No skill (gap). `tests/Blocks.Portability.Tests/` (trait `Category=Portability`). Once per run, snapshot the
candidate: copy this repo's working-tree `src/` and `Directory.Packages.props` into a new git repo under the OS temp
directory, commit, and set its `origin` to this repo's origin URL. For each of the 11 blocks: a fresh temp app
repo with a manifest naming that block and the snapshot's commit; run `SyncCli.Run` forward with `--source
<snapshot> --json`; write a `Directory.Packages.props` (pinning on) from the report only; add a new class library
referencing the block; `dotnet build` it; assert success and that the copied files equal the snapshot's. Every temp
path is under the OS temp directory; the test asserts it is outside `D:\src`. Needs network restore.

Accept: 11 test cases pass.

Satisfies: spec AC "Portability check".

### Step 12 — Public-surface comparison and the names list

No skill (gap). Run `tools/Blocks.SurfaceDump` on the final build, diff against `surface-reference.txt`, and write
`docs/public-names.md`: every difference grouped by block (`old → new`, added, removed, annotation changed), plus the
error reply's wire names. Every row of the spec's § Public names table must appear in it; a spec row with no matching
difference, or a difference with no explanation in the spec or `implementation.md` (warning fixes, step 2), stops the
step and goes to the architect as a question.

Accept: `docs/public-names.md` exists; every diff line is accounted for; every spec table row is present.

Satisfies: spec AC "`docs/public-names.md` lists every difference…".

### Step 13 — Repo-wide hygiene tests

No skill (gap). `tests/Blocks.Hygiene.Tests/`, reading files as bytes over the whole repo except `bin`, `obj`
and `.git`:
- no file starts with `EF BB BF` (`docs/` included);
- no `//insight`, `//talk`, `//todo` (case-insensitive) and no commented-out code (a `//` line whose text ends in
  `;`, `{` or `}` or starts with a C# statement keyword) in `src/` and `tools/`;
- no `#pragma warning disable`, `SuppressMessage`, `NoWarn` or `WarningsNotAsErrors` anywhere;
- no `article` or `journal` (case-insensitive substring) in `src/`;
- in `src/`, every `.cs` file in a folder declares the same namespace;
- in the loaded block assemblies, no method returning `Task`/`ValueTask` lacks the `Async` suffix, except framework-fixed
  names (`Handle`, `Consume`, `Invoke`, overrides);
- `Directory.Packages.props` and every project file: MediatR exactly 12.5.0; no MediatR ≥ 13, AutoMapper ≥ 15,
  MassTransit* ≥ 9, FluentAssertions ≥ 8;
- licences: for every package the solution resolves (`dotnet list Blocks.slnx package --include-transitive --format json`),
  the `.nuspec` in the NuGet global packages folder carries an SPDX licence expression of MIT, Apache-2.0,
  BSD-2-Clause, BSD-3-Clause or MS-PL, or a licence URL on a reviewed allow list kept in the test file; any other
  licence fails the test with the package named (the architect rules on each before it joins the allow list);
- each of the 11 block read-mes has the three headings; the EF read-me names both SQL Server helpers; the root read-me
  names `blocks.json`, `forward`, `back`, `status`, `--adopt` and the way-out sentence.

Accept: every hygiene test passes; each was seen failing once on a planted violation (listed in `implementation.md`);
the same record as `foreign-repos-before.txt`, taken again for the three read-only repos, equals it.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Shapes of the 16 kept conflicts and every public name outside the list | step 12 diff against the reference's own surface | pinned |
| Step 4 changes comments only | step 4 byte-identical deterministic DLLs | pinned |
| Behaviour of the 16 kept conflicts | no tests in version one (full coverage is version two); the code review reads the diff | accepted-gap (owner's version-one scope) |
| The three read-only repos are untouched | step 13 compare with `foreign-repos-before.txt` (HEAD, status, changed-content hash, untracked-file hashes) | pinned; inside ignored folders only last-write times are compared (their size rules out hashing), so a write there that keeps the time is not seen — accepted-gap |
| A block builds alone in an app | step 11 | pinned |

## Testing Strategy

xUnit v3 + AwesomeAssertions (no commercial test library; setup as sprint-rituals' test projects). Behaviour fixes
red-first. Sync tool on real temporary git repos, no mocks of git. Portability tests are slow (network restore) and
tagged. Hygiene tests run in every build.

## KB Impact

None — the repo has no KB.

## Decisions

| Decision | Why | Rejected alternative | Status |
|---|---|---|---|
| Sync tool and checks in C# | PowerShell 7 is not installed; one language and one test stack | a PowerShell script with Pester | decided |
| Transitive pinning on, pins annotated with the blocks that need them | keeps protobuf-net on 3.x as the reference runs it, and lets the package report name the pins | pinning off (protobuf-net drops to 2.4.8) | decided |
| Drop package references no block code uses | an app would carry JWT, OpenAPI and Swagger packages for nothing | keep the reference's lists | decided |
| `Blocks.Http.Abstractions` uses the ASP.NET Core shared framework | its package has no 10.x release | stay on the 2.3.0 package | decided |
| Namespace direction: the folder's majority, the folder path on a tie | the fewest public types move | the folder path everywhere (moves 6+ entity types) | decided |
| Nullable annotation fixes allowed in step 2 and listed | zero warnings without suppressions is a spec criterion | suppress, or leave warnings | decided |
| Test projects only for the blocks this release changes | the owner chose "risky parts tested"; full coverage is version two | one test project per block now | decided |
| Reference baseline built from a scratch copy under the temp directory | the reference repo stays untouched and the baseline holds no step-2 edits | a baseline from this repo's step-2 build | decided |

## Open Questions

None.

## Plan Review

Critic review, reader pair Codex + Sonnet (`reader-pair.md`); full record in `review-critic.md` (plan rounds 1–2) and
`review-critic-codex.md`. Round 1: 24 merged findings (1 CRITICAL, 7 HIGH, 12 MEDIUM, 4 LOW), all fixed. Round 2
(delta, floor MEDIUM): 1 HIGH, 4 MEDIUM, all fixed; no CRITICAL, so no round 3 — the plan is approved.
