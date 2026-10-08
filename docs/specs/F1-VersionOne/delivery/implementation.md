# F1 — Version One of the generic building blocks — Implementation

**Model:** claude-opus-5-5

## Step 1 — Repo skeleton and the foreign-repo snapshot
**Files:**
- `Blocks.slnx` — new solution, folders `src`, `tests`, `tools`; holds `tools/Blocks.ForeignSnapshot`.
- `Directory.Packages.props` — new, at the repo root; `ManagePackageVersionsCentrally` and `CentralPackageTransitivePinningEnabled` true. No `PackageVersion` items yet: no block exists until step 2, and the `Blocks="…"` pin metadata arrives with the pins.
- `Directory.Build.props` — new; `TreatWarningsAsErrors` and `Deterministic` true, nothing else.
- `.editorconfig` — new; `root = true`, `charset = utf-8`, `end_of_line = lf`.
- `.gitattributes` — new; `* text=auto eol=lf`.
- `.gitignore` — added `.claude/.current-agent`, `.claude/audit/`, `.claude/.pipeline-state` and `docs/specs/F1-VersionOne/delivery/foreign-repos-*.txt`. The working-tree copy had CRLF line endings, so it was rewritten with LF to match `.gitattributes`.
- `tools/Blocks.ForeignSnapshot/` (`Blocks.ForeignSnapshot.csproj`, `Program.cs`, `SnapshotCli.cs`, `SnapshotRecorder.cs`, `Git.cs`) — new net10.0 console with no package references. It runs `record --repo <path> … --out <file>`, and every git call goes through `Git.Bytes`, which always puts `--no-optional-locks` first and keeps stdout only.
  - **Record lines (tab-separated, per repo):** `head`, `status` (one per line of `git status --porcelain --ignored`), `diff <path> <sha256>` (per path of `git diff HEAD --name-only -z`, hashing `git diff HEAD --binary -- <path>`), `untracked <path> <sha256>` (per file of `git ls-files --others --exclude-standard -z`, content read with a shared read-only open), and `ignored <path> <utc>` (per `!!` entry of `status --porcelain --ignored -z`; last-write time of the folder or file).
  - **Format:** each repo opens with `# repo\t<full path>` in argument order; lines inside a repo are sorted ordinal; output is UTF-8 without a byte-order mark, with LF.
- `docs/specs/F1-VersionOne/delivery/foreign-repos-before.txt` — recorded for the reference, sprint-rituals and knowledge-gateway, in that order, before any other step's work. It is git-ignored: on disk only, never committed.

**Skills:** central-package-management

**Decisions:**
- The NUL-separated (`-z`) git forms are used for the path lists so a quoted path can be opened as written. The `status` lines keep the plain porcelain output whole, as planned.
- The repo header uses the full path, so two runs given the same arguments write the same header.

**Deviations:**
- `Directory.Packages.props` sits at the repo root, not at `src/` as the central-package-management skill shows. The plan places it there because `tests/` and `tools/` projects need it too, and steps 10 and 11 use that path.

