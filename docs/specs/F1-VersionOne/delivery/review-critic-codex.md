# Codex critic — F1-VersionOne spec

Round: 1. Persisted by the architect (main session) from the Codex job output, 2026-10-08.

## Verdict: NO-GO

**NO-GO — Round 1.** Found **7 HIGH, 9 MEDIUM and 2 LOW** issues. No files were modified; no builds or tests were run.

The 11-block inventory, three product-wording locations, namespace entries, claims-query bug and `GETUTCDATE()` default match the reference source. The owner’s recorded choice permits app edits and two-way sync, superseding the hand-off’s earlier prohibition. The MediatR 12.x and MassTransit 8.x licensing direction is valid. [MediatR 12.5.0 licence](https://www.nuget.org/packages/MediatR/12.5.0), [MassTransit 8.5.2 licence](https://raw.githubusercontent.com/MassTransit/MassTransit/v8.5.2/LICENSE).

Findings 1–16 concern [spec.md](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/definition/spec.md). Reference source paths below are relative to `D:\src\dotnet-microservices\src\BuildingBlocks`.

1. **HIGH — spec.md:69–80 — Send-back cannot complete its documented round trip.**  
   Send-back leaves the app’s lock unchanged. After the developer commits the returned block here, forward sync still finds the app copy different from the old lock and refuses. The spec needs a defined way to recognise that the edit has been sent and safely refresh the lock.

2. **HIGH — spec.md:61–73 — First-import and unusable-baseline refusal rules are missing.**  
   Flow 1 requires comparison with a lock but never defines bootstrap without one. It also gives no outcome for a populated destination without a trustworthy lock, a corrupt lock, an unavailable baseline commit or mismatched repository provenance. This matters immediately for knowledge-gateway’s existing four blocks: an implementation could overwrite them or reject every initial import.

3. **HIGH — spec.md:63, 69–79 — Change detection lacks a complete file-set contract.**  
   Hashes of previously copied files alone do not detect newly added files. Forward mirroring could therefore delete an added `.cs` file without recognising an app edit. Define comparison of file membership and contents, including additions, deletions and renames on both sides. Send-back must compare the actual master working tree, including staged and unstaged changes. Also define which files participate so build outputs are excluded while new source files remain protected.

4. **HIGH — spec.md:67–70, 94 — Forward-check scope contradicts the “before anything forward” rule.**  
   The flow appears to check the newly selected blocks and their dependencies. Rule 6 and picked idea I require sending app edits back before forwarding anything. An edited block removed from the manifest could escape that check, become orphaned or lose its lock entry. Previously locked blocks and block removal need explicit treatment.

5. **HIGH — spec.md:141 — Mandatory internal-helper convention is deferred without a recorded override.**  
   The hand-off explicitly binds Y2, and `reference-conventions.md:127` requires blocks to keep their internals internal. The supplied `ideas.md` and `conflicts.drawio` do not record an exception moving that obligation to version two. The spec currently contradicts a binding requirement.

6. **HIGH — spec.md:36–40 — Convention scope is contradictory.**  
   “Follows the settled conventions” conflicts with “only these” departures from reference shapes. Applicable settled rules require additional changes: A35 affects `RegexExtension`; X4 affects several async public names; A8/Y20 affect `JwtOptions` and mutable options properties. A plan cannot determine whether to apply these rules or preserve the reference. State the applicable conventions and any authorised release exceptions.

7. **HIGH — spec.md:47, 54, 129 — Adding a conflict exception does not make the existing already-exists refusal return 409.**  
   `Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs:37–42` contains `EnsureNotExistsOrThrowAsync`, which throws `BadRequestException` when the entity exists. Keeping that implementation returns 400 despite binding Y22. The spec needs to address this existing trigger and require a corresponding 409 check.

8. **MEDIUM — spec.md:119–125 — Public-names table omits convention-required renames.**  
   Missing entries include:
   `RegexExtension → RegexExtensions`; Redis `Repository<T>.Exists → ExistsAsync`; repository and extension `GenerateNewId` overloads → `GenerateNewIdAsync`; `SetSequenceSeed → SetSequenceSeedAsync`; `SeedFromJson → SeedFromJsonAsync`; Hasura `TrackObjectRelationship` and `TrackArrayRelationship` → their `Async` names; and `TransactionProvider.GetCurrentTransaction → GetCurrentTransactionAsync`.  
   D2 also needs a disposition for protected virtual `AuditedEntityConfiguration<T,TKey>.DefaultDateSql`: removing that override point affects consumers and must be recorded in the surface comparison.

9. **MEDIUM — spec.md:30–31, 99–100 — The empty-project criterion does not establish an isolated portability check.**  
   It does not specify where the copy lives, which project is built or what surrounding files may exist. A test inside the checkout could inherit configuration or retain references to original projects and still pass. Require an isolated destination containing only the copied projects, their transitive block dependencies and the necessary central package configuration.

10. **MEDIUM — spec.md:68, 73, 77, 83 — Commit selection is incomplete across the flows.**  
    Copying uses the manifest commit, but dependency discovery and package reporting are not explicitly tied to that same commit. “Changed here” also lacks a defined comparison target: manifest commit, checkout HEAD or working tree. Different choices produce different dependencies, package reports and status results.

11. **MEDIUM — spec.md:107 — The upsert acceptance test can pass without fixing the bug.**  
    An insert through `UpsertAsync` can save a private-field-backed property using the unchanged reference implementation. A property with an ordinary readable CLR accessor may also miss the defective case. Require updating an existing entity with a mapped field-backed member that the old `SetValues(entity)` path misses, then verify persistence from a fresh context.

12. **MEDIUM — spec.md:55–56, 104–110 — Acceptance coverage omits A2 and wrapped cancellation.**  
    There is no criterion proving a non-role claim query returns the requested claim values rather than roles. The actual faulty member is `HttpContextProvider.GetClaimValues`, implementing `IClaimsProvider`; `IUserClaimsProvider` only declares `UserRole`. The error criteria also do not explicitly exercise cancellation inside another exception, although D1 requires it.

13. **MEDIUM — spec.md:47, 104–105 — Unconditional 499 behavior cannot hold after response start.**  
    The reference guards its 499 assignment with `!Response.HasStarted`. Kestrel rejects changing the status after headers are committed. Define the outcome for cancellation before and after response start, including wrapped cancellation; otherwise the literal requirement can produce a second exception or an impossible acceptance condition. [Kestrel status setter](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Servers/Kestrel/Core/src/Internal/Http/HttpProtocol.cs).

14. **MEDIUM — spec.md:48, 108 — Removing the date default does not establish database portability for the whole block.**  
    Retained reference helpers still execute SQL Server-specific `DBCC CHECKIDENT` and `SET IDENTITY_INSERT` statements in `DbContextExtensions.Seed.cs` and `ManualGenerateIdScope.cs`. Clarify the portability boundary and provider restrictions. The current build and model-default criteria cannot verify the broader “any database” rationale.

15. **MEDIUM — spec.md:69–70 — Picked refusal diagnostics are dropped.**  
    Picked idea B (`ideas.md:13,17`) requires refusal to list differing files. Forward sync only says it stops; send-back names a block. The file diagnostics remain part of the recorded choice and need a requirement and acceptance check.

16. **MEDIUM — spec.md:50 — D4 overstates the available reference evidence.**  
    “No specific handler ever ran” is asserted as established reference behavior. `blocks-comparison.md:82` explicitly says reference behavior was not run and depends on its FastEndpoints version: reference 7.0.1 versus sprint-rituals 8.1.0. The source confirms the suspect interface-typed call, but the historical claim needs qualification or a version-specific reproducer.

17. **LOW — [conflicts.drawio:20](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/definition/conflicts.drawio:20) — Incorrect helper count.**  
    Cell `k11` says the reference has eight validation-message helpers. `Blocks.Core/FluentValidation/Extensions.cs` declares seven. The comparison evidence repeats the incorrect count.

18. **LOW — [handoff-blocks-repo.md:47](D:/src/knowledge-gateway/docs/specs/F194-ReferenceFoundations/delivery/handoff-blocks-repo.md:47) — Incorrect shared licence boundary.**  
    “9.x and later are commercial” is incorrect for MediatR and AutoMapper: MediatR 12.5.0 is Apache-2.0 and AutoMapper 14.0.0 is MIT. The spec’s MediatR 12.x constraint is correct. [MediatR package licence](https://www.nuget.org/packages/MediatR/12.5.0), [AutoMapper package licence](https://www.nuget.org/packages/AutoMapper/14.0.0).

Codex session ID: 01a11a63-d29a-7c93-a135-9724fe0412b9
Resume in Codex: codex resume 01a11a63-d29a-7c93-a135-9724fe0412b9


## Verdict: NO-GO

Round: 2 (delta read, floor MEDIUM). Persisted by the architect from the Codex job output, 2026-10-08.

**NO-GO — 3 MEDIUM findings.**

All references below are to [spec.md](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/definition/spec.md).

1. Round-1 closure at the MEDIUM floor:

| Finding | Status | Spec lines |
|---|---|---|
| R1-01 | closed | 81–89, 104–108 |
| R1-02 | partial | 74–75, 88–89, 124–127 |
| R1-03 | closed | 77–79, 119–122 |
| R1-04 | closed | 94, 128–129 |
| R1-05 | closed | 170–171, 215–216 |
| R1-06 | partial | 37–44 |
| R1-07 | closed | 51, 149 |
| R1-08 | closed | 164–165, 169–192 |
| R1-09 | closed | 70–72, 99–100, 137–140 |
| R1-10 | closed | 81–89, 104–105 |
| R1-11 | closed | 152–153 |
| R1-12 | closed | 59–60, 147–148, 155–156 |
| R1-13 | closed | 51, 147–148 |
| R1-14 | closed | 52, 161 |
| R1-15 | closed | 95–96, 105, 130 |
| R1-16 | closed | 54, 150–151 |
| R1-17 | partial | 93–108, 131–132 |
| R1-23 | closed | 62–63, 154 |

Remaining findings:

- **MEDIUM — R1-02 partial — spec.md:74–75, 124–127.** Bad-lock handling remains undefined. Rule 7 handles an absent entry, and lines 88–89 handle unavailable commits, but neither requires refusal before writes when the lock is malformed or internally inconsistent.

- **MEDIUM — R1-06 partial — spec.md:39–44.** Item 5 requires public naming changes, while item 7 still permits only the departures, additions and fixes below it to change the reference’s shapes or behaviour. The naming changes are outside that list, leaving conflicting instructions about the permitted API changes.

- **MEDIUM — R1-17 partial; fold — spec.md:131–132.** The new recovery sequence can repeat the same refusal. If the manifest still names the lock’s old commit, setting the app edit aside and taking forward retains that base; reapplying and sending back again encounters both sides changed. The exit omits updating the manifest to the repository change being reconciled.

2. **Yes:** the naming requirement contradicts the exclusivity clause at line 44, as described in R1-06.

3. **No separate break of an earlier guarantee was established within the permitted delta.**

Codex session ID: 01a11a7e-afef-7b42-bc20-0cb01b6bbacc
Resume in Codex: codex resume 01a11a7e-afef-7b42-bc20-0cb01b6bbacc


# Plan review

## Verdict: NO-GO

Round: 1 (plan). Persisted by the architect from the Codex job output, 2026-10-08.

**NO-GO — Round 1.**

Reviewed all seven scope requirements, D1–D4, A1–A4, Flows 1–3, Rules 1–11, and all 16 acceptance criteria. No files were modified, and no builds were run.

1. **CRITICAL — [plan.md:221](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:221): Write boundaries lack an enforceable contract.** Step 10 does not specify validation of block names, lock paths, dependency paths, or symlinks/junctions before copying or deleting. Traversal or redirected paths could reach outside the permitted folders, including protected repositories or the app’s package file. Step 11’s “outside the repo” condition also does not exclude the protected repositories. Require containment checks and negative tests proving refusal before any write. This affects Rule 4 and the explicit repository exclusions.

2. **HIGH — [plan.md:227](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:227): The adoption check does not guarantee recoverability.** `git status --porcelain -- <folder>` omits ignored files. An ignored file included in the sync file set could be replaced or removed while that check reports clean. Rule 7 requires every affected file to be recoverable from history; verify tracked, committed content rather than cleanliness alone.

3. **HIGH — [plan.md:93](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:93): The blanket Microsoft package upgrade requests a nonexistent version line.** `Blocks.Http.Abstractions.csproj:10` references `Microsoft.AspNetCore.Http.Abstractions`; the reference pins it to 2.3.0. That package has no 10.0.x release. Specify an explicit exception or framework-reference migration. [NuGet version history](https://www.nuget.org/packages/Microsoft.AspNetCore.Http.Abstractions/).

4. **HIGH — [plan.md:103](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:103): The surface baseline is captured after migration edits.** It cannot reveal public/protected changes introduced by framework compatibility fixes or warning cleanup in step 2. The spec requires comparison against the reference’s surface. Capture an untouched reference baseline from an isolated copy, or establish an equivalent source-based comparison, before migration edits.

5. **HIGH — [plan.md:113](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:113): “Apply exactly this table” misses namespace inconsistencies required by scope item 3.** Additional mixed folders include:
   - `Blocks.Core/Extensions`: `Blocks.Core` versus `Blocks.Core.Extensions`.
   - `Blocks.Core/Cache`: `Blocks.Core` versus `Blocks.Core.Cache`.
   - `Blocks.Domain/Entities`: `Blocks.Entities` versus `Blocks.Domain.Entities`.
   - The roots of `Blocks.Domain` and `Blocks.Exceptions` also contain differing namespaces.

   These need explicit disposition before implementation. Step 12 only observes changes already made; it does not discover omitted fixes.

6. **HIGH — [plan.md:241](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:241): Portability does not establish which candidate commit it tests.** Forward sync reads a committed tree, while the current HEAD contains only README, LICENSE and `.gitignore`. No preceding step creates a commit containing the candidate blocks. The check could fail for missing blocks or test an older committed implementation. Specify a committed temporary source snapshot and verify that copied files equal the candidate being reviewed.

7. **HIGH — [plan.md:107](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:107): The BOM check produces false negatives on this machine.** I ran its pattern against reference `Blocks.Exceptions/HttpException.cs`: grep returned no match, while a byte read confirmed `EF-BB-BF`. It also scans only `src` and `tools`, before later test and documentation files exist. Use a byte-based final check covering every repository file required by the criterion.

8. **MEDIUM — [plan.md:158](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:158): The helper fix precedes its regression test.** Step 5 changes `EnsureNotExistsOrThrowAsync`; step 6 subsequently tests that it throws `ConflictException`. Following this order cannot demonstrate the claimed red-first failure for that change. Add the test before modifying the helper.

9. **MEDIUM — [plan.md:101](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:101): Step 2’s edit restriction conflicts with zero-warning acceptance.** Existing nullable warnings already need attention—for example, reference `HttpContextProvider.cs:45–46` returns the nullable extension result through a non-nullable `string` method. These are not necessarily framework-upgrade breakages. Explicitly permit and account for necessary warning cleanup while preserving the required shapes and behavior.

10. **MEDIUM — [plan.md:79](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:79): Warning and package acceptance is incomplete.** `TreatWarningsAsErrors` does not prevent warning suppressions, and no step checks the spec’s prohibition on suppressions. The package grep checks only MediatR and MassTransit, before test projects exist; it cannot enforce the complete “no commercial package, tests included” requirement.

11. **MEDIUM — [plan.md:130](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:130): Naming acceptance is neither executable as written nor complete.** The Async pattern contains literal ellipses, requires unsupported constructs in ordinary grep, misses generic method names, and excludes private/internal methods despite requiring their renaming. Namespace checks omit `GlobalExceptionMiddleware` and the transactional interceptor. Whole-word product checks cannot detect retained `GetArticleId`, `articleId`, or `articleCommand`. Required edits could therefore be skipped while the checks pass.

12. **MEDIUM — [plan.md:144](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:144): Comment acceptance can pass with commented-out code remaining.** Removing the markers leaves examples such as `JwtOptions.cs:14` and `Repositories/Repository.cs:66–67` undetected. Neither `git diff --stat` nor an unchanged public surface proves that only comments changed. The same surface comparison also cannot pin the unchanged behavior of the 16 retained conflicts.

13. **MEDIUM — [plan.md:160](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:160): Mapper tests omit development details.** D1 explicitly requires error details in development, but the planned tests only verify the production 500 body. Include development-details assertions and explicit camelCase assertions for nested validation members `propertyName` and `errorMessage`.

14. **MEDIUM — [plan.md:212](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:212): README acceptance does not verify the requested content.** Eleven empty block READMEs plus one “SQL Server” mention would pass. The root usage guide, conflict-recovery instructions, registration guidance, dependencies, and both SQL Server-only helpers have no effective acceptance check.

15. **MEDIUM — [plan.md:223](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:223): Editor-folder exclusions are incomplete.** The plan excludes `.vs` and `*.user`, but the spec excludes editor folders generally. `.idea` and `.vscode` would enter the fingerprints and whole-block copies under the stated file-set definition. Define and test the complete exclusion policy.

16. **MEDIUM — [plan.md:254](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:254): Step 12 can pass while the spec’s public-name table remains incomplete.** Acceptance allows a recorded “Spec delta” and defers correction to the architect. The actual criterion requires the spec table itself to be complete. Reconciliation must finish before acceptance.

17. **MEDIUM — [plan.md:268](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:268): Empty git status is an invalid preservation check.** The reference already has untracked `.claude` files; sprint-rituals already has modified and untracked files. Requiring empty status fails before work begins. Conversely, git status normally ignores `bin`/`obj`, so it cannot prove those folders were untouched. Compare against captured initial state, including relevant ignored content.

18. **LOW — [plan.md:59](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:59): The GlobalUsings factual claim is false.** Reference `Blocks.EntityFrameworkCore/GlobalUsings.cs` exists and supplies EF Core and entity namespace imports. Correct the claim without applying unrelated code-shape changes.

19. **LOW — [plan.md:5](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:5): The build-slice explanation misstates dependencies.** The slices cover steps 1–12 without gaps, but only portability uses the sync tool. Step 12 uses `Blocks.SurfaceDump`, created in step 2.

The dependency table, tracked-file counts, listed rename locations, and cited sprint-rituals upsert, publisher and cancellation patterns matched source. The named MediatR and AwesomeAssertions choices are permissively licensed; MassTransit’s 8.5.10 source remains Apache-2.0. [MediatR 12.5.0](https://www.nuget.org/packages/MediatR/12.5.0), [AwesomeAssertions 9.0.0](https://www.nuget.org/packages/AwesomeAssertions/9.0.0), [MassTransit license](https://github.com/MassTransit/MassTransit/blob/v8.5.10/LICENSE).

Codex session ID: 01a11a86-5fed-7771-a576-8eed45b885d4
Resume in Codex: codex resume 01a11a86-5fed-7771-a576-8eed45b885d4


## Verdict: NO-GO

Round: 2 (plan, delta read, floor MEDIUM). Persisted by the architect from the Codex job output, 2026-10-08.

**NO-GO — 1 HIGH, 4 MEDIUM findings.**

1. Round-1 closure at the MEDIUM floor. Line numbers refer to [plan.md](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md).

| Finding | Closure | Plan line(s) |
|---|---|---|
| P1-01 | Partial | 262, 265 |
| P1-02 | Closed | 267, 268 |
| P1-03 | Closed | 121 |
| P1-04 | Closed | 110, 303 |
| P1-05 | Closed | 140, 145 |
| P1-06 | Closed | 289, 292 |
| P1-07 | Partial | 315, 316 |
| P1-08 | Closed | 126, 210 |
| P1-09 | Closed | 200, 202 |
| P1-10 | Partial | 320, 325 |
| P1-11 | Closed | 321, 322, 323 |
| P1-12 | Closed | 173, 318 |
| P1-13 | Closed | 193, 194 |
| P1-14 | Closed | 237, 275, 327 |
| P1-15 | Closed | 254 |
| P1-16 | Closed | 303, 305, 309 |
| P1-17 | Closed | 95, 331 |
| P1-18 | Closed | 88, 119, 260 |
| P1-19 | Closed | 182, 185 |
| P1-20 | Closed | 257, 258 |

2. The new text has one inconsistency; no additional contradiction was found in the opened spec sections.

- **MEDIUM · fold · [plan.md:258](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:258):** “Uncommitted here” compares the working tree against the manifest commit. With manifest commit B, newer source HEAD C, and a clean working tree at C, status incorrectly reports uncommitted changes. The comparison defines divergence from the pinned commit, not uncommitted work.

3. Four guarantees remain insufficiently protected.

- **HIGH · P1-01 partial · [plan.md:265](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:265):** Containment checks directory links but provides no protection against destination-file aliases. A hard-linked destination file passes the listed path checks; an in-place overwrite also changes its counterpart outside the allowed folders. The negative tests omit this case.

- **MEDIUM · P1-07 partial · [plan.md:315](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:315):** The BOM gate excludes `docs/`. A BOM in documentation, including generated `docs/public-names.md`, passes despite the earlier repository-wide guarantee.

- **MEDIUM · P1-10 partial · [plan.md:325](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:325):** The suppression check is present, but the commercial-package check only prohibits four named version lines. Other commercial direct or transitive dependencies can pass; no broader licence review is required.

- **MEDIUM · fold · [plan.md:331](D:/src/dotnet-building-blocks/docs/specs/F1-VersionOne/delivery/plan.md:331):** HEAD and porcelain-status equality cannot establish that the foreign repositories remained untouched. Changing an already-modified file’s contents leaves its `M` status unchanged. The replacement guard can therefore pass after a prohibited write, contrary to the invariant marked “pinned” at line 341.

Codex session ID: 01a11ab0-8116-76a1-8f98-5499b0fe8329
Resume in Codex: codex resume 01a11ab0-8116-76a1-8f98-5499b0fe8329

