# F3-GatewayFeedback — critic record

## Round 1 — 2026-10-11 — spec — primary (codex)

Header note: the primary's verdict (NO-GO, 3 HIGH · 8 MEDIUM · 1 LOW) is persisted verbatim in
`review-critic-codex.md` § Verdict: NO-GO (job task-mv3k7phw-49erx2); it cites no anchor, so its rows below carry
`unranked — no anchor fits; Codex severity {level}`. Both readers ran (pair picked: codex + sonnet). The second
reader's message was truncated on delivery and recovered from its transcript with the salvage script; its own
headings are demoted one level below.

## Second reader — round 1 (sonnet)

### F3-GatewayFeedback — Critic Review (second reader, round 1)

#### Mode: Spec Review
#### Verdict: REVISE

Two HIGH findings, no CRITICAL. The sections on knowledge-gateway's findings and on the owner's rulings hold up. The gaps are in the details of C7, C9, C3 and C2, and in the tests and docs that surround them.

#### Pre-commitment Predictions
- **C2 type design** (arity, nullability, compatibility). Found: a nullability trap in the one-parameter forms, an interface-implementer break, and a DI break for the open-generic behavior.
- **C7 "one shape" claim.** Found: the key casing differs between the two writers.
- **C9 move side effects.** Found: the pins' `Blocks=` annotation, read-mes, namespace and test lists are not covered.
- **C3 semantics.** Found: the log scope contradicts ruling D1, and swallowing a failed stream hides it from the client.
- **"Handed to the plan" completeness.** Found: C1's selection mechanism and several others are missing from the list.
- **Not predicted.** The acceptance line for C8 cites tests that do not exist.

#### Coverage ledger
Round: 1 · Depth: mechanism · Bar: product completeness, applied as a mechanism check against live code (technical feature, no product doc) · Ruler: seed only (`docs/critic-calibration/ruler.md` does not exist in this repo) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Source points 1–14 against C1–C11 and Dropped | `source-feedback.md`; `spec.md` § What changes, Answers | COVERED (see Open Questions for the residue) |
| C1 events from any key type | `DbContextExtensions.DomainEvents.cs`; `AggregateRoot.cs`; `AuditedEntityConfiguration.cs` | PARTIAL (mechanism not handed over, F-M4) |
| C2 audit who-type | `IAuditedEntity.cs`; `AggregateRoot.cs`; `IAuditableAction.cs`; `IClaimsProvider.cs`; `HttpContextProvider.cs`; the three Assign steps; `AuditedEntityConfiguration.cs`; `TenantEntity.cs`; MediatR and FastEndpoints read-mes | PARTIAL (F-M3, F-M4) |
| C3 and C4 error handler | `GlobalExceptionMiddleware.cs`; `GlobalExceptionMiddlewareTests.cs`; `ErrorMapperHost.cs`; F1 spec D1 | PARTIAL (F-M1, F-M2) |
| C5 design-time factory | `DbContextDesignTimeFactory.cs`; `ApplicationDbContext.cs` | COVERED |
| C6 stored-JSON converters | `BuilderExtensions.cs` | COVERED (F-L6) |
| C7 and C8 validation reply and endpoint set-up | `Blocks.FastEndpoints/Extensions.cs`; `FastEndpoints.xml`; a scratch run of `ValidationOptions`; `ValidationBehavior.cs`; `tests/Blocks.FastEndpoints.Tests` | PARTIAL (F-H1, F-M5) |
| C9 new `Blocks.Grpc` block | `src/Blocks.AspNetCore/Grpc/*`; the csproj; `Directory.Packages.props`; `PackageReport.cs`; hygiene and portability tests; `Blocks.slnx`; F1 `plan.md` | PARTIAL (F-H2, F-L1, F-L2) |
| C10 `Use…` extension and async migrate | `DbContextExtensions.cs`; `NamingTests.cs` | COVERED |
| C11 read-mes | The AspNetCore, Core, EF, FastEndpoints, Domain and MediatR read-mes; root `README.md`; `ConfigurationExtensions.cs` | PARTIAL (F-M7, F-L7) |
| Rules 1–4 and the acceptance list | `docs/public-names.md`; `surface-reference.txt` header; the F1 chain that built it | PARTIAL (F-M6) |
| Owner rulings | knowledge-gateway `questions.md` Q22–Q28; F201 spec; F1 spec D1–D3 | COVERED (the 2026-10-11 rulings have no file, F-L4) |
| Not opened | `LoggingBehavior`; EF repository, tenant and seed code; `EnumEntityConfiguration`; `MetadataConfiguration`; `Blocks.Sync` tests; `ForeignSnapshot` | NOT OPENED — no C1–C11 change touches them |

#### Findings

#### [HIGH] F-H1 — C7 does not say how the per-field keys are cased
**Anchor:** seed-0007 · above seed-0022 · below seed-0002
**Band:** HIGH–MEDIUM; driver: whether the architect treats key casing as a plan detail.
**Shape:** two-writer shape, key casing unspecified
**Consequence:** The handler's per-field map keys come out PascalCase while the endpoint library's are camelCase, so form fields miss their messages.
**Evidence:** quoted, path and scenario.
- C7 says the handler should write "the shape the endpoint-library set-up already writes".
- FastEndpoints' `ValidationOptions.UsePropertyNamingPolicy` converts FluentValidation property names with the app's JSON naming policy (`FastEndpoints.xml:732`). I ran a scratch program against FastEndpoints 8.3.0 and its default is `True`. The endpoint library therefore writes `errors: { "name": [...] }`.
- The handler uses `FluentValidation` property names as they are ("Name"). Its serializer sets only `PropertyNamingPolicy` (`GlobalExceptionMiddleware.cs:17`), which does not touch dictionary keys.
- The existing test pins `errors[0].propertyName == "Name"` (`GlobalExceptionMiddlewareTests.cs:192`).
- A literal port gives `{"Name": [...]}`. That passes the C7 line "per-field map of message arrays", and it is a second shape on the wire for the same failure.

**Surfaces:** `spec.md` § C7, Acceptance C7; `Blocks.FastEndpoints/Extensions.cs:15-33`; `GlobalExceptionMiddleware.cs:86-104`
**Source:** the owner's "one shape" ruling. **Issue:** nothing fixes key casing, nested names (`Address.City`, `Items[0].Name`) or collisions. **Impact:** the point of C7 (one shape a Vue form library takes directly) is defeated silently.
**Fix:** state in C7 that keys follow the same naming policy as the endpoint library (camelCase). Make the acceptance test assert a camelCase key, and add the same for a nested name. Keep the endpoint library and the handler pinned to one fixture so they cannot drift.

#### [HIGH] F-H2 — C9 leaves the protobuf pins and their `Blocks=` annotation pointing at the old blocks
**Anchor:** seed-0008 · above seed-0022 · below seed-0002
**Band:** HIGH–MEDIUM; driver: whether the plan author remembers the annotation unprompted. Silent-wrong-output class, so it is not downgraded.
**Shape:** move leaves a derived list stale
**Consequence:** An app taking the new gRPC block is never told to pin protobuf-net, so restore picks 2.4.8 under a 3.x-only client, and the pin list names the wrong blocks.
**Evidence:** quoted and path.
- `Directory.Packages.props:43-44`: `protobuf-net` and `protobuf-net.Core` carry `Blocks="Blocks.AspNetCore;Blocks.FastEndpoints"`.
- `tools/Blocks.Sync/PackageReport.cs:31-36` adds a pin to an app's needed list only if one of the taken blocks is named in the annotation.
- F1 `plan.md:42-43`: "With transitive pinning off, restore resolves protobuf-net 2.4.8… the reference pins 3.2.56". `plan.md:408` records the annotated pins as a decision.
- Both blocks reached the pins only through `Blocks.AspNetCore → protobuf-net.Grpc`. After C9 neither does, and `Blocks.Grpc` is the only block that needs them.
- Nothing fails in the repo. The solution sees all pins centrally. The portability probe compiles with or without the pins. No test reads `Blocks=`.
- C9 and C11 never mention the props file. The AspNetCore read-me lines 23-25 and the FastEndpoints read-me "Pinned… (through `Blocks.AspNetCore`)" become false.