**Verification:**
- `dotnet build tools/Blocks.ForeignSnapshot`: 0 warnings, 0 errors. This build reads both root props files.
- Two back-to-back `record` runs on the reference alone: `cmp` finds the files identical.
- The record has 639 lines and three `# repo` sections. By kind: head 3, status 277, diff 14 (8 sprint-rituals + 6 knowledge-gateway), untracked 100 (3 / 18 / 79, matching the architect's counts), ignored 242.
- The record has no byte-order mark and no CR.
- `git status` does not list it.

## Step 2 — Move the 11 blocks onto .NET 10; the surface tool and the reference baseline
**Files:**
- `tools/Blocks.SurfaceDump/` — new net10.0 console with one package, `System.Reflection.MetadataLoadContext`.
  - Usage: `Blocks.SurfaceDump <dll>... [--probe <folder>]... [--out <file>]`.
  - It reads assemblies without loading them. Dependencies are resolved from each target's folder, the `--probe` folders, and the running .NET and ASP.NET Core shared frameworks, so a net9 build can be read with only .NET 10 installed.
  - It writes one line per public or protected type and member: `{assembly} | {namespace} | {type} | {kind} {declaration}`, with lines sorted ordinal and LF line endings.
  - A declaration carries the access level; `static`/`abstract`/`virtual`/`override`/`sealed override`; return and parameter types with nullable annotations (`NullabilityInfoContext`); `this`/`params`/`ref`/`out`/`in`; default values; and generic constraints (`class`, `class?`, `struct`, `unmanaged`, `notnull`, types, `new()`).
  - A type's line carries its kind (`class`/`record`/`struct`/`interface`/`enum`/`delegate`, plus `static`/`abstract`/`sealed`), its base type and its interfaces.
  - Files: `Program.cs`, `SurfaceDumper.cs`, `Declarations.cs`, `TypeNames.cs`.
- `tests/Blocks.SurfaceDump.Tests/` — new. xUnit v3 + AwesomeAssertions, set up as in sprint-rituals.
  - `FixtureCompiler` compiles each source with Roslyn into a `Fixture.dll` in its own temp folder.
  - `SurfaceDumperTests` has 9 tests: the same source gives the same dump; a renamed method, a removed member, an added member, a moved namespace, a `string` → `string?` return and a class made `sealed` each change the dump; generic constraints and a `Task<T?>` default parameter appear; internal and private members do not.
- `docs/specs/F1-VersionOne/delivery/surface-reference.txt` — 684 lines.
  - Source: the reference's tracked block files (`git ls-files 'src/BuildingBlocks/Blocks.*' src/Directory.Packages.props`, 128 files) were copied to `%TEMP%\blocks-baseline-fda2eb7`. They were built there as they are (net9) with `-p:CopyLocalLockFileAssemblies=true`, so the package DLLs sit beside each block for the dump.
  - Nothing ran in the reference repo: only `git --no-optional-locks ls-files` and file reads.
- `src/Blocks.*/` (11 blocks, 127 files):
  - Copied from the reference. The leading byte-order mark was stripped from every file, and CRLF was converted to LF to match `.gitattributes`.
  - Project files rewritten: `net10.0`, no versions, references unchanged except:
    - Blocks.AspNetCore drops `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.OpenApi` and `Swashbuckle.AspNetCore`.
    - Blocks.FastEndpoints drops `FastEndpoints.Swagger`.
    - Blocks.Http.Abstractions: the `Microsoft.AspNetCore.Http.Abstractions` package is replaced by `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
  - Before the drops I re-grepped `src` for `JwtBearer|OpenApi|Swashbuckle|Swagger`: no hit. The build passes without them.
- `Directory.Packages.props`:
  - Microsoft.Extensions.* and EF Core 10.0.12 (latest 10.0.x)
  - EFCore.NamingConventions 10.0.1
  - FastEndpoints and FastEndpoints.Messaging.Core 8.3.0 (latest 8.x)
  - MediatR 12.5.0, MediatR.Contracts 2.0.1
  - MassTransit.RabbitMQ 8.5.11 (latest 8.x)
  - FluentValidation and its DependencyInjectionExtensions 12.1.1
  - Kept at the reference's version (restore needed nothing newer): Grpc.Net.Client 2.71.0, protobuf-net.Grpc 1.2.2, GraphQL.Client and its serializer 6.1.0, Humanizer.Core 2.14.1, Mapster 7.4.0, Newtonsoft.Json 13.0.3, Redis.OM 1.0.1, Refit.HttpClientFactory and Refit.Newtonsoft.Json 8.0.0
  - Transitive pins carried over, each with `Blocks="Blocks.AspNetCore;Blocks.FastEndpoints"`: protobuf-net 3.2.56, protobuf-net.Core 3.2.56, System.ServiceModel.Primitives 8.1.2
  - Tools and tests: System.Reflection.MetadataLoadContext 10.0.12; the sprint-rituals test set (xunit.v3 3.2.2, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 17.14.1, AwesomeAssertions 9.0.0); Microsoft.CodeAnalysis.CSharp 5.0.0 for the fixtures
- `Blocks.slnx` — the 11 blocks under `src`, the test project under `tests`, the surface tool under `tools`.
- **Warning fixes.** These are the only code edits in this step; line numbers are as they now stand.
  - `src/Blocks.Core/Cache/ThreadSafeMemoryCache.cs`
    - Lines 12, 20, 32, 44, 52, 59 (CS8601/CS8603, nullable): the annotations now match `IMemoryCache` — `TryGet` has `out T? value`, the four `GetOrCreate` overloads return `T?`, `GetOrCreateAsync` returns `Task<T?>`.
    - Lines 36, 63 (CS8604, null check): the cache key is `typeof(T).FullName ?? typeof(T).Name`.
  - `src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs:7–9` — the same annotations on `TryGet`, `GetOrCreateAsync` and `GetOrCreate(string, Func)`.
  - `src/Blocks.Core/Extensions/EnumExtensions.cs:11–12` (CS8600/CS8604) — null check: `FieldInfo?`, and the attribute is looked up only when the field exists.
  - `src/Blocks.Core/Extensions/JsonExtensions.cs:22` (CS8603) — `DeserializeCaseInsensitive<T>` returns `T?`, as `JsonSerializer.Deserialize` does.
  - `src/Blocks.Core/Extensions/ObjectExtensions.cs:12, 19` (CS8619) — the return types become `Dictionary<string, object?>` and `Dictionary<string, string?>`.
  - `src/Blocks.Core/Extensions/ReflectionExtensions.cs:16` (CS8603 ×3) — `GetValue` returns `object?`.
  - `src/Blocks.Core/Extensions/StringExtensions.cs:36` (CS8604, raised at `HttpContextProvider.cs:49`) — `ToInt(this string? input)`. `int.TryParse` already handles null.
  - `src/Blocks.AspNetCore/HttpContextProvider.cs:11, 45` (CS8603) — `IRouteProvider.GetRouteValue` and `HttpContextProvider.GetRouteValue` return `string?`, as the extension they forward to does.
  - `src/Blocks.AspNetCore/ModelBinding/GenericModelBinderProvider.cs:13` (CS8600) — cast to `IModelBinder?`, which the method already returns. Not a public change.
  - `src/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs:21–25` (CS8602, new on EF 10's annotations) — null check: the seeding log line moved inside `if (data != null)`.
  - `src/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs:36` (CS0618) — `GetQueryFilter()` becomes `FindDeclaredQueryFilter(null)?.Expression`, the anonymous filter, which is what the obsolete call returned.
  - `src/Blocks.EntityFrameworkCore/Repositories/TenantRepositoryBase.cs:19, 27` (CS8603) — `GetById` returns `TEntity?` and `GetAsync` returns `Task<TEntity?>`.
  - `src/Blocks.EntityFrameworkCore/Seeding/ManualGenerateIdScope.cs:1–2, 26–28, 39–40` (EF1002 ×2) — the table name is `GetService<ISqlGenerationHelper>().DelimitIdentifier(table, schema)`. Each statement is a plain `string.Concat` passed to `ExecuteSqlRaw`; `Dispose` uses an early return.
- FastEndpoints 7→8: no `Send*Async` call exists in the blocks (stage-1 grep, no hits), and FastEndpoints 8.3.0 compiled without edits. MediatR 13→12.5.0 compiled without edits.

**Skills:** framework-currency, central-package-management

**Decisions:**
- **FastEndpoints is on 8.3.0, the latest 8.x.** The plan-time trial used 8.1.0; the plan says "8.x" and framework-currency says latest stable.
- **Test packages are pinned to sprint-rituals' versions,** not the newest: xunit.v3 3.2.2 rather than 4.x, Test.Sdk 17.14.1 rather than 18.x. The plan says "setup as sprint-rituals' test projects", and that set is known to work on this machine.
- **The surface line format** is `{assembly} | {namespace} | {type} | {kind} {declaration}`, so step 12 can group by block.
- **Recipe to dump this repo's build in step 12:** build with `-p:CopyLocalLockFileAssemblies=true`, then pass the 11 block DLLs.

**Deviations:**
- The block files are LF in the working tree, not the reference's CRLF. One behaviour-visible effect: the raw SQL string in `src/Blocks.Hasura/HasuraMetadataService.cs:16–26` now has LF line breaks inside the SQL text, which is whitespace only to the database. The repo's `eol=lf` would give the same bytes at checkout anyway.
- The step re-invoked `central-package-management`; the props file stays at the repo root as in step 1.

**Verification:**
- `dotnet build Blocks.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test tests/Blocks.SurfaceDump.Tests`: 9/9 passed.
- `dotnet list Blocks.slnx package --include-transitive` shows:
  - Blocks.MediatR: MediatR 12.5.0
  - Blocks.Messaging: MassTransit 8.5.11 (transitive) and MassTransit.RabbitMQ 8.5.11
  - Blocks.AspNetCore and Blocks.FastEndpoints: protobuf-net 3.2.56 and protobuf-net.Core 3.2.56
- The three-form CPM grep finds no version in any project file, and there is exactly one `Directory.Packages.props`.
- No `NoWarn`, `#pragma warning`, `SuppressMessage` or `WarningsNotAsErrors` anywhere.

## Step 3 — Names: namespaces, convention renames, product wording
**Files:**
- `tests/Blocks.Hygiene.Tests/` — new. xUnit v3 + AwesomeAssertions, under `tests` in `Blocks.slnx`, referencing all 11 block projects.
  - `RepoFiles.cs` finds the repo root (the folder holding `Blocks.slnx`), walks a folder while skipping `bin`, `obj` and `.git`, and reads files as bytes decoded as UTF-8.
  - `NamingTests.EveryFolderInSrc_DeclaresOneNamespace` takes the first `namespace` declaration of every `.cs` under `src/`; files without one, such as `GlobalUsings.cs`, are skipped. It fails on any folder with more than one namespace.
  - `NamingTests.EveryTaskReturningMethodInTheBlocks_EndsInAsync` loads the 11 block assemblies and checks every declared method returning `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`. It exempts `Handle`, `Consume`, `Invoke`, overrides (`GetBaseDefinition() != method`), special names, and compiler-generated methods and types.
  - `ProductWordTests.NoFileInSrc_ContainsAProductWord` flags any line of any `src/` file containing `article` or `journal`, case-insensitive.
  - Each test reports every hit in its failure message, not just the first.
- `src/Blocks.FastEndpoints/AssignUserIdPreProcessor.cs` — namespace `Blocks.FastEnpoints` → `Blocks.FastEndpoints`; the local `articleCommand` → `command`.
- `src/Blocks.MediatR/Behaviors/AssignUserIdBehavior.cs`, `LoggingBehavior.cs`, `ValidationBehavior.cs` — `Blocks.MediatR.Behaviours` → `Blocks.MediatR.Behaviors`.
- `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` (`Blocks.AspNetCore`) and `RequestDiagnosticsMiddleware.cs` (`Blocks.AspNetCore.Middleware`) → `Blocks.AspNetCore.Middlewares`.
- `src/Blocks.EntityFrameworkCore/Interceptors/TransactionalDispatchDomainEventsInterceptor.cs` — `Blocks.EntityFrameworkCore` → `Blocks.EntityFrameworkCore.Interceptors`.
- `src/Blocks.Core/Extensions/AssemblyExtensions.cs`, `TypeExtensions.cs` and `RegexExtensions.cs` — `Blocks.Core.Extensions` → `Blocks.Core`. In the last of these, the class `RegexExtension` → `RegexExtensions` and the file was renamed from `RegexExtension.cs` with `mv`.
- `src/Blocks.Messaging/MassTransit/DependencyInjection.cs` — removed `using Blocks.Core.Extensions;`, a namespace that no longer exists.
- `src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs`, `ThreadSafeMemoryCache.cs` — `Blocks.Core` → `Blocks.Core.Cache`. The consumers already had `using Blocks.Core.Cache;`.
- `src/Blocks.Domain/Entities/IAuditedEntity.cs` — `Blocks.Domain.Entities` → `Blocks.Entities`; dropped its now-redundant `using Blocks.Entities;`.
- `src/Blocks.Domain/Entities/AggregateRoot.cs` — dropped the stale `using Blocks.Domain.Entities;`.
- `src/Blocks.Domain/IDomainObject.cs` — `Blocks.Entities` → `Blocks.Domain`.
- `src/Blocks.Domain/Entities/Entity.cs`, `IAssociationEntity.cs`, `IMetadataEntity.cs`, `src/Blocks.Domain/ValueObjects/ValueObject.cs` — added `using Blocks.Domain;` for `IDomainObject`, which moved.
- `src/Blocks.Exceptions/Extensions.cs` — `Blocks.Linq` → `Blocks.Exceptions`.
- `src/Blocks.EntityFrameworkCore/Transactions/TransactionProvider.cs:9` — `GetCurrentTransaction` → `GetCurrentTransactionAsync`. It had no callers in the repo.
- `src/Blocks.Hasura/HasuraMetadataService.cs:99, 103, 114, 133` — `TrackObjectRelationship` / `TrackArrayRelationship` → `…Async`, at both the declarations and the calls.
- `src/Blocks.Redis/Extensions.cs:21, 24, 28, 45` — `GenerateNewId`, `SetSequenceSeed`, `SeedFromJson` → `…Async`, including the call at line 45.
- `src/Blocks.Redis/Repository.cs`
  - Lines 26, 36, 52, 53: `Exists` → `ExistsAsync`; both `GenerateNewId` overloads → `GenerateNewIdAsync`, including the call at line 36.
  - Line 45: the comment loses "(e.g Sections in Journal)".
- `src/Blocks.AspNetCore/HttpContextProvider.cs` — removed `IRouteProvider.GetArticleId()` and `HttpContextProvider.GetArticleId()`.

**Skills:** tdd (TDD yes per the amended plan); no pattern skill (plan: none, gap)

**Decisions:**
- **Async-name exemptions.** The test applies the plan's list (`Handle`, `Consume`, `Invoke`, overrides). It also skips compiler-generated members (lambdas, local functions, state machines) and special-name accessors, which are not user-named methods.
- **Product-word test scope.** It reads every file under `src/`, not only `.cs`, so project files and read-mes are covered too.

**Deviations:** None.

**Red on the pre-step code** — this is each test's planted violation for step 13's Accept:
- `EveryFolderInSrc_DeclaresOneNamespace`:
  `src/Blocks.AspNetCore/Middlewares: Blocks.AspNetCore, Blocks.AspNetCore.Middleware, Blocks.AspNetCore.Middlewares`;
  `src/Blocks.Core/Cache: Blocks.Core, Blocks.Core.Cache`;
  `src/Blocks.Core/Extensions: Blocks.Core, Blocks.Core.Extensions`;
  `src/Blocks.Domain/Entities: Blocks.Domain.Entities, Blocks.Entities`;
  `src/Blocks.Domain: Blocks.Domain, Blocks.Entities`;
  `src/Blocks.EntityFrameworkCore/Interceptors: Blocks.EntityFrameworkCore, Blocks.EntityFrameworkCore.Interceptors`;
  `src/Blocks.Exceptions: Blocks.Exceptions, Blocks.Linq`;
  `src/Blocks.FastEndpoints: Blocks.FastEndpoints, Blocks.FastEnpoints`.
- `EveryTaskReturningMethodInTheBlocks_EndsInAsync`:
  `Blocks.EntityFrameworkCore.TransactionProvider.GetCurrentTransaction`;
  `Blocks.Hasura.HasuraMetadataService.TrackArrayRelationship`;
  `Blocks.Hasura.HasuraMetadataService.TrackObjectRelationship`;
  `Blocks.Redis.Extensions.GenerateNewId`; `Blocks.Redis.Extensions.SeedFromJson`;
  `Blocks.Redis.Extensions.SetSequenceSeed`;
  ``Blocks.Redis.Repository`1.Exists``; ``Blocks.Redis.Repository`1.GenerateNewId`` (both overloads).
  That is all 9 rows of the plan's scan.
- `NoFileInSrc_ContainsAProductWord`: `src/Blocks.Redis/Repository.cs:45` (Journal); `src/Blocks.FastEndpoints/AssignUserIdPreProcessor.cs:11, 14` (`articleCommand`); `src/Blocks.AspNetCore/HttpContextProvider.cs:12, 48, 49` (`GetArticleId`, `"articleId"`).

**Verification:**
- After the renames, `dotnet test tests/Blocks.Hygiene.Tests`: 3/3 passed.
- `dotnet build Blocks.slnx --no-incremental`: 0 warnings, 0 errors.

## Step 4 — Comments to the conventions
**Files:**
- `tests/Blocks.Hygiene.Tests/CommentTests.cs` — new, with two tests over every `.cs` in `src/` and `tools/`:
  - `NoSourceFile_CarriesACourseNoteOrToDoMarker` matches `//\s*(insight|talk|todo)\b`, case-insensitive, so the reference's spaced form `// insight - …` is caught too.
  - `NoSourceFile_CarriesCommentedOutCode` checks every line that starts with `//` but not `///`. It flags the line when the comment text ends in `;`, `{` or `}`, or starts with a C# statement keyword (`if else for foreach while do switch case return var throw try catch finally using break continue goto yield await lock`).
- Markers removed. Where an `//insight` carried a reason the code does not show, that reason was kept as one plain line:
  - `src/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs` — line 41 becomes `// Retries run on the gRPC channel (MethodConfig RetryPolicy): a Polly handler at the HttpClient layer never sees RpcException.`; line 94 becomes `// One channel and client pair is shared for the whole process instead of one per scope.`. "Deliberately not wired by any service" was dropped: it describes the reference app, not the block.
  - `src/Blocks.Domain/Entities/AggregateRoot.cs` — the two-line audit insight becomes `// Only aggregates carry audit fields: other entities are saved as part of an aggregate and share its audit values.`; the `//talk` on immutable collections is removed.
  - `src/Blocks.EntityFrameworkCore/Repositories/Repository.cs` (`DeleteByIdAsync`) — the two insights and the two commented-out lines become `// Raw SQL: marking a stub entity as Deleted is impossible when the entity has required properties.`
  - Removed outright, as course notes with no reason the code needs:
    - `src/Blocks.AspNetCore/HttpContextProvider.cs` (`//insight - Solid Princile interface segregation`)
    - `src/Blocks.Core/GuardExtensions.cs`
    - `src/Blocks.Domain/IAuditableAction.cs`
    - `src/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` (one `talk`, one `insight`)
    - `src/Blocks.Redis/Entity.cs`
  - `src/Blocks.Core/Security/JwtOptions.cs` — the `//todo` and the commented-out `SigningCredentials` property are removed.
- Commented-out code removed:
  - `src/Blocks.Core/Cache/ICacheable.cs` (`//string CacheKey { get; }`)
  - `src/Blocks.EntityFrameworkCore/TenantDbContext.cs` (`//this.ChangeTracker.DetectChanges();`)
  - `src/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.DomainEvents.cs` — two fluent-chain lines, `//.Entries<IAggregateRoot>()…` and `//.Select(x => (IEvent)x) …`. The test's rule cannot see these; they were found by reading.
  - `src/Blocks.Hasura/HasuraRegistration.cs` — the `Authorization` header line, and the 6-line `RefitSettings` block (3 of its lines end in `,` or `(` and are invisible to the rule).
  - `src/Blocks.AspNetCore/Middlewares/RequestContextMiddleware.cs:50` — the trailing `// or Guid.NewGuid().ToString();`.
- Also removed:
  - The course-note markers `// [Course.AdvancedC#]` in `src/Blocks.EntityFrameworkCore/Repositories/CachedRepository.cs` and `IRepository.cs` (spec item 5: no course-note markers).
  - The to-do-like `// add other headers if needed, like tenant id, etc.` in `HasuraRegistration.cs`.
  - `// switch might be better here for type branching` in `src/Blocks.Core/Extensions/ReflectionExtensions.cs`: a to-do remark that also trips the statement-keyword rule.
- `///` blocks — all 8 kept, because the behaviour is not readable from the signature:
  - `src/Blocks.Core/Extensions/DateTimeExtensions.cs` (`ToUnixEpochDate`) — the unit (seconds) and rounding are not in `long ToUnixEpochDate(DateTime)`.
  - `src/Blocks.Core/Json/PrivateContractResolver.cs` — the class name does not say that private fields and properties are included.
  - `src/Blocks.Core/Mapster/Extensions.cs` (`MapToConstructor`) — which constructor is used. The text was corrected to "the destination's first declared constructor" because the code calls `GetConstructors().First()`; the old text said "the constructor with the most parameters".
  - `src/Blocks.Domain/Entities/IAssociationEntity.cs`, `IMetadataEntity.cs`, `TenantEntity.cs` (`IMultitenancy`) — empty marker interfaces whose meaning is the doc. In TenantEntity, the typo `EntitiyId` was fixed to `EntityId`.
  - `src/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs` (`SeedFromJsonFile`) — the file naming and location, and what the `bool` means. The malformed block (empty `<returns></returns>`, no closing `</summary>`) was tidied to a closed summary.
  - `src/Blocks.EntityFrameworkCore/Interceptors/TransactionalDispatchDomainEventsInterceptor.cs` — the three override docs (start a transaction before saving; dispatch, then commit, after saving; roll back on failure). The overrides' signatures carry none of this. Typos fixed: dispacth, imediatly, commited.

**Skills:** tdd (TDD yes per the amended plan); no pattern skill (plan: none, gap)

**Decisions:**
- **Marker pattern** allows whitespace after `//` (`//\s*insight`). The reference writes both `//insight` and `// insight`, and the plan-time count of 10 / 2 / 1 includes both forms.
- **Commented-out-code rule** is the plan's exact definition. Lines it cannot see (fluent `.Method(...)` lines, lines ending in `,` or `(`) were removed by reading. A broader rule would flag ordinary prose ending in a comma, such as `RequestContextMiddleware.cs:31`.

**Deviations:**
- Removed a few comments beyond the plan's three marker words and commented-out code: the `[Course.AdvancedC#]` tags, one to-do-like line in Hasura and one in `ReflectionExtensions`. The basis is spec item 5 (no course-note markers, no to-do comments).
- The `MapToConstructor` doc text now states what the code does rather than what the old comment claimed. This is a comment-only change; the code-versus-doc mismatch is under Carry-Over Findings below.

**Red on the pre-step-4 code** — each test's planted violation for step 13's Accept:
- `NoSourceFile_CarriesACourseNoteOrToDoMarker` — 13 lines (10 insight, 2 talk, 1 todo):
  `Redis/Entity.cs:6`; `EF/Repositories/Repository.cs:63, 64`; `EF/Extensions/RepositoryExtensions.cs:9, 19`; `Domain/IAuditableAction.cs:17`; `Domain/Entities/AggregateRoot.cs:21, 29`; `Core/GuardExtensions.cs:7`; `Core/Security/JwtOptions.cs:13`; `AspNetCore/HttpContextProvider.cs:15`; `AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs:41, 94`.
- `NoSourceFile_CarriesCommentedOutCode` — 10 lines:
  `Hasura/HasuraRegistration.cs:38, 53, 55, 58`; `EF/TenantDbContext.cs:25`; `EF/Repositories/Repository.cs:66, 67`; `Core/Security/JwtOptions.cs:14`; `Core/Extensions/ReflectionExtensions.cs:18`; `Core/Cache/ICacheable.cs:5`.

**Verification:**
- **Byte-identical DLLs.** Each of the 11 blocks was built with `--no-incremental -p:Deterministic=true -p:DebugType=none` before any comment edit and again after. `cmp` reports all 11 DLLs identical: Blocks.AspNetCore, Core, Domain, EntityFrameworkCore, Exceptions, FastEndpoints, Hasura, Http.Abstractions, MediatR, Messaging and Redis. The before and after copies are in the session scratchpad, `dll-before/` and `dll-after/`.
- After the edits, `dotnet test tests/Blocks.Hygiene.Tests`: 5/5 passed.
- `dotnet build Blocks.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test Blocks.slnx`: Hygiene 5/5, SurfaceDump 9/9.

## Step 5 — Error types and the error mapper (D1, A1)
**Files:**
- `src/Blocks.Exceptions/ForbiddenException.cs`, `ConflictException.cs`, `BadGatewayException.cs` — new; each an `HttpException` subclass with a `(string)` and a `(string, Exception)` constructor, as `BadRequestException` is (403, 409, 502).
- `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs` — D1 only:
  - `MapStatusCode` gains the three arms, then `HttpException e => e.HttpStatusCode` before the 500 fallback, so an unlisted HTTP error answers its own status.
  - One `catch (Exception ex) when (IsCausedByCancellation(ex))` replaces the `OperationCanceledException` catch; it walks the inner-exception chain and sets 499 only while `!Response.HasStarted`. It sits before the validation catch.
  - A 500 outside development replies "An unexpected error occurred." (`UnexpectedErrorMessage`); in development the thrown message stays.
  - Replies are serialized with `JsonNamingPolicy.CamelCase` (one static `JsonSerializerOptions`).
  - Kept: `sealed`, `IWebHostEnvironment`, the log threshold (500 and up), development `Details` = stack trace on every reply, the validation reply shape. Removed the `// biger than 500` comment (narration, boy-scout).
- `tests/Blocks.AspNetCore.Tests/` — new (xUnit v3, AwesomeAssertions, `Microsoft.AspNetCore.TestHost`; `FrameworkReference Microsoft.AspNetCore.App` as sprint-rituals' API tests). Added to `Blocks.slnx` under `tests`.
  - `ErrorMapperHost.cs` — a `HostBuilder` + `UseTestServer` host with only `GlobalExceptionMiddleware` and one endpoint; returns status and body.
  - `GlobalExceptionMiddlewareTests.cs` — 20 cases: `EachErrorType_AnswersItsStatus` (argument, bad-request, domain 400; 401; 403; 404; 409; 500; 502), `ValidationError_Answers400`, `Cancellation_Answers499`, `UnlistedHttpError_AnswersItsOwnStatus` (a 418 subclass), `CancellationWrappedInAnotherError_Answers499`, `CancellationAfterTheResponseStarted_LeavesTheStatus` (202 + body kept, no client exception), `ServerErrorOutsideDevelopment_CarriesTheFixedTextOnly`, `BadGatewayOutsideDevelopment_CarriesItsOwnMessage`, `ServerErrorInDevelopment_CarriesDetails` (message kept, details set), `ValidationErrorInDevelopment_CarriesDetails`, `ErrorReply_UsesCamelCaseNames`, `ValidationReply_UsesCamelCaseNames` (`errors[].propertyName`, `errors[].errorMessage`).
- `Directory.Packages.props` — `Microsoft.AspNetCore.TestHost` 10.0.12.

**Skills:** error-handling, tdd

**Rules:** none listed (`Satisfies:` names the spec AC "Error mapper tests", covered by the tests above except the conflict helper, step 6).

**Decisions:**
- **Fixed text only outside development**, as the plan words it; in development a 500 keeps the thrown message (the reference's behaviour) beside the stack trace. Pinned both ways by tests.
- **The fixed text applies to status 500 exactly**, not 5xx: a 502 `BadGatewayException` keeps its own message (pinned by a test).
- **Explicit arms kept for every listed type** beside the generic `HttpException` arm, per the error-handling skill's recipe (a type and its arm land together); the generic arm covers the rest.
- **The two cancellation catches are one** `when` filter: the inner-chain walk starts at the exception itself, so a bare `OperationCanceledException` is the first case of the same check.

**Deviations:** None.

**Red first:**
- 403/409/502: red at 500 with the types in place and no arms.
- Unlisted 418, wrapped cancellation: red at 500. After-started wrapped cancellation: red with `HttpRequestException` (the old code set the status on a started response).
- Fixed text and camelCase: red (`StatusCode`… names; message leaked). The two development-details tests went red only on the name casing — they characterize the reference's existing `Details`.
- Born green (reference behaviour kept): 400 ×3, 401, 404, 500 status, validation 400, plain 499.

**Mutation check (author-written, 6 mutants, all killed):** delete the generic `HttpException` arm; stop the inner-exception walk at the top; drop the `HasStarted` guard; drop `!IsDevelopment()` from the fixed-text condition; widen `== 500` to `>= 500`; drop the camelCase options. Classes: guard-deletion (arm, `HasStarted`, development check), refinement (inner walk, `==` → `>=`), predicate (`==` → `>=`). About 3 minutes; tdd skill's loop, recorded here after the fact.

**Verification:**
- `dotnet build Blocks.slnx`: 0 warnings, 0 errors.
- `dotnet test tests/Blocks.AspNetCore.Tests`: 20/20. `dotnet test tests/Blocks.Hygiene.Tests`: 5/5.

## Step 6 — EF Core fixes (D2, D3, A4) and the conflict helper
**Files:**
- `src/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` — `EnsureNotExistsOrThrowAsync` throws `ConflictException` instead of `BadRequestException` (409 through the step-5 mapper).
- `src/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs` — D2: removed `protected virtual string DefaultDateSql` and the `HasDefaultValueSql(DefaultDateSql)` on `CreatedOn` (now `IsRequired()` only). The entity's own `CreatedOn = DateTime.UtcNow` initializer supplies the value.
- `src/Blocks.EntityFrameworkCore/Repositories/Repository.cs`
  - D3: `UpsertAsync` copies `_dbContext.Entry(entity).CurrentValues` (sprint-rituals' line, as the persistence-patterns skill prescribes).
  - A4: `DeleteByIdAsync` runs `ExecuteSqlRawAsync(DeleteByIdStatement(), [id], ct)`. The new private `DeleteByIdStatement()` reads the table (`StoreObjectIdentifier.Create(…, Table)`) and the single key column (`GetColumnName(table)`) from the model, delimits both with `ISqlGenerationHelper.DelimitIdentifier`, and joins them with `string.Concat` into `DELETE FROM … WHERE … = {0}`; the id travels as a parameter. Signature and `bool` return kept; the public `TableName` property is untouched.
  - Added usings `Microsoft.EntityFrameworkCore.Infrastructure`, `.Metadata`, `.Storage`.
- `tests/Blocks.EntityFrameworkCore.Tests/` — new (xUnit v3, AwesomeAssertions, `Microsoft.EntityFrameworkCore.Sqlite`); in `Blocks.slnx` under `tests`.
  - `TestDatabase.cs` — one open in-memory SQLite connection per test, `EnsureCreated`; `Note : Entity` with only a private field `_text` mapped as `Text` (`PropertyAccessMode.Field`, no CLR property); `AuditedNote : AggregateRoot` on `AuditedEntityConfiguration` (domain events ignored); a plain `TestDbContext`; `RepositoryBase` subclasses for both.
  - `RepositoryTests.cs` — `EnsureNotExistsOrThrow_ThrowsConflictForAnExistingId` (type and `StatusCode` 409), `EnsureNotExistsOrThrow_PassesForAMissingId`, `Upsert_UpdatesAPropertyStoredInAPrivateField` (fresh-context read-back), `Upsert_AddsAnEntityThatDoesNotExist`, `DeleteById_RemovesOnlyThatEntityAndReturnsTrue`, `DeleteById_ReturnsFalseForAMissingId`.
  - `AuditedEntityConfigurationTests.cs` — `AuditedEntity_HasNoDatabaseSpecificDefaultOnCreatedOn` (no `GetDefaultValueSql()`, still required), `AuditedEntity_SavesWithTheCreatedOnItCarries`.
- `Directory.Packages.props` — `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12.
- `docs/specs/F1-VersionOne/delivery/questions.md` — Q4 (below).

**Skills:** persistence-patterns, tdd

**Rules:** none listed (`Satisfies:` names spec ACs: upsert of a field-backed entity — `Upsert_UpdatesAPropertyStoredInAPrivateField`; delete by id — `DeleteById_RemovesOnlyThatEntityAndReturnsTrue`; no database-specific default — `AuditedEntity_HasNoDatabaseSpecificDefaultOnCreatedOn`; conflict helper — `EnsureNotExistsOrThrow_ThrowsConflictForAnExistingId` with step 5's 409 mapping).

**Decisions:**
- **Delete key column from the model**, not the literal `Id` the reference used, so a renamed or convention-cased key column works; a composite key throws (`Single()`), as the reference's `Id` literal never handled one either.
- **Unknown entity / unmapped table / unmapped key** throw `InvalidOperationException` with the type name, the messages `ManualGenerateIdScope` already uses.

**Deviations:**
- **D3 red-first could not be shown** (Q4, open to the architect). On EF Core 10.0.12, `SetValues(entity)` with the entity's own CLR type reads through the model's getters, which honour field access, so the plan's test passes on the old line too. A probe (deleted) showed `SetValues` even fills `_text` from an anonymous object. The line is changed per spec D3 and the test stays as a pin; reverting the line is the one mutation survivor. Steps 7–9 do not depend on Q4.

**Red first:**
- Conflict helper: `Expected type to be ConflictException, but found BadRequestException`.
- D2: `Expected createdOn.GetDefaultValueSql() to be <null>, but found "GETUTCDATE()"`.
- A4: both delete tests `SqliteException: near "@p0": syntax error` (the table name went as a parameter).
- D3: green on the old code (Deviations, Q4). Born green as must-not checks: missing id passes the conflict helper; upsert of a new id adds; `CreatedOn` saved as carried.

**Mutation check (author-written):** `rows > 0` → `>= 0` killed; WHERE clause replaced by an always-true one killed (2 fails); upsert add-branch deleted killed; D3 line reverted SURVIVED (equivalent on EF Core 10 for this shape, see Q4). Classes: predicate, guard-deletion, refinement.

**Verification:**
- `dotnet build Blocks.slnx`: 0 warnings, 0 errors (no EF1002 on the concatenated statement).
- `dotnet test` EntityFrameworkCore.Tests 8/8, AspNetCore.Tests 20/20, Hygiene.Tests 5/5.

## Step 7 — FastEndpoints publisher (D4)
**Files:**
- `src/Blocks.FastEndpoints/DomainEventPublisher.cs` — publishes `((IEvent)@event).PublishAsync(Mode.WaitForAll, ct)`: the non-generic `IEvent` overload dispatches by the event's runtime type. Class shape (`sealed`, single event) and the `IDomainEventPublisher` contract unchanged; the cast is the only line taken from sprint-rituals.
- `tests/Blocks.FastEndpoints.Tests/` — new (xUnit v3, AwesomeAssertions, `Microsoft.AspNetCore.TestHost`, `FrameworkReference Microsoft.AspNetCore.App`); in `Blocks.slnx` under `tests`.
  - `DomainEventPublisherTests.cs` — a TestServer host with `AddFastEndpoints` scanning the test assembly and `MapFastEndpoints` (FastEndpoints needs one endpoint to start, hence `PingEndpoint`). Two events (`NoteAdded`, `NoteRemoved`), each carrying a list its handler appends to.
  - `PublishedEvent_ReachesTheHandlerForItsConcreteType` — publish through `IDomainEventPublisher`; `NoteAddedHandler` ran, exactly once.
  - `AnotherEventType_ReachesItsOwnHandler` — the same for the second event type.

**Skills:** domain-patterns, tdd

**Rules:** none listed (`Satisfies:` names spec AC "A FastEndpoints test…" — `PublishedEvent_ReachesTheHandlerForItsConcreteType`).

**Decisions:**
- **A real host, not a FastEndpoints test factory:** `UseFastEndpoints` requires a `WebApplication` (it casts to `IEndpointRouteBuilder`), so the test maps endpoints with `UseEndpoints(e => e.MapFastEndpoints())` on a `HostBuilder` test server; handlers are discovered by the same assembly scan an app uses.

**Deviations:** None.

**Red first:** both tests red on the old generic call on FastEndpoints 8.3.0 — `Expected added.Handled to be equal to {"NoteAddedHandler"}, but found empty collection` (and the same for `NoteRemoved`). Green after the cast.

**Verification:**
- `dotnet build Blocks.slnx`: 0 warnings, 0 errors.
- `dotnet test` FastEndpoints.Tests 2/2, Hygiene.Tests 5/5.

## Step 8 — Claims provider fix (A2)
**Files:**
- `src/Blocks.AspNetCore/HttpContextProvider.cs` — `GetClaimValues(claimName)` reads `claimName` instead of the hard-coded `ClaimTypes.Role`; still de-duplicated with `ToHashSet()`. `GetUserRoles()` / `GetUserRoles<TEnum>()` pass `ClaimTypes.Role` themselves, so they are unchanged.
- `tests/Blocks.AspNetCore.Tests/HttpContextProviderTests.cs` — new, on a `DefaultHttpContext` behind a real `HttpContextAccessor` (no host needed):
  - `GetClaimValues_ReturnsTheValuesOfTheClaimAskedFor` — two role claims and two `scope` claims; each query returns its own pair.
  - `GetClaimValues_ReturnsNothingForAClaimThePrincipalLacks`.

**Skills:** service-infra-conventions, tdd

**Rules:** none listed (`Satisfies:` names spec AC "A claims test…" — `GetClaimValues_ReturnsTheValuesOfTheClaimAskedFor`).

**Decisions:** None.

**Deviations:** None.

**Red first:** both red on the old line — the `scope` query returned the role values (`found at least one item {"editor"}`; the pair differed). Green after the fix.

**Verification:**
- `dotnet build Blocks.slnx`: 0 warnings, 0 errors.
- `dotnet test` AspNetCore.Tests 22/22, Hygiene.Tests 5/5.

## Step 9 — Block read-mes
**Files:** `src/Blocks.{AspNetCore,Core,Domain,EntityFrameworkCore,Exceptions,FastEndpoints,Hasura,Http.Abstractions,MediatR,Messaging,Redis}/README.md` — new, one per block, each with `## Purpose`, `## Depends on` (blocks, then packages) and `## Registration`, 15–40 lines, UTF-8 without byte-order mark, LF.
- Every type, method and constructor dependency named was checked against the source: registration methods (`AddAndValidateOptions`, `AddCodeFirstGrpcClient`, `AddMassTransitWithRabbitMQ`, `AddHasuraGraphQL` / `AddHasuraMetadata`, `UseCustomFastEndpoints`, `Migrate<T>`), and constructor needs (`ThreadSafeMemoryCache` → `IMemoryCache`; `AssignUserIdBehavior` → `IClaimsProvider`; `LoggingBehavior` → `RequestContext`; `HasuraMetadataInitService` → `IOptions<HasuraOptions>`).
- The packages listed are the project files' references after step 2. AspNetCore's and FastEndpoints' read-mes also name the three annotated pins (`protobuf-net`, `protobuf-net.Core`, `System.ServiceModel.Primitives`).
- **EntityFrameworkCore** has a "SQL Server only" paragraph naming both helpers: the manual-id insert scope (`UseManualGenerateId` / `ManualGenerateIdScope`, `SET IDENTITY_INSERT`) and the table reseed (`TryReseedTable`, `DBCC CHECKIDENT`).
- AspNetCore's read-me describes the error mapper as step 5 left it; EntityFrameworkCore's names `EnsureNotExistsOrThrowAsync` → `ConflictException` (step 6).

**Skills:** None (plan: no skill, gap; TDD no — gated by step 13's read-me test).

**Rules:** none listed.

**Decisions:**
- Registration sections show what an app writes, including the app-side lines a block needs but does not register itself (`AddMemoryCache`, `AddHttpContextAccessor`, `AddHostedService<HasuraMetadataInitService>`, the Redis OM index).

**Deviations:** None.

**Verification:**
- All 11 files: the three headings present; no byte-order mark; 0 CR bytes; 15–40 lines.
- No `article` / `journal` in any read-me; `dotnet test tests/Blocks.Hygiene.Tests` 5/5 (the product-word test reads every file in `src/`).
- No build input changed (read-mes are not compiled).

## Step 6 (redo) — D3 after the owner's Q4 ruling
The amended plan step 6 (Q4 answered by the owner, 2026-10-08) keeps the reference's upsert line. The conflict helper, D2 and A4 from the first pass are unchanged and still pass.

**Files:**
- `src/Blocks.EntityFrameworkCore/Repositories/Repository.cs` — `UpsertAsync` back to the reference's `_dbContext.Entry(existingEntity).CurrentValues.SetValues(entity)`; the first pass's `SetValues(_dbContext.Entry(entity).CurrentValues)` is gone.
- `tests/Blocks.EntityFrameworkCore.Tests/TestDatabase.cs` — added `Folder` and `FiledNote` (a `Title`, and an optional `Folder` navigation with no CLR key property). `FiledNoteConfiguration` maps the shadow foreign key `FolderId` and a shadow column `Tag`. `TestDbContext` applies both, and there is a `FiledNoteRepository`.
- `tests/Blocks.EntityFrameworkCore.Tests/RepositoryTests.cs` — new `Upsert_KeepsShadowPropertyValues`:
  - saves note 1 in folder 7 with `Tag = "pinned"`, then upserts a new `FiledNote { Id = 1, Title = "new" }`;
  - a fresh context reads `Title = "new"` (the update happened) and `FolderId = 7`, `Tag = "pinned"` (the shadow values kept);
  - the three checks run in one `AssertionScope`.
  - `Upsert_UpdatesAPropertyStoredInAPrivateField` is kept as written, as the step's one fail-first exemption.

**Skills:** persistence-patterns, tdd

**Rules:** none listed (`Satisfies:` names the spec upsert AC, both clauses: `Upsert_UpdatesAPropertyStoredInAPrivateField` and `Upsert_KeepsShadowPropertyValues`; the other three ACs as in the first pass).

**Decisions:** The shadow test also checks a normal property (`Title`) in the same run, so a no-op upsert cannot pass it.

**Deviations:**
- **persistence-patterns skill vs the plan:** the skill prescribes `SetValues(Entry(entity).CurrentValues)`. The amended plan (owner ruling) keeps the reference's `SetValues(entity)`, because on EF Core 10.0.12 the skill's line nulls shadow values. The plan's feature decision was followed; the skill gap is logged in `lessons.md` § Skill Gaps.
- `Upsert_UpdatesAPropertyStoredInAPrivateField` is green on both lines, as the amended plan states.

**Red first:** `Upsert_KeepsShadowPropertyValues` on the first pass's line: `Expected saved.FolderId to be 7, but found <null>. Expected saved.Tag to be "pinned", but found <null>.` Green after the revert.

**Mutation check (author-written):** deleting the `SetValues` line fails both upsert tests (guard-deletion). Reverting to the first pass's line fails the shadow test (the red above).

**Verification:**
- `dotnet build Blocks.slnx`: 0 warnings, 0 errors.
- `dotnet test`: EntityFrameworkCore.Tests 9/9, AspNetCore.Tests 22/22, FastEndpoints.Tests 2/2, Hygiene.Tests 5/5.

## Step 10 — The sync tool and the root read-me
**Files:**
- `tools/Blocks.Sync/` — new net10.0 console, no package references, in `Blocks.slnx` under `tools`. Entry `SyncCli.Run(args, stdout, stderr)`; `Program.cs` calls it.
  - `SyncCli.cs` — argument parsing (`--adopt` and `--json` are forward-only; anything else is a usage error, exit 2), the default `--source` (the folder holding `Blocks.slnx` above the tool's binaries), and exit codes (refusal 1; usage, environment and git errors 2).
  - `SyncSession.cs` — opens a run: app folder exists, source is a git checkout, manifest, origin check (`git remote get-url origin`, trailing `/` and `.git` ignored, case-insensitive), commit resolution ("fetch first" for the manifest's commit and every lock commit), lock, dependency closure from `ProjectReference` lines at the manifest's commit.
  - `Manifest.cs`, `LockFile.cs` — `blocks.json` / `blocks.lock.json` reading and validation (Rules 4 and 11); the lock is written sorted, indented, LF, no byte-order mark.
  - `BlockVersion.cs` — the file set (excluded folders `bin`, `obj`, `.vs`, `.vscode`, `.idea` at any depth; files `*.user`, `*.suo`, `.DS_Store`), the fingerprint (SHA-256 after removing a leading byte-order mark and CRLF → LF), a block read from a folder (refuses a symbolic link or junction anywhere in it) or from a commit (`git ls-tree -r -z`, `git cat-file blob`), and the three-way comparisons.
  - `Containment.cs` — block-name pattern, relative-path checks (no root, no `:`, no empty, `.` or `..` segment), inside-root check, link check of every existing folder from the root down to the write folder.
  - `SafeFiles.cs` — every write goes to a new temporary file in the same folder and is moved over the old name; removal deletes file-set files and the folders they leave empty.
  - `ForwardCommand.cs`, `BackCommand.cs`, `StatusCommand.cs` — Flows 1–3; every refusal is collected and printed before anything is written.
  - `FirstTake.cs` — Rule 7's check: `git status --porcelain -z --ignored -- .` run in the block folder; any entry in the file set counts (an app folder outside a git repository counts too).
  - `PackageReport.cs` — the closure's `PackageReference` versions from this repo's `Directory.Packages.props` at the manifest's commit plus every pin whose `Blocks` metadata names a closure block, compared with the app's packages file; text or `--json`.
  - `Messages.cs` — refusal texts and the way-out sentence.
  - `Git.cs` — read-only git calls, each with `--no-optional-locks`; the commands used are `rev-parse`, `remote get-url`, `cat-file`, `ls-tree`, `status`.
- `tests/Blocks.Sync.Tests/` — new xUnit v3 + AwesomeAssertions project (in `Blocks.slnx` under `tests`), 83 tests on temporary git repositories under `%TEMP%/blocks-sync-tests/{guid}` (a fake blocks repo with `Blocks.Core` ← `Blocks.Domain` ← `Blocks.Web`, a pin `protobuf-net` annotated `Blocks="Blocks.Web"`, a `tests/` folder; and a fake app repo). `SyncFixture.cs` holds the set-up, the before/after listing of the temp tree (path → SHA-256, minus allowed paths), and junction / hard-link creation (`mklink`).
- `README.md` — the root read-me: what the repo is, the block list, how to write `blocks.json`, the three commands, exit codes, `--adopt`, the way out of "changed on both sides", dropped blocks.
- `Blocks.slnx` — the two new projects.

**Tests, one per plan item:**

| Plan item | Test(s) |
|---|---|
| Flow 1 | `Flow1_Forward_CopiesListedBlockWithItsDependenciesAndWritesTheLock`; package report `Flow1_PackageReport_ListsMissingAndDifferentVersionsIncludingAnAnnotatedPin` |
| Flow 2 | `Flow2_Back_CopiesTheAppsBlockWholeIntoTheWorkingTreeWithoutAByteOrderMark`, `Back_BlockUnchangedInTheApp_LeavesTheWorkingTreeAlone`, `Back_BothSidesEqual_IsInStepAndWritesNothing` |
| Flow 3, each state | `Flow3_InStep`, `Flow3_ChangedInTheApp`, `Flow3_ChangedHere`, `Flow3_ChangedOnBothSides`, `Flow3_NoLongerListed`, `Flow3_NotYetTaken`, `Flow3_NotYetTaken_WithAnExistingFolder_SaysTheFirstTakeNeedsAdopt`; marks `Flow3_NewerHere_WhenTheCheckoutsHeadHoldsAnotherVersion`, `Flow3_UncommittedHere_WhenTheWorkingTreeDiffersFromHead`, `Flow3_BothMarks`; `Flow3_StatusWritesNothing` |
| Rule 1 | `Rule1_Forward_AddsReplacesAndRemovesFilesSoTheCopyEqualsTheBlock` |
| Rule 2 | `Rule2_Forward_RefusesAnUnsentAppEditAndWritesNothing`, `Rule2_Back_NeverOverwritesAnUncommittedChangeMadeHere` |
| Rule 3 | `Rule3_Forward_FileRenamedInTheApp_IsAChangeInTheApp`, `Rule3_Forward_LineEndingsAndAByteOrderMarkInTheApp_AreNotAChange` |
| Rule 4 | `Rule4_Forward_WritesOnlyTheBlockFoldersAndTheLock`, `Rule4_Back_WritesOnlyThisRepositorysBlockFolder`, `Rule4_BlockThatDoesNotExistUnderSrc_IsRefused`, `Rule4_Back_BlockMissingUnderThisRepositorysSrc_IsRefused` |
| Rule 5 | `Rule5_Forward_NeverCopiesTheTestsOfABlock` |
| Rule 6 | `Rules6And8_DroppedBlockEditedInTheApp_StillStopsAForwardRun` (and Rule 2's forward test) |
| Rule 7 | `Rule7_ExistingFolderWithNoLockEntry_IsRefusedWithoutAdopt`, `Rule7_AdoptOnACommittedFolder_ListsTheFilesToRemoveThenReplacesTheFolder`, `Rule7_AdoptWithAnIgnoredFileInTheFolder_IsRefused`, `Rule7_AdoptWithAnUncommittedEdit_IsRefused`, `Rule7_AdoptWithAnUntrackedFolder_IsRefused`, `Rule7_AdoptIgnoresIgnoredBuildOutput`, `Rule7_AfterTheFirstTake_AnAppEditIsRefusedAgain` |
| Rule 8 | `Rule8_DroppedBlock_KeepsItsFolderAndLockEntry`, `Flow3_NoLongerListed`, `Rules6And8_…` |
| Rule 9 | every refusal test asserts the block name and its file lines; `Rules9And10_ChangedOnBothSides_IsRefusedWithBothFileListsAndTheWayOut` asserts both sides' lists |
| Rule 10 | `Rules9And10_…` (the text); `Rule10_TheWayOutOfChangedOnBothSides_FollowedToTheEnd` (followed to the end: refused, edit set aside, forward, edit re-applied, back, commit here, manifest moved, forward → in step, lock at the new commit) |
| Rule 11 | `Rule11_UnreadableLock_StopsTheRunBeforeAnyWrite`, `Rule11_LockEntryWithNoFolder_StopsTheRunBeforeAnyWrite`, `Rule11_LockFileListThatIsNotTheBlocksFiles_StopsTheRunBeforeAnyWrite` (3 cases), `Rule11_LockEntryWithNoCommit_StopsTheRun`, `Rule11_Status_UnreadableLock_StopsTheRun` |
| Both refusals | changed in the app: `Rule2_Forward_…`; changed on both sides: `Rules9And10_…`, `Rule2_Back_…` |
| A file added in the app | `Forward_FileAddedInTheApp_IsAChangeInTheApp` |
| Each excluded folder and file kind | `Forward_BuildOutputAndEditorFiles_AreIgnoredAndLeftInPlace` (9 cases: `bin`, `obj`, `.vs`, `.vscode`, `.idea`, a nested `bin`, `*.user`, `*.suo`, `.DS_Store`) |
| Line endings and a byte-order mark | `Rule3_Forward_LineEndingsAndAByteOrderMarkInTheApp_AreNotAChange`; forward writes stored bytes `Forward_WritesThisRepositorysBytesAsStored` |
| A dropped block | `Rule8_…`, `Rules6And8_…`, `Flow3_NoLongerListed` |
| Block name with `..` | `Rule4_BlockNameThatIsNotABlockName_IsRefusedWithNothingWritten` (`../Blocks.Core`, `Blocks.Core/..`, `Blocks..Core`, `Other.Core`) |
| A rooted `blocksFolder` | `Rule4_RootedBlocksFolder_IsRefusedWithNothingWritten`; also `Rule4_BlocksFolderWithParentSegment_IsRefused` |
| A lock path escaping the app | `Rule4_LockPathEscapingTheApp_IsRefusedWithNothingWritten` |
| A junction on a write path | `Rule4_JunctionOnAWritePath_IsRefusedWithNothingWritten` (the block folder), `Rule4_JunctionAboveTheBlockFolder_IsRefusedWithNothingWritten`, `Rule4_JunctionInsideTheBlockFolder_IsRefusedWithNothingWritten`, `Rule4_Back_JunctionOnTheSendBackPath_IsRefusedWithNothingWritten` |
| A hard-linked destination | `Rule4_HardLinkedDestinationFile_LeavesTheOutsideFileUnchanged` (before/after listing of the temp tree outside the allowed folders is equal) |
| Package report with an annotated pin | `Flow1_PackageReport_ListsMissingAndDifferentVersionsIncludingAnAnnotatedPin`, `PackageReport_LeavesOutPinsForBlocksNotTaken`, `PackageReport_AppWithEveryVersion_SaysSo`, `Forward_NeverEditsTheAppsPackagesFile` |
| `--json` parsed | `Flow1_PackageReport_AsJson_IsOneParsableObjectOnStdout` |
| Origin check, "fetch first" | `Forward_ManifestSourceNotThisCheckoutsOrigin_IsRefused`, `Forward_ManifestCommitMissingInTheCheckout_SaysFetchFirst`, `LockCommitMissingInTheCheckout_SaysFetchFirst` |
| Usage and environment errors | `UsageError_ExitsWithTwo` (4 cases), `MissingApp_IsAUsageError`, `AppWithNoManifest_IsAnEnvironmentError`, `ManifestMissingAField_IsAnEnvironmentError` |
| Three-way, both sides equal | `ThreeWay_BothSidesEqual_IsInStepAndRefreshesTheLock` |

**Skills:** tdd (plan: no pattern skill, gap; TDD yes).

**Rules:** none listed (`Satisfies:` names the spec AC "Sync tests…" and the root read-me part of "Every block has a read-me…"; step 13 adds the root read-me test).

**Decisions:**
- Block names in the manifest are full names (`Blocks.Core`), as the plan's name pattern requires.
- Exit codes: a wrong origin, every containment and Rule 11 failure, both three-way refusals and the first-take refusals exit 1 (refused); a missing or malformed manifest, a missing commit ("fetch first") and git failures exit 2.
- A first take is refused without `--adopt` even when the app's folder already equals the block; a folder holding only excluded files (for example `bin`) counts as absent, so it is copied without `--adopt`.
- Forward checks every block in the closure plus every lock block; for closure blocks the lock entry is refreshed to the manifest's commit (also when in step). Dropped blocks keep their entry unchanged.
- Back works over the lock's blocks only (a block with no lock entry has no base) and never writes the lock; the next forward refreshes it.
- With `--json`, stdout carries only the report object; progress lines go to stderr. The report's `ours` is this repo's version, `theirs` the app's.
- Status prints one line per block: `{block}: {state}` plus `; newer here` and `; uncommitted here` marks; the marks are computed for every block.
- `Git.cs` is a copy of the snapshot tool's process wrapper with a non-throwing variant; the tools stay independent projects (see Carry-Over).

**Red first:** each test was seen failing before its code, except as listed under Deviations.
- Flow 1: `Expected result.ExitCode to be 0, but found 2` (the stub).
- Back (7 tests): `Expected result.ExitCode to be 0, but found 2` / `to be 1, but found 2` (back not dispatched).
- Status (11 of 12): `Expected result.ExitCode to be 0, but found 2`.
- Package report (4 of 5): `The input does not contain any JSON tokens` / `Expected result.Stdout to end with "packages the app's …"`.

**Mutation check (author-written; classes: guard-deletion, predicate (`&& false`), refinement on the junction scope).** Run by a scratch script that fails on an absent or repeated anchor, scored by the test runner's exit code, compile failures reported separately (~4 minutes in all).
- Forward: the dropped-block refusal, the both-sides branch, the changed-in-app branch, the `--adopt` gate, the not-committed gate, the parent-folder link check, the temp-file-then-move write (→ direct write: the hard-link test fails), the removal of files dropped here, the origin check, the lock folder check, the fingerprint pattern, the excluded-folder filter, the CRLF and byte-order-mark normalisation, the block-folder link check (killed only after adding `Rule4_JunctionInsideTheBlockFolder…`), the `..`/root path check, the first-take status filter — all killed.
- Back: the unchanged-in-app skip, the block-exists check, the both-sides branch, the link check, the byte-order-mark removal — all killed.
- Survivors, triaged: `Containment.Inside`'s starts-with check — EQUIVALENT (redundant chain: every path reaching it already passed `CheckRelative`, which refuses roots, `:` and `..`; kept as the terminal guard). Two survivors in `FirstTake` (an ancestor-path branch and a `+ "file"` suffix) were EQUIVALENT — git never reports a path above the `-- .` pathspec (checked in a scratch repo), and `IsInFileSet` already treats a trailing `/` correctly — so the code was simplified instead.

**Deviations:**
- Not strictly one test at a time: the forward refusal branches (both three-way refusals, first take, containment, lock checks) were written with the first forward slice, so their tests (in `ForwardTests`, `FirstTakeTests`, `ContainmentTests`) were born green. Their teeth are shown by the mutation check above, every branch killed. Back, status and the package report were driven red first.
- Verification spend not named by the plan: the mutation battery (~4 minutes), recorded here rather than before it ran.

**Verification:**
- `dotnet build Blocks.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test tests/Blocks.Sync.Tests`: 83/83 (about 21 s). `dotnet test tests/Blocks.Hygiene.Tests`: 5/5.
- New files: no byte-order mark, no CR bytes; `%TEMP%/blocks-sync-tests` empty after the run.

## Step 11 — Portability check
**Files:**
- `tests/Blocks.Portability.Tests/` — new xUnit v3 + AwesomeAssertions project referencing `tools/Blocks.Sync`, in `Blocks.slnx` under `tests`; the test class carries `[Trait("Category", "Portability")]`.
  - `SnapshotFixture.cs` (class fixture, once per run) — copies this repo's working-tree `src/` (skipping `bin`, `obj`, `.vs`, `.vscode`, `.idea`) and `Directory.Packages.props` into a new git repo under `%TEMP%/blocks-portability/{guid}/snapshot`, commits it, and sets `origin` to this repo's origin URL (read with `git --no-optional-locks remote get-url origin`). The block list is the `src/Blocks.*` folders, read at discovery (11 today).
  - `PortabilityTests.cs` — `Block_TakenAloneIntoAnEmptyApp_Builds(block)`, one case per block: asserts the temp paths are outside `D:\src`; a fresh app repo with a manifest naming that block and the snapshot's commit; `SyncCli.Run forward --source <snapshot> --json` exits 0; the report has no `different` rows; `Directory.Packages.props` (pinning on) written from the report's `missing` rows only; every block in the lock equals the snapshot's copy byte for byte (checked before the build); a new class library `Probe` referencing the block; `dotnet build` exits 0.
  - `Shell.cs` — process runner; child processes drop inherited `MSBuild*` and `VSTEST*` variables so the nested `dotnet build` resolves its own SDK.
- `Blocks.slnx` — the new project.

**Skills:** None (plan: no skill, gap; TDD no).

**Rules:** none listed (`Satisfies:` spec AC "Portability check").

**Decisions:**
- The block list comes from the `src/Blocks.*` folders, so a block added later is checked without editing the test.
- The probe library has one empty class; it references the block and nothing else. No `Directory.Build.props` is written into the app — the spec allows only the central file and the library.

**Deviations:** None.

**Planted failure:** with the central file written empty (the report ignored), the cases fail with `error NU1010: The following PackageReference items do not define a corresponding PackageVersion item: …` (for example `Blocks.Hasura`, `Blocks.EntityFrameworkCore`). Reverted; green again.

**Verification:**
- `dotnet build tests/Blocks.Portability.Tests`: 0 warnings, 0 errors.
- `dotnet test tests/Blocks.Portability.Tests`: 11/11 (about 35 s with a warm NuGet cache; network needed on a cold one). `%TEMP%/blocks-portability` empty after the run.

## Step 12 — Public-surface comparison and the names list
**Files:**
- `docs/public-names.md` — new. Every surface difference, grouped by block (namespace moved, renamed, removed, added, annotation changed), each with its reason; the error reply's wire names (reference PascalCase, version one camelCase, validation `errors[].propertyName` / `errorMessage`); and a table mapping each of the spec's 20 § Public names rows to where it appears.

**How:** each block built with `dotnet build src/Blocks.{Name}/Blocks.{Name}.csproj -p:CopyLocalLockFileAssemblies=true` (step 2's recipe), then `dotnet run --project tools/Blocks.SurfaceDump -- <the 11 block DLLs> --out <scratch>/surface-final.txt` (690 lines, against the reference's 684), then `diff` with `surface-reference.txt`: 93 lines only in the reference, 99 only in version one.

**Accounting:** every diff line sits in one row of `docs/public-names.md` (per block, removed / added lines: AspNetCore 10/8, Core 27/27, Domain 24/24, EntityFrameworkCore 10/9, Exceptions 2/11, FastEndpoints 3/3, Hasura 2/2, MediatR 9/9, Redis 6/6; Http.Abstractions and Messaging 0/0).
- Explained by the spec: every namespace move, every rename, `GetArticleId` and `DefaultDateSql` removed, the three new exception types, the camelCase reply.
- Explained by step 2's warning fixes: `GetRouteValue` → `string?`, `PropertiesToDictionary` / `ToStringDictionary` value types, `ReflectionExtensions.GetValue` → `object?`, `ToInt(string?)`, `TenantRepositoryBase.GetById` / `GetAsync` → `TEntity?`, and two `ThreadSafeMemoryCache.GetOrCreate` parameters (`T? value` → `T value`, `Func<ICacheEntry, T?>` → `Func<ICacheEntry, T>`). The source declares those two parameters the same in both versions; the dump changed with step 2's return-type annotation on the same two methods (note 1 in the list).
- Consequence of the moves: the base lists of 17 Domain types, which now name `IAuditedEntity` and `IDomainObject` by their new namespaces.
- No difference was left without an explanation, so no question went to the architect.

**Skills:** None (plan: no skill, gap; TDD no).

**Rules:** none listed (`Satisfies:` spec AC "`docs/public-names.md` lists every difference…").

**Decisions:** The final dump stays in the session scratchpad, not in the repo; the plan asks only for the list.

**Deviations:** None.

**Verification:** 93 + 99 diff lines counted against the list's rows (above); all 20 spec rows present; the file is UTF-8 without byte-order mark, LF.

## Step 13 — Repo-wide hygiene tests
**Files:**
- `tests/Blocks.Hygiene.Tests/FileTests.cs` — new: `NoFileInTheRepo_StartsWithAByteOrderMark` (every file, `docs/` included); `NoFileInTheRepo_SuppressesAWarning` (`#pragma warning disable`, `SuppressMessage`, `NoWarn`, `WarningsNotAsErrors`, case-insensitive, in every file except `*.md`; the four forms are built from two string pieces each, so the test file does not match itself).
- `tests/Blocks.Hygiene.Tests/PackageTests.cs` — new: `PackageVersions_StayOnTheFreeReleases` (every `PackageVersion` / `PackageReference` with `Version` or `VersionOverride` in `*.csproj`, `*.props`, `*.targets`: MediatR present and exactly 12.5.0; no MediatR ≥ 13, AutoMapper ≥ 15, MassTransit* ≥ 9, FluentAssertions ≥ 8); `EveryResolvedPackage_CarriesAnAllowedLicence` (runs `dotnet list Blocks.slnx package --include-transitive --format json`, reads each package's `.nuspec` in the NuGet global packages folder — `NUGET_PACKAGES`, else `dotnet nuget locals global-packages --list`; passes an SPDX expression of MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause or MS-PL, an `OR` list with one of them, or a licence URL on the allow list in the file, which is empty today).
- `tests/Blocks.Hygiene.Tests/ReadMeTests.cs` — new: `EveryBlock_HasAReadMeWithTheThreeHeadings` (11 `src/Blocks.*` folders, `## Purpose`, `## Depends on`, `## Registration`); `TheDataAccessReadMe_SaysBothSeedingHelpersNeedSqlServer` ("SQL Server only", `ManualGenerateIdScope`, `TryReseedTable`); `TheRootReadMe_SaysHowToRunTheSyncTool` (`blocks.json`, `-- forward`, `-- back`, `-- status`, `--adopt` and the way-out sentence, line breaks folded).
- `tests/Blocks.Hygiene.Tests/RepoFiles.cs` — the walker also skips `.claude` folders.
- `tools/Blocks.ForeignSnapshot/SnapshotComparer.cs` — new `compare` logic, Q2 scope: the record's first section (the reference) is compared line for line; in the other sections the `head` line and every `status`, `diff`, `untracked` and `ignored` line whose path is under `src/BuildingBlocks/` (or is a collapsed folder above it, such as `?? src/`) must be equal; every other difference is listed. A repo recorded on one side only fails.
- `tools/Blocks.ForeignSnapshot/SnapshotCli.cs` — `compare --before <file> --after <file>` prints the must-be-equal and listed differences, exits 0 when the must-be-equal part is empty, 1 otherwise, 2 on usage; `record` unchanged in behaviour.
- `docs/specs/F1-VersionOne/delivery/foreign-repos-after.txt` — written by `record` with the three repos in step 1's order; git-ignored like the before file.

**Skills:** None (plan: no skill, gap; TDD no).

**Rules:** none listed.

**Decisions:**
- "Anywhere" for the suppression test means every file except Markdown: the plan, spec and this file name the four forms as prose, so a literal whole-repo scan would fail on the plan itself.
- The hygiene walker skips `.claude` folders (session state written by the tooling, partly git-ignored) as well as `bin`, `obj` and `.git`, for every hygiene test.
- `compare` takes the reference as the first section of the before record (step 1 recorded it first), so the command needs no repo names.

**Planted violations (each red, then reverted; runs with `--no-build` where only a file changed):**

| Test | Planted | Red output |
|---|---|---|
| byte-order mark | `docs/planted-bom.md` starting `EF BB BF` | `found "docs/planted-bom.md"` |
| suppressions | `docs/planted.txt` with `<NoWarn>CS1591</NoWarn>` | `found "docs/planted.txt:1: <NoWarn>CS1591</NoWarn>"` |
| package versions | MediatR 13.0.0 in `Directory.Packages.props`; then MassTransit.RabbitMQ 9.0.0 | `Expected … to be "12.5.0", but "13.0.0" differs`; `found "Directory.Packages.props: MassTransit.RabbitMQ 9.0.0"` |
| licences | MIT dropped from the allowed list (rebuilt) | `found "FastEndpoints 8.3.0: expression MIT …"` |
| block read-mes | `## Registration` renamed in `src/Blocks.Redis/README.md` | `found "Blocks.Redis: ## Registration"` |
| data-access read-me | `TryReseedTable` renamed in the EF read-me | `Expected text "# Blocks.EntityFrameworkCore …" to contain "TryReseedTable"` |
| root read-me | "apply the edit again" reworded | `Expected words "# dotnet-building-blocks …" to contain "set the app's edit aside, …"` |
| namespace, Async names, product words, markers, commented-out code | the real pre-step code in steps 3 and 4 (Q3) | recorded in the step 3 and 4 blocks |

`compare` on hand-made records: identical → exit 0; a reference listing line changed → 1; another repo's line outside `src/BuildingBlocks/` → 0, listed; another repo's HEAD → 1; an untracked hash and an ignored time under `src/BuildingBlocks/` → 1 each; `?? src/` added → 1; a repo missing after → 1; no arguments → 2.

**Foreign repos (Q2):** `compare --before foreign-repos-before.txt --after foreign-repos-after.txt` → `must be equal: 0 difference(s)`, `listed, not failed: 0 difference(s)`, exit 0. The two records are byte-identical (`cmp`), so there is nothing for the owner to confirm as their own.

**Deviations:** None beyond the Decisions above.

**Verification:**
- `dotnet build Blocks.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test tests/Blocks.Hygiene.Tests`: 12/12 (the comment tests now also scan the new `tools/` code).

## Fix Round 1
Fix list: `review.md` § Step 2 — Fix list, cycle 1, rows 1–14, plus row 15 (Q5, owner answer option 1, sent by the team lead during the round). Under-bar block: none. Q6 and Q7 bind rows 13 and 14.

**Files:**
- Row 1 (HIGH, lock contents) — `tools/Blocks.Sync/SyncSession.cs`: after every lock commit resolves ("fetch first" keeps precedence), each lock entry's file list is compared with the block's files at that entry's commit (`BlockVersion.FromCommit`); a mismatch refuses every command (forward, back, status) before any write. `tools/Blocks.Sync/Messages.cs` — `LockIsNotTheBlock`: `refused: blocks.lock.json cannot be read: {block}'s files are not the block's files at {commit}` plus the changed file lines (Rule 9).
- Row 2 (HIGH, hidden edits) — `tools/Blocks.Sync/FirstTake.cs`: when `git status` lists nothing, every physical file in the block's file set must equal a blob of the app's `HEAD` (`git ls-tree -r -z HEAD -- .`, `cat-file blob`), compared by fingerprint; a file not in `HEAD` or different is listed as `differs from the app's last commit: {path}`. Catches `--assume-unchanged` and `--skip-worktree` edits.
- Row 3 (file↔folder) — `tools/Blocks.Sync/SafeFiles.cs`: `Mirror` removes obsolete files before writing; new `CheckReplaceable` runs in the preflight of forward and back and refuses (before any write) when a wanted file must replace a folder that still holds files outside the block's files (excluded files are kept, never deleted), or when a wanted path needs a folder where such a file sits. Messages `FolderInTheWay`, `FileInTheWay`.
- Row 4 (report before writes) — `tools/Blocks.Sync/ForwardCommand.cs`: `PackageReport.Build` moved before the preflight and the first write; a malformed app packages file or a missing central file here now exits 2 with both repositories unchanged.
- Row 5 (root links) — `tools/Blocks.Sync/Containment.cs` `CheckNoLinks` checks the root itself first; forward also checks the lock's folder (the app root) even when only the lock is written; back checks the source root through the block-folder check.
- Row 6 (surface tool) — `tools/Blocks.SurfaceDump/NullableFlags.cs` (new): decodes the declaration's `[Nullable]` bytes (falling back to `[NullableContext]` on the member, then the declaring types) in the compiler's pre-order; `TypeNames.Render` consumes one flag per node that carries one (type parameters, arrays, reference types, generic value types; not non-generic value types or `Nullable<T>`), so `T` and `T?` render as declared. `Declarations.cs` uses it for returns, parameters, properties, fields and events; `NullabilityInfoContext` is gone (it reports an unconstrained `T` as nullable). `docs/specs/F1-VersionOne/delivery/surface-reference.txt` regenerated from the same reference build (`%TEMP%\blocks-baseline-fda2eb7`, unchanged since step 2): 684 lines; the only changes from the old file are generic-parameter marks. `docs/public-names.md`: counts 102 / 108; the two wrong "note 1" rows replaced by `TryGet` (`out T?`), `GetOrCreateAsync` (`Task<T?>`), `GetOrCreate` (`T?`) for interface and class, and `DeserializeCaseInsensitive` (`T?`); rows for the three renames (row 7); Messaging no longer "no change".
- Row 7 (names) — `src/Blocks.Core/Mapster/DependencyInjection.cs` → `MapsterRegistrationExtensions.cs` (class `MapsterRegistrationExtensions`); `src/Blocks.Messaging/MassTransit/DependencyInjection.cs` → `MassTransitRegistrationExtensions.cs`; `src/Blocks.Hasura/HasuraRegistration.cs` → `HasuraRegistrationExtensions.cs`. Files moved with `mv`. No code, read-me or test referenced the old class names. `tests/Blocks.Hygiene.Tests/NamingTests.cs` — new `EveryClassOfExtensionMethodsInTheBlocks_EndsInExtensions`.
- Row 8 — `src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs`: `//:IMemoryCache` removed. `tests/Blocks.Hygiene.Tests/CommentTests.cs`: the commented-out-code test also reads a trailing comment after code (only when no `"` or `/` precedes it, so URLs and strings are skipped), and both forms now also flag a commented-out base list (`:Name`) and a fluent call (`.Name(`).
- Row 9 — `tools/Blocks.Sync/SyncCli.cs`: `--app` / `--source` paths go through `TryFullPath` inside the parse; empty, blank or invalid paths are usage errors (exit 2).
- Row 10 — `Containment.CheckRelative` drops empty and `.` segments and returns the cleaned path (spec Rule 4 asks only relative, no `..`, no root); a path with nothing left is refused with `'{path}' names no folder inside the app`. `tools/Blocks.Sync/Manifest.cs` keeps the cleaned `blocksFolder` / `packagesFile`. `tools/Blocks.Sync/LockFile.cs`: a lock path must already be in clean form (replaces the backslash check), else "cannot be read".
- Row 11 — `tests/Blocks.AspNetCore.Tests/ErrorMapperHost.cs`: optional `CapturedLogs` provider; `GlobalExceptionMiddlewareTests.OnlyAStatusOf500AndAbove_IsLoggedAsAnError` (404 no, 500 yes, 502 yes). `tests/Blocks.EntityFrameworkCore.Tests/TestDatabase.cs`: `Label` with its key mapped to column `LabelKey`; `RepositoryTests.DeleteById_UsesTheKeyColumnTheModelMaps`. `tests/Blocks.Sync.Tests/CliTests.DefaultSource_IsTheCheckoutTheToolRunsFrom` (no `--source`: the refusal names this checkout's origin).
- Row 12 — narration comments removed from 19 files across AspNetCore, Core, Domain, EntityFrameworkCore, Hasura and Messaging (36 lines: e.g. `// get correlation`, `// begin`, `// end`, `// Core` / `// Batching` headers in `IRepository`, the `RabbitMqOptions` defaults, `// time in milliseconds`, `// Remove padding`); runs of blank lines left in those files were collapsed. Kept, as reasons the code cannot show: the gRPC retry and singleton lines, the `ThreadSafeMemoryCache` thread-safety line, the aggregate audit line, the reseed "previous attempt failed" line, the TPH root line, the raw-SQL delete line, the Hasura retry, already-tracked 400, older-versions fallback and result-shape lines, the Redis.OM workaround. A comment-stripped compare of each touched file before and after shows no code line changed (copies in the session scratchpad, `comments-before/`).
- Row 13 (Q6) — `Directory.Packages.props`: `System.ServiceModel.Primitives` pin removed; `src/Blocks.AspNetCore/README.md` and `src/Blocks.FastEndpoints/README.md` pin lines name only `protobuf-net` and `protobuf-net.Core`.
- Row 14 (Q7) — `src/Blocks.AspNetCore/README.md`: outside development only a 500 is replaced by the fixed text; every other status carries its exception's message, so write those messages for the client.
- Row 15 (Q5) — `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`: the 499 filter is `context.RequestAborted.IsCancellationRequested && IsCausedByCancellation(ex)`; its trailing comment went with it. A cancellation without a client abort falls through to the normal mapping (a wrapping `HttpException` → its own status, a bare cancellation → 500, both logged). `ErrorMapperHost` gains `clientAborted`, which sets an `IHttpRequestLifetimeFeature` whose `RequestAborted` is cancelled, so the reply can still be read. `Cancellation_Answers499`, `CancellationWrappedInAnotherError_Answers499` and `CancellationAfterTheResponseStarted_LeavesTheStatus` now run with the client aborted (the first two also assert no error log; the third writes with `CancellationToken.None`). New `UpstreamTimeoutWrappedAsBadGateway_Answers502` (logged) and `CancellationWithoutClientAbort_IsNot499` (500, logged). `src/Blocks.AspNetCore/README.md`: "499 when the client aborts the request".
- Tests added in `tests/Blocks.Sync.Tests/`: `ContainmentTests` — `Rule11_LockFingerprintThatHidesAnAppEdit_StopsTheRunBeforeAnyWrite`, `Rule11_LockFileListMissingAFileOfTheBlock_StopsTheRunBeforeAnyWrite` (forward, back, status), `Rule4_BlocksFolderWithACurrentFolderSegmentOrATrailingSlash_IsTheSameFolder` (3 cases), `Rule4_BlocksFolderThatNamesNoFolder_IsRefusedNamingTheCause` (2), `Rule4_AppRootThatIsAJunction_IsRefusedWithNothingWritten`, `…_IsRefusedEvenWhenOnlyTheLockWouldBeWritten`, `Rule4_Back_SourceRootThatIsAJunction_IsRefusedWithNothingWritten`, and lock paths `./Text.cs`, `Strings//Casing.cs` added to `Rule11_LockFileListThatIsNotTheBlocksFiles…`; `FirstTakeTests.Rule7_AdoptWithAnEditHiddenFromGitStatus_IsRefused` (assume-unchanged, skip-worktree); `TopologyTests.cs` (new) — forward file→folder and folder→file, back both ways, and a folder holding build output that must become a file (refused, nothing written); `PackageReportTests` — unreadable app packages file and no central file here, both "nothing written"; `CliTests` — empty `--app` / `--source`, an invalid character, the default source. `SyncFixture.cs` — `RunIn(app, source, …)` and `RunArgs(…)` for runs through a junction or without `--source`.
- `tests/Blocks.SurfaceDump.Tests/SurfaceDumperTests.cs` — `UnconstrainedGenericMadeNullable_ChangesDump` (return, parameter, `out`, `Task<T>`, `Func<T>`, a field of a generic class).

**Skills:** None invoked through the Skill tool this round — deviation, see `## Skills Used`. The tdd loop was followed: every new behaviour test was seen red first, except the pins below.

**Rules:** none listed.

**Decisions:**
- Lock validation applies to every command, after all lock commits resolve, so "fetch first" (exit 2) still wins over a mismatch (exit 1).
- The first-take physical check compares fingerprints, so a line-ending or byte-order-mark difference (Rule 3) is not "not committed"; it runs only when `git status` lists nothing, so a file is never reported twice.
- A folder in the way of a wanted file is replaced only when everything under it is a file the copy removes; any other entry (build output, editor files, a link) refuses the run. Excluded files are never deleted.
- `./x`, `x/` and `x//y` are accepted as the same folder (spec Rule 4 does not forbid them); lock paths, which the tool writes, must be in clean form.
- Extension-class names: `MapsterRegistrationExtensions`, `MassTransitRegistrationExtensions`, `HasuraRegistrationExtensions`, after the existing `GrpcClientRegistrationExtensions`. The naming check counts a static class whose public methods are all extension methods; `Blocks.Core.Guard` (guard helpers plus one extension, `ThrowIfFalse`) is not such a class and keeps its name (Carry-Over).
- The surface tool now prints the declared annotation only; `[MaybeNull]`-style attributes are not part of the dump (the reference and version one go through the same tool).

**Red first:**
- Row 6: `Expected before … to have an item matching l.EndsWith("method public abstract T Get<T>(T value)")` — the old tool printed `T? Get<T>(T? value)` for both sources.
- Row 7: `found "Blocks.Hasura.HasuraRegistration, Blocks.Mapster.DependencyInjection, Blocks.Messaging.MassTransit.DependencyInjection"` (a first draft that counted any class with one extension also listed `Blocks.Core.Guard`; narrowed, see Decisions).
- Row 8: `found "src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs:5: public interface IThreadSafeMemoryCache //:IMemoryCache"`; no other line in `src/` or `tools/` matched the wider rule.
- Sync, 26 failing before the code: the hidden-fingerprint lock let forward overwrite the app edit (listing differed); `--adopt` with an assume-unchanged edit copied (`exit 0 … first take; removing 1 file(s)`); file→folder forward `Cannot create '…\Data' because a file or directory with the same name already exists`; the unreadable app packages file was reached after `copied at …`; the junction app root `copied at …`; empty paths threw `ArgumentException`; `./blocks` and `blocks/` refused as "must be a relative path"; `./Text.cs` and `Strings//Casing.cs` refused with the relative-path message instead of "cannot be read".
- Row 15: `Expected reply.Status to be HttpStatusCode.BadGateway {value: 502}, but found HttpStatusCode.499` and `… InternalServerError {value: 500}, but found HttpStatusCode.499` on the old filter.
- Pins, born green: the log threshold (killed by `>=` → `>`: the 500 case fails), the delete key column (killed by `DelimitIdentifier("Id")`: `DeleteById_UsesTheKeyColumnTheModelMaps` fails), the default source, and the three client-aborted 499 tests (behaviour kept). Both mutants reverted (checked by grep).

**Deviations:**
- `src/Blocks.Core/Guard` not renamed (Decisions; Carry-Over).
- Row 12 also collapsed pre-existing double blank lines in the 19 touched files (whitespace only).
- Skill tool not invoked for the round's steps 5, 6 and 10 work (tdd, error-handling, persistence-patterns); the red-first record above is the tdd evidence.

**Verification:**
- `dotnet build Blocks.slnx -warnaserror`: 0 warnings, 0 errors.
- `dotnet test` per project: AspNetCore 27/27, EntityFrameworkCore 10/10, FastEndpoints 2/2, SurfaceDump 10/10, Hygiene 13/13, Sync 110/110. Portability not run (close gate; row 13 changes the central file it reads).
- Surface: 11 blocks built with `-p:CopyLocalLockFileAssemblies=true`, dumped (690 lines), diffed with the regenerated baseline: 102 / 108 lines, per block AspNetCore 10/8, Core 31/31, Domain 24/24, EntityFrameworkCore 10/9, Exceptions 2/11, FastEndpoints 3/3, Hasura 5/5, MediatR 9/9, Messaging 2/2, Redis 6/6, Http.Abstractions 0/0 — each line in one row of `docs/public-names.md`. Item 15 changes no public surface.
- No CR byte in the touched files; `%TEMP%/blocks-sync-tests` empty after the run.

## Fix Round 2
Fix list: `review.md` § Step 2 — Fix list, cycle 2, rows 1–2. Under-bar block (hidden deletions under `--skip-worktree`; the surface tool's crash on a missing path) read, not fixed.

**Files:**
- Row 1 (skill slips) — no file changed by the skill checks themselves; each mapped skill was invoked through the Skill tool and fix round 1's work on its step checked against it:
  - Step 2, central-package-management: three-form grep over every `*.csproj` in `src/`, `tests/`, `tools/` (`Version="`, `<Version>`, `VersionOverride=`): no hits. One `Directory.Packages.props`, at the repo root (the plan's place; the skill shows `src/`, already recorded at step 1). `ManagePackageVersionsCentrally` and `CentralPackageTransitivePinningEnabled` on. Row 13's removal of the `System.ServiceModel.Primitives` pin leaves no reference to it in the central file or the read-mes. No change.
  - Step 5, error-handling: row 15's 499 path is a separate `catch … when` for a client disconnect, as the skill's mapping lists it; errors stay thrown and are translated once in the middleware, `try/catch` only at that boundary, stack traces only in development. The generic `HttpException` arm beside the listed types is step 5's plan decision (Key Decisions), not new in fix round 1. No change.
  - Step 6, persistence-patterns: row 11 added only the test `DeleteById_UsesTheKeyColumnTheModelMaps`; `DeleteByIdAsync` stays the skill's raw-SQL delete. No change.
  - Steps 3, 4, 5, 6, 10, tdd: the red-first record of fix round 1 covers new behaviour. Its born-green pins had only a predicate or refinement mutant each; the retro-fit variant asks for all three classes, so a battery was run (below). One survivor was real: `CliTests.DefaultSource_IsTheCheckoutTheToolRunsFrom` stays green when the default source is the tool's own folder instead of the checkout root (git resolves the same origin from a subfolder). `tests/Blocks.Sync.Tests/CliTests.cs` — new `DefaultSource_IsTheRootOfThatCheckout`: a manifest with this checkout's origin and a missing commit; the "fetch first" message must name the checkout root.
- Row 2 (empty folders) — `tools/Blocks.Sync/SafeFiles.cs`: `Mirror` deletes the empty folders under a wanted file's path before writing it (`DeleteEmptyFolders`: bottom-up, non-recursive `Directory.Delete`, never enters a link, so it can never delete a file). `CheckReplaceable` is unchanged: it already refuses any file or link under that path that the copy does not remove, so after the preflight only empty folders can be left there. Forward and back both go through these two calls.
- `tests/Blocks.Sync.Tests/TopologyTests.cs` — `Forward_FolderWithAnEmptyFolderBesideItsFileThatBecameAFileHere_IsReplacedInTheApp` (the re-review's case), `Forward_NewFileWhereTheAppHasEmptyFolders_IsWrittenInTheirPlace` (empty target, nested), `Back_FolderWithAnEmptyFolderBesideItsFileThatBecameAFileInTheApp_IsReplacedHere`, `Back_NewFileWhereThisRepoHasEmptyFolders_IsWrittenInTheirPlace`, `Forward_FolderHoldingAJunctionThatMustBecomeAFile_IsRefusedWithNothingWritten` (the junction's target file is kept).

**Skills:** central-package-management (step 2), tdd (steps 3, 4, 5, 6, 10 and row 2), error-handling (step 5), persistence-patterns (step 6) — all through the Skill tool, before the round's first edit.

**Rules:** none listed.

**Decisions:**
- Row 2 removes the empty folders rather than refusing: they hold nothing, and a refusal would leave a user stuck on a folder git does not even track. Anything else under the path (an excluded file, build output, a link) still refuses before any write, as fix round 1 decided.
- A link under the path is refused earlier than the topology preflight, by the block-folder read (`BlockVersion`: `refused: {path} is a symbolic link or junction`); the junction test asserts that message verbatim.

**Red first:**
- Row 2: `Expected result.ExitCode to be 0 because Access to the path is denied.` — forward wrote the file onto the leftover `Data/` folder.
- The other four topology tests were written after the code (born green); the battery shows their teeth.
- `DefaultSource_IsTheRootOfThatCheckout`: written after the survivor was found; killed the survivor (below).

**Mutation battery (author-written, scripted; a missing anchor aborts; scored by the runner's exit code, each kill names failing tests, so none was a compile failure; green baseline before, green after revert):**
- New code, `TopologyTests`: guard-deletion of the empty-folder removal — killed (4 tests); predicate `Directory.Exists` → `!Directory.Exists` — killed (10); guard-deletion of the recursion — killed (4); refinement, recurse into links too — survived, EQUIVALENT by construction (redundant chain: `BlockVersion` refuses any link in the block folder before any write); the chain as a set (that filter plus the `BlockVersion` link refusal) — killed by the junction test.
- Fix round 1 pins: log guard deleted (log every status) — killed; `HasStarted` guard deleted — killed; cancellation never found (predicate) — killed; top exception only, no inner chain (refinement) — killed; `rows > 0` → `rows >= 0` — killed; default source without the upward search (guard-deletion) — survived, REAL, then killed by the new test.

**Deviations:**
- The skill checks changed no code; the tdd check added one test (above).

**Verification:**
- `dotnet test tests/Blocks.Sync.Tests -warnaserror`: 116/116. `tests/Blocks.Hygiene.Tests`: 13/13. AspNetCore and EntityFrameworkCore test classes green after the battery's revert (production files checked by grep). No CR byte in the touched files; `%TEMP%/blocks-sync-tests` empty after the run.

## Key Decisions
- Step 1: the foreign-repo record uses `-z` path lists and full-path repo headers.
- Step 2:
  - FastEndpoints is on 8.3.0 (latest 8.x).
  - Test packages are at sprint-rituals' versions.
  - Surface line format: `{assembly} | {namespace} | {type} | {kind} {declaration}`.
  - Step 12 dumps the build made with `-p:CopyLocalLockFileAssemblies=true`.
- Step 3: the Async-name test also exempts compiler-generated members and accessors; the product-word test reads every file in `src/`.
- Step 4: the marker pattern allows whitespace after `//`; the commented-out-code rule is exactly the plan's.
- Step 5:
  - A 500 replies with the fixed text only outside development; in development the thrown message stays.
  - The fixed text is for status 500 exactly; a 502 keeps its message.
  - Explicit arms kept for the listed types beside a generic `HttpException` arm.
  - One `when` filter walks the inner-exception chain for cancellation.
- Step 6:
  - Delete-by-id reads the key column from the model (single key); unmapped entity, table or key → `InvalidOperationException`.
  - D3 keeps the reference's upsert line (owner, Q4); the shadow-property test also checks a normal property.
- Step 7: the publisher test runs a real `HostBuilder` test server with `MapFastEndpoints` and one endpoint.
- Step 9: Registration sections include the app-side lines a block needs but does not register itself.
- Step 10:
  - Manifest block names are full names (`Blocks.Core`).
  - Exit 1 for every refusal (origin, containment, Rule 11, three-way, first take); exit 2 for usage, a missing or malformed manifest, "fetch first" and git failures.
  - A first take needs `--adopt` even when the folder already equals the block; a folder with only excluded files counts as absent.
  - Back works over the lock's blocks and never writes the lock.
  - With `--json`, stdout carries only the report; `ours` is this repo's version, `theirs` the app's.
- Step 11: the portability cases come from the `src/Blocks.*` folders; no `Directory.Build.props` is written into the temp app.
- Step 12: the final surface dump stays in the session scratchpad.
- Step 13:
  - The suppression test scans every file except Markdown.
  - The hygiene walker skips `.claude` folders.
  - `compare` treats the record's first section as the reference.
- Fix round 1:
  - The lock is checked against each entry's commit on every command; "fetch first" still comes first.
  - `--adopt` also compares the folder's files with the app's last commit, by fingerprint.
  - A folder that must become a file (or the reverse) is replaced only when nothing outside the block's files is in the way; otherwise the run is refused before any write.
  - `./x` and `x/` name the same folder; lock paths must be in clean form.
  - Registration classes renamed to `…RegistrationExtensions`; `Guard` keeps its name.
  - The surface tool prints declared annotations only, `T` versus `T?` included.
  - 499 only when the client aborted the request (Q5).
- Fix round 2:
  - Empty folders where a block file must go are deleted before the write; anything else there still refuses before any write.
  - The default source is pinned to the checkout root by its "fetch first" message.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | central-package-management | Props file kept at the repo root, as the plan says (skill shows `src/`) |
| 2 | framework-currency, central-package-management | No `Send*Async` calls to migrate |
| 3 | tdd | Plan: no pattern skill (gap); TDD yes after the Q3 amendment |
| 4 | tdd | Plan: no pattern skill (gap); TDD yes after the Q3 amendment |
| 5 | error-handling, tdd | |
| 6 | persistence-patterns, tdd | First pass and redo each invoked both. In the redo, the plan (owner ruling Q4) overrides the skill's upsert line; skill gap logged |
| 7 | domain-patterns, tdd | |
| 8 | service-infra-conventions, tdd | |
| 9 | None | Plan: no skill (gap); TDD no |
| 10 | tdd | Plan: no pattern skill (gap); TDD yes. Some refusal-branch tests were born green; the mutation check in the step block shows their teeth |
| 11 | None | Plan: no skill (gap); TDD no |
| 12 | None | Plan: no skill (gap); TDD no |
| 13 | None | Plan: no skill (gap); TDD no |
| Fix round 1 | None — deviation | tdd, error-handling (row 15, step 5), persistence-patterns (row 11, step 6) not invoked through the Skill tool; the red-first record in the Fix Round 1 block is the tdd evidence |
| Fix round 2 | central-package-management (step 2), tdd (steps 3, 4, 5, 6, 10; row 2), error-handling (step 5), persistence-patterns (step 6) | Invoked through the Skill tool; fix round 1's work checked against each — one test added (tdd retro-fit survivor), no other change |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| `System.ServiceModel.Primitives` pin is in no block's graph on .NET 10 | low | architect | `dotnet list Blocks.slnx package --include-transitive` lists no ServiceModel. protobuf-net.Grpc 1.2.2's net8.0 dependency group has no ServiceModel; it came only through protobuf-net 2.4.8, which the protobuf-net pin replaces | Kept as the plan says, with `Blocks="Blocks.AspNetCore;Blocks.FastEndpoints"`; could be dropped |
| `MapToConstructor` doc claimed "the constructor with the most parameters"; the code uses `GetConstructors().First()` | low | reviewer | `src/Blocks.Core/Mapster/Extensions.cs:8-12` | Doc corrected to match the code; behaviour unchanged (out of scope). Possibly a latent bug in the reference |
| `IRouteProvider.GetRouteValue` now returns `string?` (warning fix) | low | architect | `src/Blocks.AspNetCore/HttpContextProvider.cs:11` | The spec's public-names row says "`GetRouteValue(key)`, which returns text"; it is nullable text now. Step 12 lists it |
| The commented-out-code rule (plan step 13 definition) misses fluent `.Method()` lines and lines ending in `,` or `(` | low | reviewer | `DbContextExtensions.DomainEvents.cs` (2 lines) and `HasuraRegistration.cs` `RefitSettings` block, removed by reading in step 4 | Future commented-out code of those shapes will not fail the gate |
| Block files are LF; the Hasura raw SQL string now has LF line breaks | low | reviewer | `src/Blocks.Hasura/HasuraMetadataService.cs:16-26` | Whitespace inside SQL only |
| `TryReseedTable` sends the table name as a query parameter | low | architect | `src/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.Seed.cs` — `ExecuteSql($"DBCC CHECKIDENT({tableName}, RESEED, 0)")` | The same shape as A4; SQL Server may accept a variable there, and a failure is swallowed (`Console.WriteLine`). Out of version one's scope; the read-me marks it SQL Server only |
| The error mapper keeps the reference's development 500 message (the thrown text) | low | reviewer | `GlobalExceptionMiddleware.cs` `HandleExceptionAsync`; test `ServerErrorInDevelopment_CarriesDetails` | The plan words the fixed text "outside development"; sprint-rituals uses the fixed text always |
| `Blocks.Messaging` keeps narration comments (`// Configure RabbitMQ connection`, `// Default to localhost`…) | low | reviewer | `src/Blocks.Messaging/MassTransit/DependencyInjection.cs`, `RabbitMqOptions.cs` | Step 4's rules (markers, commented-out code) do not catch narration; not touched in this slice |
| `tools/Blocks.Sync/Git.cs` repeats the snapshot tool's process wrapper | low | reviewer | `tools/Blocks.ForeignSnapshot/Git.cs` vs `tools/Blocks.Sync/Git.cs` | Kept separate so each tool stays a standalone project; a shared project would be new structure |
| Two `ThreadSafeMemoryCache.GetOrCreate` parameters show `T?` → `T` in the surface diff with unchanged source | low | reviewer | `docs/public-names.md` note 1; `src/Blocks.Core/Cache/ThreadSafeMemoryCache.cs` | Changed together with step 2's return-type annotation on the same two methods; the cause in the compiler's nullable metadata was not confirmed |
| `Containment.Inside`'s starts-with check cannot be reached by any input that passes `CheckRelative` | low | reviewer | mutation survivor, step 10 | Kept as the terminal guard (EQUIVALENT by construction) |
| `Blocks.Core.Guard` holds one extension method (`ThrowIfFalse(this bool …)`) beside its guard helpers | low | architect | the naming check counts classes whose public methods are all extensions; `GuardExtensions` already exists | Not renamed in fix round 1; moving `ThrowIfFalse` would change a public member not on the fix list |
| The ServiceModel pin removal (Q6) is not yet exercised by the portability check | low | reviewer | `Directory.Packages.props`; `tests/Blocks.Portability.Tests` | Not run in fix round 1; the close gate's suite covers it |
| The sync tests and the portability check run git and `mklink` from the test process; the junction and hard-link tests use `cmd /c mklink` on Windows | low | reviewer | `tests/Blocks.Sync.Tests/SyncFixture.cs` | Non-Windows uses `Directory.CreateSymbolicLink` / `ln`, not run here |

## Deviations from Plan
- Step 2: the working-tree block files are LF instead of the reference's CRLF (see Carry-Over).
- Step 4: removed the `[Course.AdvancedC#]` markers and two to-do-like comments beyond the plan's three marker words (spec item 5), and corrected the `MapToConstructor` doc text to match the code.
- Step 6 first pass:
  - The D3 red-first could not be shown on EF Core 10 (Q4).
  - The owner then kept the reference's line; the redo block records the plan's shadow-property test, red first.
  - In the redo, the persistence-patterns skill's upsert line is not followed, per the plan.
- Step 10: some refusal-branch tests were written after their code (born green); a recorded mutation check (~4 minutes, not named by the plan) shows each one fails when its guard is removed.
- Step 13: the suppression test skips Markdown files, and every hygiene test skips `.claude` folders (Decisions above).
- Fix round 1: `Guard` not renamed; double blank lines collapsed in the files whose comments were removed; the round's mapped skills were not invoked through the Skill tool.
- Fix round 2: none from the plan; the skill checks changed no code, one test added.

*Status: COMPLETE — developer, 2026-10-08 (fix round 2 applied)*
