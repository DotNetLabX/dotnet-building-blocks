# F3-GatewayFeedback — Codex critic record

## Verdict: NO-GO

Round 1, spec review, 2026-10-11. Job task-mv3k7phw-49erx2, read-only, persisted by the architect main session from the job's collected output (verbatim below).

**NO-GO — Round 1.** Found 3 HIGH, 8 MEDIUM and 1 LOW issues. No CRITICAL findings.

All findings concern [docs/specs/F3-GatewayFeedback/definition/spec.md](D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/definition/spec.md); line numbers below refer to that file. This was a static, read-only review. No files were written and no build or tests were run.

1. **HIGH — lines 34, 65–66, 117: C3 has contradictory logging requirements.** C3 requires logging after a response starts “on every branch,” while the decision says errors mapped below 500 remain unlogged. F1 D1 deliberately avoids treating client-aborted cancellation as a server failure; existing cancellation tests assert no error log. Specify the policy for started-response validation errors, HTTP 4xx errors and client cancellation. The acceptance criterion must cover these branches and verify unchanged body and headers as well as status.

2. **HIGH — lines 40, 53–54: C9 moves four public types, not two.** The two files declare `GrpcClientRegistrationExtensions`, `GrpcServicesOptions`, `GrpcServiceSettings` and `GrpcRetrySettings`. Moving the files changes all four namespaces, exceeding Rule 3’s stated two-type exception. Name all four types, their destination namespace and their public-name rows. C9’s structural checks also do not prove that the three registration methods retain their lifetimes, retry behavior and certificate handling.

3. **HIGH — lines 40, 75–77: C9 omits the transitive serializer pins.** `Directory.Packages.props:43–44` associates `protobuf-net` and `protobuf-net.Core` 3.2.56 with `Blocks.AspNetCore;Blocks.FastEndpoints`. `tools/Blocks.Sync/PackageReport.cs:31–35` selects pins using those block names. Without transferring ownership to `Blocks.Grpc`, an app taking that block alone will not receive the pins and can resolve protobuf-net 2.4.8. The solution’s central pins conceal this; the portability test checks only successful compilation. Require correct pin ownership and verify the copied block’s package report and resolved versions.

4. **MEDIUM — lines 32, 61, 130–138: C1’s save scope and mechanism remain unresolved.** Both event interceptors implement only asynchronous save callbacks. “Every aggregate on save” therefore leaves synchronous `SaveChanges()` ambiguous. Selection also depends on the non-generic marker: `AggregateRoot<int>` and `AggregateTenantEntity` do not implement `IAggregateRoot`, despite having integer keys. Define the supported save paths and cover these aggregate forms alongside Guid keys. Add the mechanism for selecting events independently of key/audit types to “Handed to the plan,” preserving the existing integer interface contract.

5. **MEDIUM — lines 53–54, 62–64, 69–71: The acceptance criteria do not prove Rule 3’s compatibility promise.** A legacy aggregate compiling and saving does not exercise existing `IClaimsProvider` implementations, enum-based `IAuditableAction<TActionType>` commands, audit-configuration subclasses or design-time factories that override only `ConfigureProvider`. C6 must also preserve zero-argument converter method-group uses, which adding an optional parameter can break. Require a representative legacy consumer compilation check and verify that nullable numeric audit members retain their existing meaning.