**Surfaces:** `spec.md` § C9, C11, Acceptance C9; `Directory.Packages.props`; `PackageReport.cs`; the AspNetCore and FastEndpoints read-mes
**Source:** F1 plan decision on annotated pins. **Issue:** not in C9, C11 or the acceptance list. **Impact:** the first app to take `Blocks.Grpc` gets a wrong protobuf-net version with no signal.
**Fix:** add to C9 that the pins' `Blocks=` becomes `Blocks.Grpc` (drop `Blocks.FastEndpoints` and `Blocks.AspNetCore`). Add an acceptance line that the package report for `Blocks.Grpc` alone lists both pins, and that the report for `Blocks.AspNetCore` alone does not. Update both read-mes.

#### [MEDIUM] F-M1 — C3's "logs the error on every branch" contradicts ruling D1 and the Decisions row
**Anchor:** seed-0022 · above seed-0041 · below seed-0008
**Band:** MEDIUM–HIGH; driver: whether the cancelled-request branch should log when the reply has started.
**Shape:** two readings of one rule
**Consequence:** A client dropping a stream mid-reply could log an error-level fault on every disconnect, against the ruling that a cancelled request is no server failure.
**Evidence:** quoted.
- C3: "leaves a reply that has already started alone, on every branch, and logs the error".
- F1 D1 (`F1…/spec.md:54`): a cancelled request "was logged as a server failure" and is now 499.
- `Cancellation_Answers499` and `CancellationWrappedInAnotherError_Answers499` assert no error log.
- The Decisions row says errors mapped below 500 "stay unlogged".
- Scenario: a streamed answer, the client disconnects after byte 1, `RequestAborted` is set, and an `OperationCanceledException` arrives with `HasStarted` true. This is the most common case for the streams the spec exists for. Reading C3 literally logs at Error. Reading D1 and the Decisions row logs nothing.
- The same applies to a mapped 404 or 409 raised after the first byte. The caller cannot be told, so logging it is defensible, but the spec does not say so.

**Surfaces:** `spec.md` § C3, Acceptance C3, Decisions row 1; `GlobalExceptionMiddleware.cs:40-52`; `GlobalExceptionMiddlewareTests.cs:121-133`
**Fix:** state each case separately.
- Cancelled with the reply started: leave it alone and do not log.
- Mapped below 500 with the reply started: log, and say at which level.
- 500 and above with the reply started: log as today.

Add a test for the first case. The existing started-reply test checks status and body only.

#### [MEDIUM] F-M2 — C3 swallows a failed stream, so the client sees a normal end
**Anchor:** seed-0008 · above seed-0022 · below seed-0002
**Band:** MEDIUM–HIGH; driver: whether the streaming clients can tell a cleanly ended stream from a cut one. A silent-wrong-output candidate.
**Shape:** error hidden from the client
**Consequence:** A stream that fails after the first byte and is swallowed ends as a normal completion, so a cut-off answer reads as complete.
**Evidence:** scenario and reference.
- Acceptance C3 requires that "the handler does not throw". Today's accidental throw makes the server abort the connection, which the client sees as a broken stream.
- Returning normally lets the server finish with a clean terminating chunk.
- The framework's own exception middlewares rethrow when `HasStarted` is true, so the server aborts the connection.
- Log and rethrow, or log and `context.Abort()`, both satisfy the source ask ("leave a started reply alone and log the error"). The spec fixes neither, and the acceptance line pins the swallow reading.

**Surfaces:** `spec.md` § C3, Acceptance C3; `source-feedback.md` § 4; `GlobalExceptionMiddleware.cs:106-112`
**Fix:** decide it in the spec and name the client-visible result. Recommendation, confidence medium (it depends on how the gateway's clients detect an incomplete stream): log, then abort the connection, never end cleanly.

#### [MEDIUM] F-M3 — C2's "one-parameter forms keep today's meaning" can break silently, and the acceptance line would still pass
**Anchor:** seed-0130 · above seed-0041 · below seed-0008
**Band:** MEDIUM–HIGH; driver: whether the plan derives the one-parameter form from the two-parameter one.
**Shape:** compatibility claim without a proving test
**Consequence:** The one-parameter forms can silently turn the nullable last-modified-by into a required value, and the listed test still passes.
**Evidence:** scenario and path.
- Today `AggregateRoot<TKey>.LastModifiedById` is `TKey?` with `TKey : struct`, which is a `Nullable<TKey>` and a nullable column (`AggregateRoot.cs:24`, `IAuditedEntity.cs:11`).
- A who-type that can also be `string` cannot be constrained to `struct`. On an unconstrained `TWho`, `TWho?` is only an annotation, so for `int` it is a plain `int`.
- Deriving the one-parameter form from `<TKey, TKey>` therefore gives a non-nullable `LastModifiedById`: a required column, 0 instead of null. The acceptance line ("compiles and saves") passes.
- Two more compatibility traps sit under Rule 3. A new `IClaimsProvider` member breaks every app class that implements it, such as a test double. `AssignUserIdBehavior<,>` is registered as an open generic (`Blocks.MediatR/README.md`). A third type parameter cannot be closed by the container, so that step cannot simply gain one.

**Surfaces:** `spec.md` § C2, Rule 3, Acceptance C2, Handed to the plan; `AggregateRoot.cs`; `IClaimsProvider.cs`; `AssignUserIdBehavior.cs`
**Fix:** add an acceptance line that a one-parameter aggregate keeps a nullable last-modified-by and a nullable column. Add to the handed-over list: the nullability of the who-type, defaults on any new `IClaimsProvider` member, and the registration shape of each step.

#### [MEDIUM] F-M5 — Acceptance C8 relies on tests that do not exist, and the standalone converter has none
**Anchor:** seed-0310 · above seed-0041 · below seed-0022
**Band:** MEDIUM–LOW; driver: whether the plan writes characterization tests first.
**Shape:** acceptance line with no baseline
**Consequence:** The acceptance line leans on existing tests of the combined set-up that do not exist, so "unchanged" has no baseline and the standalone converter has no test.
**Evidence:** path.
- Acceptance C8: "the existing `UseCustomFastEndpoints` tests pass unchanged".
- `grep UseCustomFastEndpoints|ResponseBuilder|RoutePrefix` over `tests/` returns nothing. `Blocks.FastEndpoints.Tests` holds only `DomainEventPublisherTests.cs`.
- Rule 1 says every change C1–C10 has a test that fails without it. C8 splits two things (the reply builder and the enum converter), but only the reply builder has an acceptance test.
- Separately, C2's third step (the MediatR behavior) has no test project at all (`tests/` has none for MediatR). That project is a plan cost the acceptance line implies.

**Surfaces:** `spec.md` Acceptance C8, C2, Rule 1; `tests/Blocks.FastEndpoints.Tests`; the `tests/` listing
**Fix:** reword it as characterization tests written before the split (prefix, enum text, reply). Add an enum-converter test. Note the new `Blocks.MediatR.Tests` project.

#### [MEDIUM] F-M6 — Rule 2's "public-surface reference is regenerated" has no referent
**Anchor:** seed-0023 · above seed-0041 · below seed-0022
**Band:** MEDIUM–LOW; driver: whether anyone overwrites the file.
**Shape:** stale or ambiguous pointer
**Consequence:** A developer may overwrite the reference baseline dump, or leave the names list's counts and wire table stale after the validation shape changes.
**Evidence:** path.
- `docs/public-names.md:5-6` calls `docs/specs/F1-VersionOne/delivery/surface-reference.txt` the reference's own dump, taken at commit `fda2eb7`.
- The file's first lines carry the old namespace (`Blocks.AspNetCore | … | GlobalExceptionMiddleware`). It is the baseline the names list is diffed against. No committed dump of version one exists.
- Rule 2 has no acceptance line for its second half.
- `public-names.md:8-9` states "102 reference lines… 108 version-one lines… Each line belongs to exactly one row", which becomes stale. Its "error reply on the wire" table (`errors` with `propertyName`/`errorMessage`, lines 118-119) is changed by C7.

**Surfaces:** `spec.md` Rule 2, Acceptance; `docs/public-names.md`; `surface-reference.txt`
**Fix:** say that the baseline stays untouched. Run a fresh dump to a scratch path and diff it against the baseline. Update the header counts and the wire table, and add the acceptance line.

#### [MEDIUM] F-M7 — Read-mes beyond the three C11 names go stale
**Anchor:** seed-0023 · above seed-0041 · below seed-0022
**Shape:** documentation drift
**Consequence:** Read-me lines stay false after the change (argument errors as 400, pins, block table), so app teams are told the old behaviour.
**Evidence:** path.
- `Blocks.AspNetCore/README.md:7-9`: "400 for validation, argument, bad-request and domain errors". C4 changes this, and `validation replies add errors` changes under C7. The README registration line `app.UseMiddleware<…>` gains the C10 extension.
- The FastEndpoints README describes `UseCustomFastEndpoints()` as one call, and pins "through `Blocks.AspNetCore`". C8 and C9 change both.
- The Domain and MediatR read-mes describe `AggregateRoot<TKey>` and `AssignUserIdBehavior` (C2).
- The root `README.md` block table lists 11 blocks, with gRPC under `Blocks.AspNetCore`.
- Acceptance C11 reads "the three read-mes say what C11 lists".

**Surfaces:** `spec.md` C11, Acceptance C11; the read-mes named above
**Fix:** widen C11 to "every read-me a change makes false". Add the Blocks.Grpc read-me to its list.

#### [LOW] F-L7 — C11's Core wording ("bind without checking") is not what the code does
**Anchor:** seed-0127 · above seed-0041 · below seed-0023
**Shape:** inaccurate claim carried from the source
**Consequence:** A corrected read-me would state that two helpers check nothing, when both refuse a missing section.
**Evidence:** path.
- `ConfigurationExtensions.cs:29-30` (`ConfigureOptionsFromSection<T>` throws on a missing section) and `:36-39` (`GetSectionByTypeName<T>` throws via `Guard.AgainstNull`).
- They skip attribute validation only.

**Surfaces:** `spec.md` C11; `Blocks.Core/Configurations/ConfigurationExtensions.cs`
**Fix:** say "binds without attribute validation; still refuses a missing section".

#### [LOW] F-M4 — "Handed to the plan" is missing mechanisms the spec leaves open
**Anchor:** seed-0364 · above seed-0041 · below seed-0127
**Route:** plan must resolve
**Shape:** open mechanism not listed
**Consequence:** Open mechanisms stay unlisted, so the plan may settle public names or test scope by default.
**Evidence:** path.
- C1: how "any key type" is selected. `IAggregateRoot<TKey>` is generic with a `struct` constraint, so there is no non-generic base to filter on. A new public marker interface or reflection is needed, and C2 adds a second generic axis to the same problem.
- C9: the new namespace. It is not named, although the Answers promise "change one `using` line".
- C10: the asynchronous migrate's name and token. The list says only "C10's extension name".
- C2: the nullability and compatibility points in F-M3.

**Surfaces:** `spec.md` § Handed to the plan; `DbContextExtensions.DomainEvents.cs:13`; `AggregateRoot.cs:5,10`
**Fix:** add them to the list.

#### [LOW] F-L1 — C9 moves four public types, not two
**Anchor:** seed-0127 · above seed-0041 · below seed-0023
**Shape:** count mismatch
**Consequence:** Four public types move, so the names list and the "one using line" promise to apps are undercounted.
**Evidence:** path.
- Rule 3 says "C9 moves two types".
- `GrpcClientRegistrationExtensions.cs` holds one public type. `GrpcServicesOptions.cs` holds three: `GrpcServicesOptions`, `GrpcServiceSettings` and `GrpcRetrySettings`.
- The two files and "no other user" are confirmed.

**Surfaces:** `spec.md` Rule 3, C9; `src/Blocks.AspNetCore/Grpc/*`
**Fix:** say four types in two files.

#### [LOW] F-L2 — The portability check is already driven by the folder list
**Anchor:** seed-0127 · above seed-0041 · below seed-0023
**Shape:** misdescribed current code
**Consequence:** The plan item about a fixed block list is partly moot.
**Evidence:** path.
- `SnapshotFixture.Blocks()` enumerates `src/Blocks.*` (`SnapshotFixture.cs:44`), so it already covers 12.
- The fixed lists are in `NamingTests.BlockAssemblies`, the `HaveCount(11)` in `ReadMeTests.cs:17`, and the Hygiene csproj `ProjectReference` list. `NamingTests` loads assemblies by name, so the csproj reference is required.

**Surfaces:** `spec.md` Handed to the plan; the tests named above
**Fix:** restrict the item to the hygiene tests and the csproj.

#### [LOW] F-L3 — One sentence in C2's handed-over note is incoherent
**Anchor:** seed-0127 · above seed-0041 · below seed-0023
**Shape:** unclear wording
**Consequence:** The reader cannot tell which arity clashes with `IAuditableAction<TActionType>`.
**Evidence:** quoted.
- "cannot simply be a second generic of the same arity".
- The real clash is the non-generic `IAuditableAction` turning into `IAuditableAction<TWho>`, which collides with `IAuditableAction<TActionType>`.

**Surfaces:** `spec.md` § Handed to the plan; `IAuditableAction.cs:3,12`
**Fix:** restate it that way.

#### [LOW] F-L4 — The 2026-10-11 rulings have no file
**Anchor:** seed-0041 · above seed-0041 · below seed-0127
**Shape:** untraceable ruling
**Consequence:** Later readers cannot confirm C7, C9, the dropped duplicate-key point and the "fix here first" ruling.
**Evidence:** path.
- F3's `delivery/` holds only `lessons.md` and `reader-pair.md`. F1 cited numbered questions.
- The 2026-10-10 rulings (Q22–Q28) match what the spec says.

**Surfaces:** `spec.md` header, Decisions; the F3 `delivery/` folder
**Fix:** record them in a questions file or inline in the spec.

#### [LOW] F-L5 — The C7 title overclaims
**Anchor:** seed-0127 · above seed-0041 · below seed-0023
**Shape:** wording
**Consequence:** "One shape" holds only for `errors`; the outer fields and the message still differ.
**Evidence:** quoted and path.
- The Decisions row keeps each writer's outer fields.
- The endpoint library writes `statusCode`, `message`, `errors`. The handler adds `traceId` and `details`. The `message` text differs too ("Validation failed for multiple fields." against "One or more validation errors occurred.").

**Surfaces:** `spec.md` C7, Decisions; `Extensions.cs`; `GlobalExceptionMiddleware.cs`
**Fix:** retitle it "one shape for `errors`".

#### [LOW] F-L6 — Acceptance C6 is ambiguous about "in place"
**Anchor:** seed-0127 · above seed-0041 · below seed-0023
**Shape:** two readings
**Consequence:** The comparer choice decides whether a changed member of an item is saved, and the acceptance line does not say which is meant.
**Evidence:** quoted and path.
- "changes an item of a stored list in place". `BuildJsonReadOnlyListConvertor` is typed `IReadOnlyList<T>`.
- This could mean replacing an element, mutating a member of an element, or adding to the underlying list.

**Surfaces:** `spec.md` Acceptance C6; `BuilderExtensions.cs:46-77`
**Fix:** test both an element replace and a member mutation.

#### Gap Analysis
- **Rule 3 beyond C2.** C4 changes behaviour, which Rule 3 treats as out of scope since it speaks of compiling. C5's hook must be virtual, not abstract, or every derived factory breaks. C6 adds optional parameters to public static methods, which is source-compatible but not binary-compatible. The rule should say "source-compatible".
- **Blocks' own guards.** Core's `Guard.ThrowIfFalse` and the framework guards it wraps throw `ArgumentException` (`Guard.cs:20-23`). After C4 any app using them for user input answers 500. The Answers cover only the reference's domain guards.
- **Blocks.Grpc read-me.** The old read-me registration example read `GrpcServicesOptions` through `GetSectionByTypeName`, which never runs its `[Range]` and `[Required]` attributes (source § 13). The new read-me should show a checking form or say so.
- **The debug-build certificate switch.** It moves unchanged (Decision), so it keys off the consuming app's build configuration. That is the owner's call.

#### Open Questions
- F201's "request the client drops half-way" plan item (F201 § For the plan to settle) may have produced a handler fix. F3 neither carries it nor tells knowledge-gateway it was dropped. Confidence: low.
- The `…OrThrow…` repository helpers are `int`-only (`RepositoryExtensions.cs`). Guid-keyed aggregates (the subject of C1 and C2) get no helper. The ruling covered text keys, not Guid keys. Confidence: low that this is wanted.
- The "only a race reaches the duplicate key" reasoning for the dropped point rests on app code not read here. `EnsureNotExistsOrThrowAsync` checks the id only. It is the owner's call, so I record it and do not contest it.

Reviewed: D:\src\dotnet-building-blocks\docs\specs\F3-GatewayFeedback\definition\spec.md

### Merge — round 1

Coverage: complete — every baseline surface was opened by at least one reader (the second reader's unopened list
holds no baseline surface; the primary opened the repository and tenant code).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: the literal reading logs an error on every client disconnect of a stream | C3 logging contradicts D1 and the Decisions row | two readings of one rule | error log on every dropped stream | GlobalExceptionMiddleware.cs:40-52; F1 D1; cancellation tests | no | logging | spec C3, Decisions | codex-1, F-M1 |
| R1-02 | sonnet | MEDIUM | seed-0008 | C3 swallows a failed stream | error hidden from the client | a cut answer reads as complete | framework middlewares rethrow on HasStarted | no | streaming | spec C3, Acceptance C3 | F-M2 |
| R1-03 | codex, sonnet | HIGH | seed-0008 (codex unranked, HIGH) | C9 leaves the protobuf pins on the old blocks | move leaves a derived list stale | an app taking Blocks.Grpc resolves protobuf-net 2.4.8 | Directory.Packages.props:43-44; PackageReport.cs:31-36 | no | packaging | spec C9, C11 | codex-3, F-H2 |
| R1-04 | codex, sonnet | MEDIUM | contested: HIGH / LOW — settled MEDIUM: the move is ruled; the gap is the count and the untested moved behaviour | C9 moves four public types; moved behaviour unproved | count mismatch | names list and app note undercount | Grpc/GrpcServicesOptions.cs holds three types | no | gRPC | spec C9, Rule 3 | codex-2, F-L1 |
| R1-05 | sonnet | HIGH | seed-0007 | C7 key casing unspecified | two-writer shape | handler keys PascalCase, library camelCase | FastEndpoints UsePropertyNamingPolicy; test pins "Name" | no | validation | spec C7 | F-H1 |
| R1-06 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C7 acceptance misses grouping and outer fields; title overclaims | weak acceptance | duplicate field failures may throw; outer fields may drift | spec C7, Decisions | no | validation | spec C7 | codex-7, F-L5 |
| R1-07 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C1 save paths and selection mechanism open | open mechanism | sync saves and int aggregates without the marker ambiguous | DbContextExtensions.DomainEvents.cs:13; interceptors async only | no | events | spec C1, Handed | codex-4, F-M4 |
| R1-08 | codex, sonnet | MEDIUM | seed-0130 | Rule 3 compatibility unproved; C2 nullability trap | compatibility claim without proof | last-modified-by turns required; implementers break | AggregateRoot.cs:24; IClaimsProvider; open-generic behaviour | no | audit | spec C2, Rule 3 | codex-5, F-M3 |
| R1-09 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C6 comparer attachment and in-place meaning unclear | open contract | factory-only callers get no comparer | BuilderExtensions.cs:46-77 | no | converters | spec C6 | codex-6, F-L6 |
| R1-10 | codex, sonnet | MEDIUM | seed-0310 | C8 cites nonexistent tests; converter untested | acceptance with no baseline | "unchanged" proves nothing | tests/Blocks.FastEndpoints.Tests | no | endpoints | spec C8 | codex-8, F-M5 |
| R1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C10 acceptance proves invocation, not behaviour | weak acceptance | extension and async migrate unproved | spec C10 | no | additions | spec C10 | codex-9 |
| R1-12 | codex, sonnet | MEDIUM | seed-0023 | read-mes beyond C11 go stale; Core wording wrong | documentation drift | app teams told old behaviour | AspNetCore README:7-9; FastEndpoints README; root README; ConfigurationExtensions.cs:29-39 | no | docs | spec C11 | codex-10, F-M7, F-L7 |
| R1-13 | codex, sonnet | MEDIUM | seed-0023 | public-surface reference has no referent | stale pointer | baseline overwritten or counts stale | public-names.md:5-9,118-119 | no | docs | spec Rule 2 | codex-11, F-M6 |
| R1-14 | codex, sonnet | LOW | unranked — no anchor fits; Codex severity LOW | duplicate-key rationale false | inaccurate claim | the owner's drop rested on it | RepositoryExtensions.cs:35-41 | no | duplicate key | spec Dropped | codex-12, sonnet OQ3 |
| R1-15 | sonnet | LOW | seed-0364 | handed-to-plan list incomplete; arity sentence unclear | open mechanism not listed | plan settles names by default | spec Handed | no | plan hand-off | spec Handed | F-M4, F-L3 |
| R1-16 | sonnet | LOW | seed-0127 | portability check already folder-driven | misdescribed code | plan item partly moot | SnapshotFixture.cs:44; ReadMeTests.cs:17 | no | tests | spec Handed | F-L2 |
| R1-17 | sonnet | LOW | seed-0041 | 2026-10-11 rulings have no file | untraceable ruling | later readers cannot confirm | delivery/ | no | traceability | spec header | F-L4 |
| R1-18 | sonnet | LOW | seed-0127 | Core's own guards throw argument errors | consequence unstated | apps using them for input answer 500 | Guard.cs:20-23 | no | answers | spec Answers | gap analysis |
| R1-19 | sonnet | LOW | seed-0041 | F201's client-drop item and int-only helpers unaddressed | open question | knowledge-gateway not told | F201 § For the plan to settle; RepositoryExtensions.cs | no | answers | spec Answers | OQ1, OQ2 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-11)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | C3 (three cases), Acceptance C3, Decisions row 1 |
| R1-02 | fixed-real | MEDIUM | — | C3 (abort after logging), Acceptance C3, Decisions |
| R1-03 | fixed-real | HIGH | — | C9, C11, Acceptance C9 |
| R1-04 | fixed-real | MEDIUM | — | C9, Rule 3, Acceptance C9 |
| R1-05 | fixed-real | HIGH | — | C7, Acceptance C7 |
| R1-06 | fixed-real | MEDIUM | — | C7 title and text, Acceptance C7 |
| R1-07 | fixed-real | MEDIUM | — | C1, Acceptance C1, Decisions, Handed to the plan |
| R1-08 | fixed-real | MEDIUM | — | C2, Rule 3, Acceptance C2, Handed to the plan |
| R1-09 | fixed-real | MEDIUM | — | C6, Acceptance C6, Handed to the plan |
| R1-10 | fixed-real | MEDIUM | — | C8, Acceptance C8 |
| R1-11 | fixed-real | MEDIUM | — | Acceptance C10, Handed to the plan |
| R1-12 | fixed-real | MEDIUM | — | C11, Acceptance C11 |
| R1-13 | fixed-real | MEDIUM | — | Rule 2, Acceptance (public names) |
| R1-14 | fixed-real | LOW | — | owner re-asked with the corrected fact (2026-10-11): added back as C12; Dropped section removed; Out of Scope; Answers point 11 |
| R1-15 | fixed-real | LOW | — | Handed to the plan |
| R1-16 | fixed-real | LOW | — | Handed to the plan |
| R1-17 | fixed-real | LOW | — | new § The owner's rulings of 2026-10-11 |
| R1-18 | fixed-real | LOW | — | Answers (for every app) |
| R1-19 | fixed-real | LOW | — | Answers (points 4 and 2) |

Reconciled: C3 ↔ Decisions row 1 ↔ F1 D1 (cancelled request unlogged, 499 when not started); C7 ↔ C8 (one key
conversion for both writers); C9 ↔ C11 ↔ Rule 3 ↔ Answers (four types, namespace `Blocks.Grpc`, pins); C12 ↔ Out
of Scope ↔ Answers point 11 ↔ ideas.md topic 1; Rule 2 ↔ Acceptance (public names) ↔ `docs/public-names.md`.

## Round 2 — 2026-10-11 — spec — primary (codex)

Header note: one reader, as the schedule has it (the second reader joins round 1 only). Read: delta (round 1's
coverage was complete; slug number 3 is not a sample). The primary's verdict (NO-GO, 2 MEDIUM, both tagged `fold`)
and its per-finding closure table are persisted verbatim in `review-critic-codex.md`, second `## Verdict: NO-GO`
section (job task-mv3q3qvp-8fr4ch). Closure: R1-01 partial (collateral R2-01); R1-02 to R1-19 closed.

### Merge — round 2

Coverage: complete — delta read of the fold-touched sections, as dispatched.

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Decisions rows log and abort a started reply without the client-cancellation exception | two readings of one rule | a client disconnect logged and aborted, against C3 (a) and D1 | spec Decisions rows 1–2 vs C3 (a), Acceptance C3, Answers | no | fold | spec Decisions | codex R2-01 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Acceptance C3 (c) keeps tests that C4 and C7 change on purpose | contradictory acceptance | a developer cannot satisfy both | GlobalExceptionMiddlewareTests.cs:19, :189 | no | fold | spec Acceptance C3 | codex R2-02 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-11)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | MEDIUM | — | Decisions rows 1–2 (client cancellation excepted) |
| R2-02 | fixed-real | MEDIUM | — | Acceptance C3 (c) (the two intended assertion changes named) |