6. **MEDIUM — lines 37, 70–71, 136: C6 leaves the public comparer attachment contract unclear.** The two `BuildJson…Convertor` methods return only `ValueConverter`; four `HasJson…Conversion` helpers install converters. EF attaches comparers through property configuration, so deciding the comparer algorithm alone does not establish how factory-only callers obtain and install it. Specify which existing entry points gain options and comparer support. Acceptance should use those public paths, verify both JSON reading and writing, preserve default serialization, and read the mutation back through a fresh context. [EF Core comparer documentation](https://learn.microsoft.com/en-us/ef/core/modeling/value-comparers)

7. **MEDIUM — lines 38, 72, 119: C7’s criterion does not prove grouping or preservation of outer fields.** A single-field example can pass while duplicate failures for one field throw or overwrite messages. It also permits changing the message, status, trace ID or development details despite the explicit decision. Require several failures sharing a field, another field, and assertions that the existing outer fields and messages remain unchanged.

8. **MEDIUM — lines 39, 73–74, 137: C8 relies on nonexistent tests and leaves half its change unproved.** There are no `UseCustomFastEndpoints` tests; the FastEndpoints tests cover only event publishing and use `MapFastEndpoints`. The criterion tests the standalone reply builder but never the standalone enum converter or the bundle’s preserved prefix and behavior. Add those checks. The handoff must also settle how the independent settings compose within FastEndpoints configuration; method names alone do not resolve that mechanism.

9. **MEDIUM — lines 41, 78, 137: C10’s acceptance criterion establishes invocation, not behavior.** Adding an extension does not prove that it handles an exception, and “runs the asynchronous migrate” does not require any migration to be applied. Require an error request through the extension and observable migration effects/history through the async helper, while retaining the synchronous helper. Hand over the async method’s signature, cancellation contract and scope-disposal mechanism; only the middleware extension’s name is currently listed.

10. **MEDIUM — lines 42, 79–80: Documentation updates omit facts made false by this feature.** `src/Blocks.AspNetCore/README.md:8` explicitly documents argument errors as 400. `src/Blocks.FastEndpoints/README.md:18–19` attributes protobuf pins to AspNetCore, and the root README assigns gRPC registration to that block. `docs/public-names.md:119` documents validation errors as array items. C11’s specified corrections and its acceptance criterion do not require these statements to be reconciled with C4, C7 and C9.

11. **MEDIUM — lines 51–52, 80, 130–138: “Public-surface reference” has no defined artifact or comparison basis.** The existing `F1-VersionOne/delivery/surface-reference.txt` is the historical reference-project dump identified in `docs/public-names.md`, not a dump of the updated blocks. “Regenerated to match” could overwrite that baseline. Identify the intended current-surface artifact and comparison baseline, preserve the historical reference, and include this mechanism in the handoff. The acceptance list currently checks name rows but not the regenerated surface.

12. **LOW — lines 44–46: The duplicate-key rationale makes a false exclusivity claim.** Unique-key failures are not limited to simultaneous saves. `RepositoryBase.AddAsync`, `AddRangeAsync` and updates do not automatically perform uniqueness checks; `EnsureNotExistsOrThrowAsync` checks only an explicitly supplied integer ID. Sequential duplicate inserts or updates can also reach the database constraint. Preserve the owner’s explicit drop, but correct the “only two simultaneous saves” explanation.

Codex session ID: 01a12a12-8380-7c42-a921-e77474c83d45
Resume in Codex: codex resume 01a12a12-8380-7c42-a921-e77474c83d45


## Verdict: NO-GO

Round 2, spec review (delta read of the round-1 fold), 2026-10-11. Job task-mv3q3qvp-8fr4ch, read-only, persisted by the architect main session from the job's collected output (verbatim below).

**NO-GO — two MEDIUM findings, both tagged `fold`.** R1-01 remains partial; the other 18 findings are closed, with collateral noted below.

- **R2-01 — MEDIUM — fold — [spec.md:154](D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/definition/spec.md:154):** Decisions lines 154–155 prescribe logging and aborting started replies without exempting client cancellation. This contradicts C3(a), Acceptance C3 and Answers (lines 45, 81, 121–122), which require cancellation to remain unlogged and left alone. Following the new Decisions text breaks the earlier D1 cancellation guarantee. R1-01 is therefore only partially closed.

- **R2-02 — MEDIUM — fold — [spec.md:83](D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/definition/spec.md:83):** Requiring the existing not-started tests to pass unchanged contradicts C4 and C7. The current tests expect `ArgumentException` to return 400 ([test line 19](D:/src/dotnet-building-blocks/tests/Blocks.AspNetCore.Tests/GlobalExceptionMiddlewareTests.cs:19)) and validation `errors` to be an array ([test line 189](D:/src/dotnet-building-blocks/tests/Blocks.AspNetCore.Tests/GlobalExceptionMiddlewareTests.cs:189)). Those assertions must change for the required 500 and per-field map. Acceptance C3 needs explicit exceptions for these intentional changes.

All closure lines below refer to [spec.md](D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/definition/spec.md).

| Round 1 finding | Closure | Line(s) | Collateral |
|---|---|---|---|
| R1-01 | partial | 45, 81–83, 154–155 | R2-01 |
| R1-02 | closed | 45, 82–83, 155 | R2-02 |
| R1-03 | closed | 51, 53, 99–100 | none |
| R1-04 | closed | 51, 59, 65, 98–100 | none |
| R1-05 | closed | 49, 90–92, 183 | none |
| R1-06 | closed | 49, 90–92, 158 | none |
| R1-07 | closed | 43, 73–75, 156, 173–174 | none |
| R1-08 | closed | 44, 64–66, 76–80, 175–178 | none |
| R1-09 | closed | 48, 87–89, 181–182 | none |
| R1-10 | closed | 58–59, 93–95, 189–190 | none |
| R1-11 | closed | 101–103, 184 | none |
| R1-12 | closed | 53, 104–105 | none |
| R1-13 | closed | 60–63, 109–110 | none |
| R1-14 | closed | 34–35, 54, 119, 138–139, 167 | none |
| R1-15 | closed | 173–190 | none |
| R1-16 | closed | 187–188 | none |
| R1-17 | closed | 27–35 | none |
| R1-18 | closed | 136–138 | none |
| R1-19 | closed | 121–128, 168 | R2-01 |

No other MEDIUM-or-higher contradiction or earlier-guarantee break was found within the requested fold delta.

Codex session ID: 01a12aa9-8a3f-70f3-8a40-2ff972729043
Resume in Codex: codex resume 01a12aa9-8a3f-70f3-8a40-2ff972729043



<!-- Plan review, round 1 — 2026-10-13 — Codex job task-mv6lxxyu-rkzd6s (read-only), verbatim collected output -->

## Verdict: NO-GO

1. **HIGH — C12 excludes supported repository saves.** [plan.md:319](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:319) translates errors only in `ApplicationDbContext<T>`. However, [Repository.cs:93](/D:/src/dotnet-building-blocks/src/Blocks.EntityFrameworkCore/Repositories/Repository.cs:93) saves any `DbContext`, and existing repository tests use that supported arrangement. Those saves still answer 500 for recognised duplicates, contrary to C12. **Fix:** reuse the recogniser in the repository save path and test a repository backed by an ordinary context.

2. **HIGH — The event-member move violates Rule 3.** [plan.md:137](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:137), explicitly accepted at [plan.md:412](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:412), breaks consumers explicitly implementing `IAggregateRoot<TKey>.DomainEvents`, `AddDomainEvent`, or `ClearDomainEvents`. Naming the break does not make it an allowed exception. **Fix:** retain the original declarations, provide forwarding implementations for the new interface, and add an unchanged explicit implementation to step 1’s compatibility fixtures. The rejected “declare them on both interfaces” alternative is needed to preserve compatibility. [C# interface rules](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/interfaces).

3. **HIGH — C12’s tests do not meet its acceptance criterion.** [plan.md:328](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:328) substitutes a locally declared SQL exception and a generic `DbException` for actual provider exception types. The spec permits constructed errors **of the provider’s type**, not lookalikes. The tests also stop at exceptions rather than checking the required HTTP results. **Fix:** use test-only provider references to construct real exceptions; verify duplicate saves produce the fixed 409 reply and another save failure produces 500.

4. **HIGH — Step 4 misstates the mediator’s existing integer semantics.** [plan.md:185](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:185) says the integer path remains unchanged while prescribing preservation of an assigned ID. [AssignUserIdBehavior.cs:14](/D:/src/dotnet-building-blocks/src/Blocks.MediatR/Behaviors/AssignUserIdBehavior.cs:14) currently overwrites an assigned integer ID whenever a claim exists. Applying the planned preservation rule to that path introduces an additional behavior change. **Fix:** distinguish legacy integer behavior from the new typed path and characterize prepopulated integer commands before changing the behavior.

5. **MEDIUM — The FastEndpoints probe states the wrong `AddError` contract.** [plan.md:47](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:47) treats `AddError("Address.StreetName", …)` as a property error. FastEndpoints 8.3.0’s inspected metadata identifies that overload as `AddError(string message, string? errorCode, …)`: it creates a general error. Moreover, the scratch probe’s invalid request triggers automatic validation before that handler executes. **Fix:** use a `ValidationFailure` or property-expression overload, ensure the handler runs, and correct the probe evidence. [FastEndpoints implementation](https://raw.githubusercontent.com/FastEndpoints/FastEndpoints/main/Src/Library/Endpoint/Endpoint.Validation.cs).

6. **HIGH — Removing the mediator constraint needs a replacement.** [plan.md:188](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:188) removes `where TRequest : IAuditableAction` without specifying `where TRequest : notnull`. MediatR 12.5.0 requires `notnull`; leaving `TRequest` unconstrained produces CS8714 and fails this repository’s warnings-as-errors build. **Fix:** specify the replacement constraint explicitly. [MediatR 12.5.0 contract](https://raw.githubusercontent.com/jbogard/MediatR/v12.5.0/src/MediatR/IPipelineBehavior.cs).

7. **HIGH — The unconstrained open behavior introduces a dependency for ordinary requests.** [plan.md:188](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:188) makes the behavior eligible for every request while retaining its required `IClaimsProvider` constructor dependency. Previously, DI skipped the constrained behavior for unaudited requests. A previously working ordinary request can now fail during pipeline construction when no claims provider is registered. **Fix:** require claims only for supported audit requests, preserve existing construction calls, and test an ordinary request with no claims-provider registration. [.NET 10 DI implementation](https://raw.githubusercontent.com/dotnet/runtime/release/10.0/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs).

8. **MEDIUM — PostgreSQL recognition is broader than C12 allows.** [plan.md:325](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:325) recognises *any* `DbException` reporting `23505`, although C12 requires other databases to remain unchanged. The planned generic exception fixture reinforces that unintended scope. **Fix:** identify the PostgreSQL provider exception as well as its code, without a production provider reference, and add a negative test for an unrelated exception reporting the same state.

9. **MEDIUM — C6 leaves a known converter-type mismatch unaddressed.** [plan.md:298](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:298) includes both `HasJsonListConversion` forms but does not address [BuilderExtensions.cs:72](/D:/src/dotnet-building-blocks/src/Blocks.EntityFrameworkCore/Extensions/BuilderExtensions.cs:72). Its complex-property overload configures a property of type `T` with a converter whose model type is `IReadOnlyList<T>`. Adding a comparer does not repair that mismatch; EF rejects it. The planned tests can all use another helper and miss this. **Fix:** align the converter and comparer with the property’s actual type, and exercise this overload with both default and supplied options. [EF Core converter validation](https://raw.githubusercontent.com/dotnet/efcore/release/10.0/src/EFCore/Metadata/Internal/Property.cs).

10. **MEDIUM — The async save override needs its inherited optional argument preserved.** [plan.md:320](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:320) lists the new override without the token’s default. EF Core 10.0.12’s existing overload has `CancellationToken cancellationToken = default`. Omitting that default breaks version-one calls such as `context.SaveChangesAsync(false)` through the derived context type. **Fix:** explicitly preserve the optional token and pin that call in the compatibility project. [EF Core save overloads](https://raw.githubusercontent.com/dotnet/efcore/release/10.0/src/EFCore/DbContext.cs).

11. **MEDIUM — C3’s tests can miss the separate validation branch.** [plan.md:214](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:214) tests started replies for not-found and unexpected errors, but validation uses a separate catch and writer in [GlobalExceptionMiddleware.cs:45](/D:/src/dotnet-building-blocks/src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs:45). Fixing only the general writer would pass the planned tests while validation still writes after response start. **Fix:** add a started-response validation test asserting Warning logging, one abort, unchanged output, and no handler exception.

12. **MEDIUM — C11’s acceptance checks allow required documentation to remain wrong.** [plan.md:349](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:349) relies on hygiene tests that check headings, seeding terminology, and sync instructions. They do not check most C11 corrections. The FastEndpoints pin check is also line-dependent: its existing pin statement spans [README.md:18](/D:/src/dotnet-building-blocks/src/Blocks.FastEndpoints/README.md:18) and the following line, so no single line contains both the pin wording and `Blocks.AspNetCore`. **Fix:** add focused assertions covering the required statements and inspect complete paragraphs rather than individual lines.

13. **LOW — The compatibility project’s immutability check lacks a baseline.** [plan.md:130](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:130) uses a final folder diff to establish that fixtures were never edited. A diff of newly created files cannot distinguish their original contents from later edits; unstaged untracked files are absent from `git diff` entirely. **Fix:** capture step-1 contents or hashes and compare every fixture against that baseline afterward.

The build slices cover steps 1–11 once, in order, and keep coupled changes together. Review was read-only: no files modified and no builds or tests run.

Codex session ID: 01a1350f-2d5e-78a2-b77d-0a45806a5ef3
Resume in Codex: codex resume 01a1350f-2d5e-78a2-b77d-0a45806a5ef3


<!-- Plan review, round 2 — 2026-10-13 — Codex job task-mv6wjvu4-rnvp36 (read-only), verbatim collected output -->

## Verdict: NO-GO

| id | closed/partial/not closed | plan line | collateral |
|---|---|---|---|
| R1-01 | closed | 277–282: one hosted setup; isolated split-method tests | none |
| R1-02 | closed | 222–239: distinct integer rules preserved and tested | none |
| R1-03 | closed | 227: replacement `IAuditableRequest` constraint | none |
| R1-04 | closed | 227–239: ordinary requests skipped; no-claims-reader test | none |
| R1-05 | closed | 164–174: old declarations retained, forwarding bodies, explicit-implementation test | none |
| R1-06 | closed | 383–386: base-context and repository saves translate | none |
| R1-07 | partial | 399–409: real provider types; HTTP results still inferred | none |
| R1-08 | closed | 291–301: nested conversion follows ruling 5 | none |
| R1-09 | closed | 51–55, 309–310: probe corrected; validator supplies failures | none |
| R1-10 | closed | 390–393, 405–406: provider identity required; negative test | none |
| R1-11 | closed | 360–371: converter repaired; both options paths tested | none |
| R1-12 | closed | 150–151, 385: optional token retained and compatibility call pinned | none |
| R1-13 | closed | 261–263: validation after response start tested | none |
| R1-14 | partial | 430–435: whitespace folded; several corrections remain unchecked | none |
| R1-15 | closed | 426–428: README states nested settings lack startup checks | none |
| R1-16 | closed | 154–158: SHA-256 baseline and final comparison | none |
| R1-17 | closed | 189–190, 419: reference-type limitation documented | none |
| R1-18 | closed | 325, 329, 339: plain move, specific test, unstaged comparison | none |
| R1-19 | closed | 187, 231, 328: initialization and framework references specified | none |
| R1-20 | closed | 456–460: feedback channel and versioned filename | none |
| R1-21 | closed | 350–353: explicit constructor lookup reaches intended error | none |

Collateral findings at MEDIUM or above: **none** in Context, Domain Model Changes, Skill Mapping, each of Steps 1–12, Must-NOT-Change, Testing Strategy, Decisions, or Plan Review; also none in spec rulings 5–6, the C8 row, Acceptance C8, or Answers “For every app.” The two remaining findings below continue round-1 issues.

No new guarantee break was identified in the reviewed delta against Rule 3 source compatibility, the owner’s rulings, or the Must-NOT-Change rows.

1. **MEDIUM — R1-07 remains partial: save-to-HTTP acceptance is missing.** [plan.md:407](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:407) substitutes existing status-mapping tests for HTTP assertions on failed saves. `EachErrorType_AnswersItsStatus` constructs standalone exceptions and checks only status; its 500 case uses `InvalidOperationException`, not an actual nonduplicate save failure. Real provider fixtures close the exception-type gap, but the required fixed 409 reply and unchanged 500 result remain untested. **Fix:** pass failing base-context and repository saves through a middleware host; assert 409, the fixed message, absence of provider text, and 500 for a nonduplicate `DbUpdateException`.

2. **MEDIUM — R1-14 remains partial: README checks still accept incorrect statements.** [plan.md:430](/D:/src/dotnet-building-blocks/docs/specs/F3-GatewayFeedback/delivery/plan.md:430) checks mostly keyword presence. These assertions permit a Core README that incorrectly attributes startup validation to `ConfigureOptionsFromSection`, or a Grpc README that falsely promises nested validation. They also omit the nested-name change, provider/design-package responsibility, and each audit step’s rule. **Fix:** assert the required behavior statements and reject their obsolete counterparts on normalized paragraphs.

Below MEDIUM: Step 11 line 444 calls the replaced constraint “removed”; Decisions line 511 refers to three new projects although Testing Strategy lists four.

Codex session ID: 01a1361f-f582-7b53-911e-54e3637458bd
Resume in Codex: codex resume 01a1361f-f582-7b53-911e-54e3637458bd