Reconciled: Decisions rows 1–2 ↔ C3 (a) ↔ Acceptance C3 ↔ Answers point 4; Acceptance C3 (c) ↔ C4 ↔ C7 ↔
`GlobalExceptionMiddlewareTests.cs`. No CRITICAL in round 2's merge, so no round 3; the spec goes Ready.


## Round 1 — 2026-10-13 — plan — primary (codex)

Header note: the primary's verdict (NO-GO, 7 HIGH · 5 MEDIUM · 1 LOW) is persisted verbatim in
`review-critic-codex.md`, third `## Verdict: NO-GO` section (plan review, job task-mv6lxxyu-rkzd6s); it cites no
anchor, so its rows carry `unranked — no anchor fits; Codex severity {level}`. Both readers ran (pair picked: codex +
sonnet). The second reader's message was truncated on delivery and recovered whole from its transcript with the
salvage script; it is below, unedited.

## Second reader — round 1 (sonnet)

# F3-GatewayFeedback: Plan Review, Critic

## Mode: Plan Review
## Verdict: REVISE
Round 1. The plan has 2 HIGH findings, 3 MEDIUM and 4 LOW. Step 6's test design can't run as written, and Step 4's who-typed assignment rule is both ambiguous and weaker than today's `int` path. Apart from the findings below, the plan's code facts match the live source.

## Pre-commitment predictions
- **Predicted and confirmed:**
  - Removing the MediatR constraint breaks the build (CS8714).
  - The nullable annotations on the new base class need fixing (CS8618).
- **Predicted and found fine:**
  - Default interface members, explicit defaults in a derived interface, and `string : IParsable<string>`.
  - Static abstract `T.Parse` via a cached generic delegate.
  - Interceptor failure handling runs before the `ApplicationDbContext` save override sees the exception.
  - `Abort()` under `TestServer`: the existing `AbortedRequest` fake pattern in `ErrorMapperHost.cs:60-67` covers it.
- **Not predicted, found:**
  - The endpoint library's process-wide static configuration.
  - The "keep if set" rule on the who-typed path.
  - The broken complex-type JSON helper.
  - The inert `Blocks.Grpc` settings check.

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: seed only (`references/ruler.md`; no overlay or corpus under `docs/critic-calibration/`) · Read: full

I ran scratch probes in the session scratchpad and touched no repo file.

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| C1 selection | `DbContextExtensions.DomainEvents.cs`, `AggregateRoot.cs`, `TenantEntity.cs`, both interceptors | COVERED |
| C2 entities and configuration | `IAuditedEntity.cs`, `AggregateRoot.cs`, `AuditedEntityConfiguration.cs`, `EntityConfiguration.cs`, `TestDatabase.cs`; probe p1 | COVERED, F3P-09 |
| C2 commands, claims reader, three steps | `IAuditableAction.cs`, `IClaimsProvider.cs`, `HttpContextProvider.cs`, the three assign-user-id steps, MediatR csproj and behaviours | PARTIAL, F3P-02, F3P-04 |
| C3 and C4 | `GlobalExceptionMiddleware.cs`, `ErrorMapperHost.cs`, `GlobalExceptionMiddlewareTests.cs` | COVERED |
| C5 and C10 migrate | `DbContextDesignTimeFactory.cs`, `DbContextExtensions.cs`; probe p5 (hand-written migration OK under EF 10) | COVERED |
| C6 | `BuilderExtensions.cs`; EF XML doc; probe p2 | PARTIAL, F3P-05 |
| C7 and C8 | FastEndpoints `Extensions.cs`, existing `DomainEventPublisherTests.cs`; probes p3, p4 | PARTIAL, F3P-01, F3P-03 |
| C9 | `Grpc/*`, `AspNetCore.csproj`, `Directory.Packages.props`, `PackageReport.cs`, `SnapshotFixture.cs`, `PortabilityTests.cs`; probe p7 (the moved files build with the plan's three packages) | COVERED |
| C11 | All five read-mes, `ConfigurationExtensions.cs`; probe p6 | PARTIAL, F3P-06 |
| C12 | `ApplicationDbContext.cs`, `Blocks.Exceptions/*`; probe p2 | COVERED |
| Rule 2 | `public-names.md` (header, wire table, spec table), `SurfaceDump/Program.cs` | PARTIAL: Blocks.* rows not read |
| Hygiene constraints | All 7 `Hygiene.Tests` files | COVERED |
| `DbContextExtensions.Seed.cs`, `Portability.Tests/Shell.cs`, `Guard.cs`, sibling specs beyond F1 | — | NOT OPENED: not on the change path |

## Findings

### [HIGH] F3P-01 — Step 6's three endpoint-library setups cannot coexist in one test process
**Anchor:** seed-0116 · above seed-0123 · below seed-0189
**Shape:** step asks one mechanism for two incompatible things
**Consequence:** The endpoint library keeps one process-wide setup, so a later host with a different setup silently gets the first one's and test results depend on order.
**Evidence:** scenario, reproduced.
- `FastEndpoints.Config` is public and sealed, but its options live in static fields (`ErrOpts`, `EpOpts`, `SerOpts`, …).
- Host A (prefix `api`, enum converter) started first, then host B (no prefix):
  - `B /probe -> 404`.
  - `B /api/probe -> 400 {"tag":"A",…}`.
- Reverse order:
  - `A /probe -> 400 {"tag":"B",…}`.
  - `B enum: 1`.
- After a host built like the existing `tests/Blocks.FastEndpoints.Tests/DomainEventPublisherTests.cs:70-85` (`MapFastEndpoints`, no config), a later `UseFastEndpoints(c => prefix + enum text)` is ignored: `/enum -> 200 1`, `/api/enum -> 404`.

**Surfaces:** plan § Step 6 (C8 tests first; "`UseFieldMapValidationReply` alone on an endpoint with no prefix"; "`UseEnumsAsText` alone"; the C7 endpoint fixture); `DomainEventPublisherTests.cs`
**Issue:** Step 6 needs at least three configurations in `Blocks.FastEndpoints.Tests`: combined, reply-builder-alone and enum-alone. Whichever host starts first wins, and xunit v3 runs test classes in parallel. The "tests written before the split, pass unchanged after" characterization is therefore order-dependent.
**Fix:**
- Fix one hosted configuration for the whole FE test process, `UseCustomFastEndpoints`, started from a shared fixture. Make it the only FE host that sets config, or move the config tests into their own test project.
- Test the two "alone" requirements at `Config` level: apply the extension to `new Config()` in a serial collection and assert on `Endpoints.RoutePrefix`, the converters, and a direct call of `Errors.ResponseBuilder`. Reset the statics between tests, or give these tests their own project and process.
- Read `config.Serializer.Options.PropertyNamingPolicy` inside the builder lambda, not at set-up.
- Amend spec Acceptance C8 wording ("on an endpoint") accordingly.

### [HIGH] F3P-02 — Step 4: the who-typed path keeps a pre-set id, and the plan misdescribes the behaviour's `int` rule
**Anchor:** seed-0117 · above seed-0130 · below seed-0189
**Shape:** two plan statements disagree with live behaviour; the weaker reading is a spoofable audit identity
**Consequence:** A caller-supplied "created by" survives on the text path while the number path overwrites it, so audit rows can record a forged actor.
**Evidence:** quoted.
- Plan: "the filter and the behaviour set the id only when it is unset and a claim is present"; `TryAssignCreatedBy` assigns "when … `CreatedById` is null … otherwise false"; the test "a command that already carries an id keeps it (filter and behaviour)".
- Live source:
  - `src/Blocks.MediatR/Behaviors/AssignUserIdBehavior.cs:16-18` assigns whenever a claim exists.
  - `src/Blocks.FastEndpoints/AssignUserIdPreProcessor.cs:14` assigns unconditionally.
  - Only the filter is unset-only: `src/Blocks.AspNetCore/Filters/AssignUserIdFilter.cs:13`, `== default`.
- Scenario: POST `{"createdById":"admin"}` to a who-typed command behind the behaviour. An `int` command gets overwritten; a text command keeps `"admin"`.

**Untouchable:** security (silent wrong output).
**Surfaces:** plan § Step 4 (bullets 2 and 4, tests paragraph); the three source files above
**Issue:** The plan can be built two ways. For the behaviour, "keeps" contradicts "`int` path unchanged". For the pre-processor, "sets it from `GetUserIdText`" can't use the null-only helper as defined.
**Fix:**
- Specify per step and per path.
- The pre-processor and behaviour overwrite when a claim exists, on both paths.
- The filter stays unset-only.
- Give `TryAssignCreatedBy` an overwrite mode, and retarget the "keeps it" test to the filter only.
- Add a "forged id is replaced" test for the pre-processor and behaviour.
- Note that the filter's unset-only rule predates F3 and is out of scope.

### [MEDIUM] F3P-03 — C7 decision changes `UseCustomFastEndpoints`'s wire output against C8's "behaving as today"
**Anchor:** seed-0130 · above seed-0127 · below seed-0117
**Band:** MEDIUM–HIGH; driver: whether the owner accepts the change to the library writer.
**Shape:** plan decision overrides a spec statement without sign-off
**Consequence:** Apps using the endpoint library's nested validators see `address.StreetName` become `address.streetName`, and nothing tells them.
**Evidence:** quoted, probe-confirmed.
- The probe yields `["firstName","address.StreetName","items[0].streetName"]` today.
- The plan's Decisions row says "a nested name from the endpoint library changes from `address.Street` to `address.street`".
- Spec C8: "`UseCustomFastEndpoints` stays … behaving as today".
- The spec's Answers § "For every app" (lines 138-141) lists the handler's `errors` change only.

**Surfaces:** plan § Step 6, § Decisions, § Step 10 (FastEndpoints bullet), § Step 11 (wire table); spec § C7, § C8, § Answers
**Fix:**
- Route to the architect and owner: either accept the change or choose another way to make the keys equal.
- If accepted, add it to the Answers list, the FastEndpoints read-me row and the wire table.
- The spec's premise that the library already camelCases nested names is false. The earlier spec review (R1-05) took it as fact.

### [MEDIUM] F3P-04 — Step 4: dropping the behaviour's constraint fails the build, and the Decisions rationale is wrong
**Anchor:** seed-0130 · above seed-0153 · below seed-0116
**Shape:** stated fact contradicts the compiler
**Consequence:** The step as written does not compile, and the behaviour then wraps every request with no test that non-auditable requests pass through untouched.
**Evidence:** probe p1 under the repo's `TreatWarningsAsErrors`.
- `error CS8714: … Nullability of type argument 'TRequest' doesn't match 'notnull' constraint` on `IPipelineBehavior<TRequest,TResponse>`.
- The plan says "the body already type-checks".
- The real constraint at `AssignUserIdBehavior.cs:10` was supplying `notnull`.

**Surfaces:** plan § Step 4 bullet 5, § Decisions row 7; `src/Blocks.MediatR/Behaviors/AssignUserIdBehavior.cs:10`
**Fix:**
- Replace the constraint with `where TRequest : notnull`, and correct the Decisions row.
- Add a test that a request implementing neither audit interface passes through unchanged.
- Add a README line that the behaviour now resolves `IClaimsProvider` for every request.

### [MEDIUM] F3P-05 — Step 8: the complex-type JSON helper is already broken, and the plan adds an overload and comparer to it
**Anchor:** seed-0130 · above seed-0127 · below seed-0116
**Shape:** step builds on a defect the plan does not know about
**Consequence:** The "every helper installs the comparer" claim cannot be verified for one helper, whose use already fails when the model is built.
**Evidence:** probe p2, existing helper verbatim.
- `HasJsonListConversion<T>(this ComplexTypePropertyBuilder<T>)` uses `BuildJsonReadOnlyListConvertor<T>()`.
- Result: `InvalidOperationException: Converter for model type 'IReadOnlyList<IReadOnlyList<string>>' cannot be used for 'Holder.Address#Addr.Tags' because its type is 'IReadOnlyList<string>'`.
- Source: `src/Blocks.EntityFrameworkCore/Extensions/BuilderExtensions.cs:72-73`.

**Surfaces:** plan § Step 8 (C6 bullet), § Testing; `BuilderExtensions.cs:72-73`
**Fix:** Decide before building: fix the form, which changes a signature nobody can use successfully today, or leave it out of the overload and comparer list. Either way, record it in the plan and list it in `public-names.md`. Make the "both `HasJsonListConversion` forms" wording say which.

### [MEDIUM] F3P-06 — Step 10: the `Blocks.Grpc` read-me example documents a check that never runs
**Anchor:** seed-0130 · above seed-0127 · below seed-0116
**Shape:** document states a rule its own example disobeys
**Consequence:** Readers believe the service URL and retry bounds are checked at start-up, but nothing checks them.
**Evidence:** path.
- `GrpcServicesOptions.cs:7-8,14,21-23` puts `[Required]` and `[Range]` only on the nested `GrpcServiceSettings` and `GrpcRetrySettings`, with no `[ValidateObjectMembers]` or `[ValidateEnumeratedItems]`. Step 7 allows only a namespace edit to this file.
- Probe p6: `ValidateDataAnnotations()` checks nested members only when the marker is present.
- `AddCodeFirstGrpcClient(…, GrpcServicesOptions grpcOptions)` takes an instance bound at registration, not the validated `IOptions`.

**Surfaces:** plan § Step 10 last sentence; spec § C11; `ConfigurationExtensions.cs:8-23`
**Fix:** Either add the two markers during the move and say so, or write the read-me with `GetSectionByTypeName` for the instance. State that nested checks do not run without markers, which is Core's own C11 rule. Route to the architect.

### [LOW] F3P-07 — Who-type is limited to reference types, unlike the spec's "chosen by the application"
**Anchor:** seed-0137 · above — · below seed-0123
**Band:** LOW–MEDIUM; driver: whether a `Guid` or `int` user id is wanted.
**Shape:** disclosed narrowing not carried into user-facing text
**Consequence:** An app with `Guid` or `int` user ids cannot use the two-type bases or `IAuditableActionBy<>`.
**Evidence:** probe p1; `IAuditableActionBy<int>` fails the `class` constraint.
**Surfaces:** plan § Decisions row 3; spec § C2
**Fix:** State the limit in the Domain and MediatR read-mes and in `public-names.md`, and ask the PO whether value who-types are wanted.

### [LOW] F3P-08 — Step 7 mechanics
**Anchor:** seed-0127 · above — · below seed-0123
**Shape:** imprecise command and ambiguous reference
**Consequence:** The accept command can't show a rename as written, and "move the step-1 test" can be read as editing the frozen compatibility project.
**Evidence:** quoted.
- `git mv` stages in the shared index.
- After `git mv` plus an edit, plain `git diff -M` compares the working tree to the index, so it shows no rename.
- The plan says "move the step-1 test", meaning item 1 of Step 7, but Step 1 is the frozen compatibility project.

**Surfaces:** plan § Step 7 items 1-2 and Accept
**Fix:** Say "item 1's test". Use a plain move and check with `git diff -M HEAD` after staging at close.

### [LOW] F3P-09 — Test scaffolding the plan does not mention
**Anchor:** seed-0153 · above — · below seed-0127
**Shape:** unstated build input
**Consequence:** The first build of the new classes and test projects fails on two small points.
**Evidence:** probe p1 shows CS8618 on the new base class's `CreatedById`: add `= default!` or `required`.
**Surfaces:** plan § Step 3, § Step 4 (MediatR.Tests), § Step 7 (Grpc.Tests); `Directory.Packages.props`
**Fix:**
- Add `= default!` or `required` to the new base class's `CreatedById`.
- `Microsoft.Extensions.DependencyInjection` is not pinned, so the new test projects need `FrameworkReference Microsoft.AspNetCore.App`, as their peers have.
- Name the internal duplicate recogniser so it satisfies the Hygiene "…Extensions" naming rule if it is a static class of extensions.

### [LOW] F3P-10 — Step 12: the plugin-feedback path and "no skill" claim disagree with the owning skill
**Anchor:** seed-0137 · above — · below seed-0123
**Shape:** location convention differs from the owning skill
**Consequence:** The note lands under a name the plugin's feedback intake won't recognise.
**Evidence:** `improve-skills/SKILL.md:24` gives `docs/plugin-feedback/nexus-{plugin-version}-{date}.md` with a fixed entry format. The plan says `{date}-error-handling.md` and "gap: no skill".
**Surfaces:** plan § Step 12, § Skill Mapping row 12
**Fix:** Follow the improve-skills convention, or note the divergence.

## Gap analysis
- `DesignTimeFactoryBase` (Step 8): the existing `?? throw` after `Activator.CreateInstance` is dead, because Activator throws `MissingMethodException`. The "throws naming both shapes" needs an explicit constructor lookup.
- Step 10's accept greps are not machine-checkable: `grep "Blocks.AspNetCore"` on the FastEndpoints read-me also matches line 30, which is not a pin line.
- C12's test checks `DbUpdateException` is still thrown for other failures, not the 500 reply. Existing `InvalidOperationException → 500` coverage makes this acceptable.
- C12 only covers contexts derived from `ApplicationDbContext<T>`. Step 10 already says "its limits".

## Verified clean
- The plan-time line references I spot-checked match the live source (`DomainEvents.cs:13`, `AggregateRoot.cs:5`, `GlobalExceptionMiddleware.cs:22,40-44,88-92`, `GlobalExceptionMiddlewareTests.cs:19,189`, `Grpc/*`, `Directory.Packages.props:43-44`, the Hygiene lists).
- `HasConversion(ValueConverter, ValueComparer)` exists on `PropertyBuilder<T>` and `ComplexTypePropertyBuilder<T>` in EF 10.0.12.
- Hand-written migrations apply under EF 10 `MigrateAsync`.
- `FastEndpoints` 8.3.0 does not pull in protobuf-net; the pin move is consistent.

## Open questions (low confidence, not findings)
- Whether the endpoint library's static options can be reset between tests without reflection: untested.
- `AssignUserIdFilter` keeps a client-supplied `int` id today (`:13`). This is pre-existing and out of F3's scope, but the owner may want it noted.

Reviewed: D:\src\dotnet-building-blocks\docs\specs\F3-GatewayFeedback\delivery\plan.md

### Merge — round 1

Coverage: complete — every baseline surface was opened by at least one reader (the second reader's unopened list —
`DbContextExtensions.Seed.cs`, `Portability.Tests/Shell.cs`, `Guard.cs` — holds no baseline surface; the primary read
`Repository.cs` and the read-mes).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | sonnet | HIGH | seed-0116 | Step 6's three endpoint-library set-ups cannot share one test process | one mechanism asked for two incompatible things | order-dependent tests; characterization meaningless | FastEndpoints static options, reproduced both orders | no | endpoints | plan Step 6 | F3P-01 |
| R1-02 | codex, sonnet | HIGH | seed-0117 (codex unranked, HIGH) | Step 4 misstates each step's `int` rule; the text path keeps a forged id | two statements disagree with live code | audit rows record a caller-supplied actor | AssignUserIdBehavior.cs:14-18; AssignUserIdPreProcessor.cs:14; AssignUserIdFilter.cs:13 | security | audit | plan Step 4 | codex-4, F3P-02 |
| R1-03 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: the step does not compile under warnings-as-errors | Removing the behaviour's constraint fails CS8714 | stated fact contradicts the compiler | build break | IPipelineBehavior `notnull`; probe p1 | no | mediator | plan Step 4, Decisions | codex-6, F3P-04 |
| R1-04 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | An unconstrained behaviour needs a claims reader for every request | regression for existing hosts | ordinary requests fail where no claims reader is registered | DI skips constrained open generics today | no | mediator | plan Step 4 | codex-7 |
| R1-05 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Moving the event members breaks explicit implementations, against Rule 3 | compatibility claim without proof | consumer code stops compiling | plan Decisions row 2 | no | events | plan Step 2, Decisions | codex-2 |
| R1-06 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | C12 misses repository saves on an ordinary context | requirement partly delivered | a duplicate through the repository still answers 500 | Repository.cs:93 | no | duplicate key | plan Step 9 | codex-1 |
| R1-07 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | C12 tests use look-alike types, not the providers' own | acceptance with a substitute | a renamed provider type passes the test | spec Acceptance C12 | no | duplicate key | plan Step 9 | codex-3 |
| R1-08 | sonnet | MEDIUM | seed-0130 | The shared key rule changes the endpoint library's nested names, against C8 | plan decision overrides a spec line | `address.StreetName` becomes `address.streetName` unannounced | probe; spec C8, Answers | no | validation | plan Step 6, Decisions, Steps 10–11 | F3P-03 |
| R1-09 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | The probe's `AddError` result was the validator's, not `AddError`'s | wrong evidence | a test built on it adds a general error | FastEndpoints `AddError(string message, …)` | no | validation | plan Context, Step 6 | codex-5 |
| R1-10 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | PostgreSQL recognition takes any `DbException` with 23505 | scope wider than the spec | another database changes behaviour | spec C12 | no | duplicate key | plan Step 9 | codex-8 |
| R1-11 | codex, sonnet | MEDIUM | seed-0130 (codex unranked, MEDIUM) | The complex-type JSON helper is already broken | step builds on a defect | the comparer claim cannot hold for it | BuilderExtensions.cs:72-73; probe p2 | no | converters | plan Step 8 | codex-9, F3P-05 |
| R1-12 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | The async save override must keep the token's default | signature drift | `SaveChangesAsync(false)` callers | EF Core `DbContext.SaveChangesAsync` | no | duplicate key | plan Step 9 | codex-10 |
| R1-13 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C3 tests miss the validation branch after the reply started | test passes without the change | validation still writes into a started reply | GlobalExceptionMiddleware.cs:45 | no | errors | plan Step 5 | codex-11 |
| R1-14 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C11 acceptance does not check the corrections; greps are line-bound | weak acceptance | read-mes stay wrong and pass | FastEndpoints README.md:18-19, :30 | no | docs | plan Step 10 | codex-12, sonnet gap |
| R1-15 | sonnet | MEDIUM | seed-0130 | The `Blocks.Grpc` read-me would document a check that never runs | document disobeys its own rule | readers trust a start-up check that is absent | GrpcServicesOptions.cs; probe p6 | no | gRPC | plan Step 10 | F3P-06 |
| R1-16 | codex | LOW | unranked — no anchor fits; Codex severity LOW | The compatibility project's "never edited" has no baseline | unpinnable claim | a later edit goes unseen | untracked files absent from `git diff` | no | compatibility | plan Step 1 | codex-13 |
| R1-17 | sonnet | LOW | seed-0137 | Who-type limited to reference types; not said to readers | disclosed narrowing not carried | `Guid`/`int` user ids cannot use the new forms | probe p1 | no | audit | plan Decisions, Step 10 | F3P-07 |
| R1-18 | sonnet | LOW | seed-0127 | Step 7 mechanics: `git mv` stages; "step-1 test" ambiguous | imprecise command | rename check shows nothing; frozen project edited | git diff semantics | no | gRPC | plan Step 7 | F3P-08 |
| R1-19 | sonnet | LOW | seed-0153 | Unstated scaffolding: CS8618, DI package for new test projects | unstated build input | first builds fail | probe p1 | no | build | plan Steps 3, 4, 7 | F3P-09 |
| R1-20 | sonnet | LOW | seed-0137 | Plugin note path differs from the plugin's convention | location convention | intake misses the note | improve-skills/SKILL.md:24 | no | close | plan Step 12 | F3P-10 |
| R1-21 | sonnet | LOW | seed-0127 | The factory's "throws naming both shapes" needs an explicit constructor lookup | dead branch | the message never shows | DbContextDesignTimeFactory.cs:21-22 | no | factory | plan Step 8 | sonnet gap |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-13)

Two findings were put to the owner before the fold (question box, 2026-10-13): R1-08 — every part of a nested
field name lower-cased in both writers (yes); R1-07 — the PostgreSQL library's own error type in tests, its licence
allowed (yes). Recorded in the spec as rulings 5 and 6.

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | plan Context (process-wide set-up); Step 6 test layout (one hosted set-up, new `Blocks.FastEndpoints.SetupTests`); Step 4 (pre-processor tested directly); Testing Strategy; Decisions; spec Acceptance C8 |
| R1-02 | fixed-real | HIGH | — | plan Context (the three `int` rules); Step 4 (rule table, `keepExisting`, tests); Must-NOT-Change; Decisions |
| R1-03 | fixed-real | HIGH | — | plan Step 4 (`IAuditableRequest` constraint); Domain Model Changes; Decisions |
| R1-04 | fixed-real | HIGH | — | plan Step 4 (marker keeps the container skipping other requests; no-claims-reader test); Must-NOT-Change; Decisions |
| R1-05 | fixed-real | HIGH | — | plan Step 2 (members kept, forwarding default bodies, explicit-implementer test); Step 1 (explicit fixture); Must-NOT-Change; Decisions |
| R1-06 | fixed-real | HIGH | — | plan Step 9 (base context and repository save); Context; Decisions |
| R1-07 | fixed-real | HIGH | — | owner ruling 6; plan Step 9 (real provider types, test-only packages, licence entry, reply chain stated); Context (licences, staging of `PackageTests.cs`) |
| R1-08 | fixed-real | MEDIUM | — | owner ruling 5; spec C8 row, Acceptance C8, Answers; plan Context, Step 6, Step 10, Step 11, Step 12, Must-NOT-Change |
| R1-09 | fixed-real | MEDIUM | — | plan Context (probe text corrected); Step 6 (fixture through a validator) |
| R1-10 | fixed-real | MEDIUM | — | plan Step 9 (type name and code; negative tests); Must-NOT-Change |
| R1-11 | fixed-real | MEDIUM | — | plan Context; Step 8 (helper repaired, test); Decisions |
| R1-12 | fixed-real | MEDIUM | — | plan Step 9 (token default kept); Step 1 (`SaveChangesAsync(false)` fixture) |
| R1-13 | fixed-real | MEDIUM | — | plan Step 5 (validation-after-start test) |
| R1-14 | fixed-real | MEDIUM | — | plan Step 10 Accept (a `ReadMeTests` fact on folded text) |
| R1-15 | fixed-real | MEDIUM | — | plan Step 10 (Grpc read-me states no start-up check); Step 7 (classes unchanged); Decisions |
| R1-16 | fixed-real | LOW | — | plan Step 1 (SHA-256 baseline); Must-NOT-Change |
| R1-17 | fixed-real | LOW | — | plan Step 3; Step 10; Decisions |
| R1-18 | fixed-real | LOW | — | plan Step 7 (plain move, "item 1's test", `git show … \| diff` accept) |
| R1-19 | fixed-real | LOW | — | plan Step 3 (`= default!`); Steps 4, 7 (framework reference); Step 9 (recogniser naming) |
| R1-20 | fixed-real | LOW | — | plan Step 12 and Skill Mapping row 12 (improve-skills channel and file name) |
| R1-21 | fixed-real | LOW | — | plan Step 8 (explicit constructor lookup) |

Reconciled: Step 4 rule table ↔ Context `int` rules ↔ Must-NOT-Change ↔ Decisions (constraint, rules); Step 6
layout ↔ Step 4 pre-processor test ↔ Testing Strategy ↔ spec Acceptance C8; Step 9 ↔ Context staging and licences ↔
Must-NOT-Change ↔ Decisions (two save paths) ↔ spec ruling 6; ruling 5 ↔ spec C8 row and Answers ↔ plan Steps 6, 10,
11, 12; Step 2 ↔ Step 1 fixtures ↔ Decisions; Step 7 ↔ Step 10 (Grpc read-me) ↔ Decisions. Round 2 runs (round 1's
merge holds HIGH and MEDIUM): one reader, delta read, floor MEDIUM.

## Round 2 — 2026-10-13 — plan — primary (codex)

Header note: one reader, as the schedule has it (the second reader joins round 1 only). Read: delta (round 1's
coverage was complete; slug number 3 is not a sample). Floor: MEDIUM. The primary's verdict (NO-GO, 2 MEDIUM, both
partial closures of round-1 findings) and its closure table are persisted verbatim in `review-critic-codex.md`,
fourth `## Verdict: NO-GO` section (job task-mv6wjvu4-rnvp36). Closure: R1-07 and R1-14 partial; the other 19 closed;
no collateral finding.

### Merge — round 2

Coverage: complete — delta read of the fold-touched sections, as dispatched.

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | C12's 409 and 500 replies are inferred, not tested end to end | acceptance with a substitute | the fixed 409 reply from a real failed save is unproved | plan Step 9 tests; `EachErrorType_AnswersItsStatus` builds bare exceptions | no | fold | plan Step 9 | codex R2-1 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Read-me checks are keyword presence; wrong statements still pass | weak acceptance | a read-me can keep the old claim and pass | plan Step 10 Accept | no | fold | plan Step 10 | codex R2-2 |

Below MEDIUM (recorded): Step 11 called the changed constraint "removed"; the Decisions row on `.claude/verify.json`
said three new test projects where Testing Strategy lists four.

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | MEDIUM | — | plan Step 9 (an end-to-end host: duplicate → 409 with the fixed message, 547 → 500) |
| R2-02 | fixed-real | MEDIUM | — | plan Step 10 (a table of binding statements, each asserted present, and today's wrong text asserted absent) |

The two below-MEDIUM slips were corrected as well (plan Step 11, Decisions). Reconciled: Step 9 ↔ Step 5
(`UseGlobalExceptionMiddleware` is built in an earlier slice) ↔ Must-NOT-Change; Step 10 table ↔ spec C11 ↔ Steps 3,
4, 6, 7, 9 (each statement matches its step). No CRITICAL in round 2's merge, so no round 3; the plan review ends.
