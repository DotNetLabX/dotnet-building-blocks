# F1-VersionOne — Step 2 Code Review (second reader)

Plan: `docs/specs/F1-VersionOne/delivery/plan.md`. Implementation record: `implementation.md`. Read independently; no other reviewer's file was read.

## Reviewed By
reviewer, as the second reader (own checklist: simplicity/DRY, bugs/correctness, conventions) plus a mutation sample and mechanical diff checks against the reference at `fda2eb7`.
**Model:** claude-sonnet-5-5

## Verdict: APPROVED

No CRITICAL or HIGH. One MEDIUM (the names list is incomplete because the surface tool mis-renders unconstrained generic nullability) and four LOW. The work follows the plan, every behaviour fix is pinned by a test that I made fail on purpose, and the Sync tool's containment holds under the cases I tried.

## Pre-commitment Predictions
| Predicted problem area | What I found |
|---|---|
| Sync three-way logic or containment has a hole (junction, hard link, lock path) | Not found. The branches match Rules 2, 4, 6, 7, 8, 11. Five mutants (direct write, `--ignored` removal, pin filter, and others) all die on a test assertion. Two small edge issues (the two Sync LOW findings below). |
| Error mapper changes more than D1 lists | Not found. The diff against the reference is exactly D1 (arms, cancellation filter, fixed text, camelCase). A cancellation inside an upstream-call error answers 499, which D1 asks for; see Open Question 1. |
| `DeleteByIdAsync` SQL building is wrong for non-trivial models | Works for the tested shape. Key column comes from the model, but no test distinguishes it from the literal `Id` (third LOW finding). |
| Names list or hygiene gates are vacuous or narrower than the acceptance criteria | Names list: yes, one real gap (first finding). Hygiene gates: tests pass and I found no vacuous gate; two small scope narrowings (Gaps). |
| Step 2 warning fixes change public API without being listed | Found: ten public members changed annotation and are missing or misdescribed in the list (first finding). |

## Findings

### [MEDIUM] `docs/public-names.md` omits real nullable-annotation changes; the surface tool shows `T?` for any unconstrained `T`
**File:** `tools/Blocks.SurfaceDump/TypeNames.cs:89-90` (`Mark` reads `NullabilityInfo.ReadState`), `docs/public-names.md` (Blocks.Core section), `docs/specs/F1-VersionOne/delivery/surface-reference.txt:102-104,108`
**Origin:** implementation
**Issue:** The spec's last acceptance criterion and plan step 12 require the list to hold every difference between the two surfaces, including annotation changes. The tool prints `T?` for an unconstrained generic `T` whatever the source says, in the baseline and in the final dump alike, so a source change `T` to `T?` produces no diff line. I reproduced it with a fixture: `public class PlainC { public T Get<T>(T v) => v; }` under `Nullable=enable` dumps as `T? Get<T>(T? v)`. Concrete consequences, all checkable against the reference source:
- The reference source says `T DeserializeCaseInsensitive<T>`; version one says `T?` (implementation.md step 2, `JsonExtensions.cs:22`). Both dumps read `T?`, so the list has no row for it.
- `IThreadSafeMemoryCache` and `ThreadSafeMemoryCache`: `TryGet<T>(…, out T)` to `out T?`, `GetOrCreateAsync<T>` to `Task<T?>`, and the `GetOrCreate<T>` returns to `T?` (three interface members and six class members, `src/Blocks.Core/Cache/*.cs`). The list says "The other cache members differ only by namespace", which is wrong for these.
- The two "annotation changed" rows for `ThreadSafeMemoryCache.GetOrCreate` describe an artefact backwards: they say the parameter went `T? value` to `T value`; the source change is the return type `T` to `T?`. Note 1 guesses at the cause; the cause is the tool (fixture above), which also settles carry-over 10.
These are source-visible changes: a caller who dereferences `cache.GetOrCreate<Foo>(...)` now gets a nullable warning. The step-12 stop rule only checks diff line to explanation, never explanation to diff, so "every diff line is accounted for" held while the list was incomplete.
**Fix:** Make the tool read nullability of unconstrained type parameters from the member's own `[Nullable]` / `[NullableContext]` bytes (the helper `FirstByte` already exists in `Declarations.cs`), add a fixture test for `T` versus `T?` returns and parameters in `SurfaceDumper` tests, rebuild the baseline with the corrected tool, regenerate the list, and add the rows above (or add them by hand with a note, if re-baselining is not worth it for version one).
**Confidence:** 92/100

### [LOW] Manifest paths with a trailing slash or `./` prefix are refused with a message that names the wrong cause
**File:** `tools/Blocks.Sync/Containment.cs:20-25`
**Origin:** implementation
**Issue:** The plan's rule is "relative, no `..` segment, no root". `CheckRelative` also refuses empty and `.` segments. I ran `forward` with `"blocksFolder": "blocks/"`, `"./blocks"` and `"blocks//x"`: each exits 1 with `blocksFolder '…' must be a relative path with no '..' segment and no root`, which is not why it was refused. A person writing `./src/BuildingBlocks` hits this on the first run. It fails safe; the cost is a confusing first refusal.
**Fix:** Drop the empty and `.` segments before the check (and write the cleaned path back to the manifest model), or name them in the message.
**Confidence:** 95/100

### [LOW] `forward` changes the app before it reads the app's package file, so an unreadable file ends a run that already wrote
**File:** `tools/Blocks.Sync/ForwardCommand.cs:84-101`, `tools/Blocks.Sync/PackageReport.cs:70-78`
**Origin:** implementation
**Issue:** The package report is built after the copies and the lock write. I ran `forward` with an app `Directory.Packages.props` of `<Project><oops></Project>`: the block was copied, `blocks.lock.json` was written, then the run printed `Directory.Packages.props cannot be read` and exited 2 ("environment error"). Rule 11 stops the run before any write when the lock is unreadable; the same posture applies to a file the run must read anyway.
**Fix:** Parse the app's package file (and build the report) before the first write, or treat the failure as a warning after a successful copy and exit 0 with the report skipped.
**Confidence:** 90/100

### [LOW] Three behaviours the plan says to keep or provide are not pinned by any test
**File:** `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs:68` (log threshold), `src/Blocks.EntityFrameworkCore/Repositories/Repository.cs:83` (key column), `tools/Blocks.Sync/SyncCli.cs:78-86` (default `--source`)
**Origin:** design
**Issue:** My mutation sample (scratch copy, kill counted only from a failing assertion):
- `statusCode >= InternalServerError` changed to `>` (500 no longer logged): all 22 error-mapper tests pass. Plan step 5 says "Keep … its log threshold (500 and up)".
- `sql.DelimitIdentifier(keyColumn)` changed to `sql.DelimitIdentifier("Id")`: all 9 EF tests pass, because the test entity's key column is `Id`. Plan A4 says the statement is built from the model's key column name.
- Every Sync test passes `--source`; the default (walk up from the binaries to `Blocks.slnx`) has no test. I ran it by hand from this checkout and it resolves correctly (it read this repo's origin), so this is a gap, not a defect.
**Fix:** A log-capturing test for 500, 502 and 404; an entity with a renamed key column in the delete test; one `SyncCli.Run(["status", "--app", …])` without `--source` asserting the origin message.
**Confidence:** 90/100

### [LOW] Narration comments remain in at least eight blocks; the carry-over names only Messaging
**File:** e.g. `src/Blocks.AspNetCore/Middlewares/RequestDiagnosticsMiddleware.cs:15,18,47,55` (`// get correlation`, `// begin`, `// is file transfer?`, `// end`), `src/Blocks.Messaging/RabbitMqOptions.cs:5-8`, `src/Blocks.Core/Extensions/TypeExtensions.cs:8`, `src/Blocks.EntityFrameworkCore/Repositories/IRepository.cs:13-31`
**Origin:** design
**Issue:** About 40 comment lines restate what the next line shows. They are inherited unchanged from the reference and step 4's rules (markers, commented-out code, `///`) do not touch them, so this is not a plan deviation, and the spec's item 5 does not list narration. I count it only because the carry-over understates the spread. The constraint comments the developer wrote or kept (`// Raw SQL: marking a stub entity…`, `// Retries run on the gRPC channel…`, `// Only aggregates carry audit fields…`, the Redis.OM workaround) are fine.
**Fix:** Optional: remove with the other code-shape conventions in version two.
**Confidence:** 85/100

## Positive Observations
- The reference diff is clean. I diffed every `.cs` file of the 11 blocks against `fda2eb7` (BOM and CR stripped): each hunk is a listed rename, namespace move, warning fix, D1/D2/D4/A2/A4 change, or comment removal. Nothing unlisted.
- Step 4's byte-identical-DLL claim holds: `cmp` on the stored before and after DLLs reports all 11 identical.
- Sync containment is thorough and fail-safe: block-name pattern, relative-path checks, link checks from the app root down to the write folder, temp file then move (kills a hard-linked write; mutant died). `--json` keeps stdout to the report.
- Test quality is high. Tests assert behaviour through real git repositories and a real test host; the D3 shadow-property test also asserts a normal property so a no-op upsert cannot pass.
- Fresh independent evidence: foreign-repo record re-taken today and compared with `foreign-repos-before.txt`: 0 differences in both parts. Surface dump re-run: 93 reference-only and 99 version-one-only lines, the same as the record.
- The Q4 ruling is honoured: `UpsertAsync` keeps the reference's `SetValues(entity)` and the shadow test goes red on the sprint-rituals line (mutant M10).

## Gaps
- `SnapshotComparer` (the Q2 scope of the three-repo comparison) has no automated test; its record is one hand-made-records session in implementation.md. It is a one-off instrument, so I note it only.
- The BOM and suppression tests skip `.claude` folders (documented). A file committed under `.claude/` would not be scanned; `.claude/.worktree-target` is not in `.gitignore` and shows as untracked.
- `NamingTests.BlockAssemblies` is a fixed list of 11 names, while the read-me and portability tests read the `src/Blocks.*` folders; a twelfth block would need two edits.
- Non-Windows branches (`ln`, `Directory.CreateSymbolicLink`) in the Sync fixture cannot run here. By inspection `ln target link` has the right argument order.
- Unreadable-app-package-file and `.`-segment behaviours are untested (the two Sync LOW findings).

## Open Questions
1. **499 for any cancellation in the inner chain** (confidence 60; design/requirements). An `HttpClient` timeout surfaces as `TaskCanceledException`; if application code wraps it as `BadGatewayException("upstream timed out", ex)`, the mapper now answers 499 and logs nothing, where a 502 would be the honest answer. The reference had the same behaviour for a bare cancellation; D1 widens it to the inner chain on purpose. The mapper does not check `context.RequestAborted`. Worth a one-line decision from the architect on whether "caused by cancellation" should also require `RequestAborted.IsCancellationRequested`.
2. **Message leak for 5xx other than 500** (confidence 40). A 502 or 503 carries its own message to the client outside development, as D1 words it. Fine today; the blocks' read-me could say so.

## Carry-Over Findings (implementation.md), addressed one by one
| # | Item | Disposition |
|---|---|---|
| 1 | `System.ServiceModel.Primitives` pin in no graph | Confirmed. `dotnet list … --include-transitive` for Blocks.AspNetCore and Blocks.FastEndpoints shows protobuf-net 3.2.56 and protobuf-net.Core but no ServiceModel. Harmless (pins do not add references), but every app is told to add it. Architect call; I would drop it. |
| 2 | `MapToConstructor` doc versus code | Confirmed: `GetConstructors().First()` (`src/Blocks.Core/Mapster/Extensions.cs:11`); doc now matches. Latent in the reference; version two. |
| 3 | `GetRouteValue` returns `string?` | Confirmed and listed (AspNetCore row 4 of the names list). |
| 4 | Commented-out-code rule misses some shapes | Confirmed. My own scan of every remaining `//` line in `src/` found none that is code. |
| 5 | Hasura SQL string now LF | Confirmed benign: the CR-stripped diff against the reference shows no change on those lines. |
| 6 | `TryReseedTable` sends the table name as a parameter | Confirmed (`DbContextExtensions.Seed.cs:88`). `DBCC CHECKIDENT` accepts a variable, and failure is swallowed; out of scope, SQL Server only, read-me says so. |
| 7 | Development 500 keeps the thrown message | Refuted as a concern: it is what D1 and plan step 5 say ("also carries the error's details"; fixed text "outside development"). Pinned both ways by tests. |
| 8 | Messaging narration comments | Confirmed and wider; see the LOW finding. |
| 9 | `Git.cs` repeated in two tools | Confirmed, harmless. |
| 10 | `ThreadSafeMemoryCache.GetOrCreate` parameters show `T?` to `T` | **Cause found: the surface tool.** See the MEDIUM finding and the fixture. |
| 11 | `Containment.Inside` starts-with unreachable | Agree it is equivalent by construction. |
| 12 | Sync tests run `mklink` and `ln` | Confirmed; non-Windows branch not run here. |

## Evidence
| Check | Result | Command | Output |
|---|---|---|---|
| Build, warnings as errors | pass | `dotnet build Blocks.slnx --no-incremental -warnaserror` | 0 Warning(s), 0 Error(s), 21 projects |
| Tests, the whole solution | pass | `dotnet test Blocks.slnx --no-build` | AspNetCore 22, FastEndpoints 2, EF 9, SurfaceDump 9, Hygiene 12, Sync 83, Portability 11 (includes the plan-mandated `Category=Portability` cases, no filter) = 148/148 |
| Reference parity | pass | `diff` of each `src/*.cs` against `git archive fda2eb7`, BOM and CR stripped | every hunk explained (MEDIUM finding aside) |
| Surface re-run | pass | `Blocks.SurfaceDump` on a scratch build, `diff` with `surface-reference.txt` | 93 and 99 lines, as recorded |
| Foreign repos untouched | pass | `Blocks.ForeignSnapshot record` (read-only) then `compare` against `foreign-repos-before.txt` | must be equal 0; listed 0; exit 0 |
| BOM, CR, suppression words | pass | `grep -rlP '\A\xEF\xBB\xBF'`, `grep -rlI $'\r'`, `grep -rniE '#pragma warning|SuppressMessage|NoWarn|WarningsNotAsErrors'` | no hits (apart from the test that names them in pieces) |
| Step 4 byte-identical DLLs | pass | `cmp` over the 11 stored before/after DLLs | 11 of 11 identical |
| Free releases | pass | `dotnet list <block> package --include-transitive` | MediatR 12.5.0, MassTransit 8.5.11, protobuf-net 3.2.56 |
| Mutation sample (scratch copy, baseline green first) | 12 run | see below | 9 killed by a failing assertion, 3 survived |

Mutation battery (kill = failing test assertion; no compile failures occurred):
| Mutant | Result |
|---|---|
| M1 D4: remove the `(IEvent)` cast | killed (2 tests) |
| M2 D1: validation reply `Details` always null | killed (`ValidationErrorInDevelopment_CarriesDetails`) |
| M3 D1: log threshold `>=` to `>` | **survived** |
| M4 A4: key column replaced by literal `"Id"` | **survived** |
| M5 A4: table delimited without schema | survived, equivalent on SQLite (no schema) |
| M6 A2 original bug back in (`ClaimTypes.Role`) | killed (2 tests) |
| M7 Rule 4: direct write instead of temp file and move | killed (`Rule4_HardLinkedDestinationFile…`) |
| M8 Rule 7: drop `--ignored` | killed (`Rule7_AdoptWithAnIgnoredFileInTheFolder_IsRefused`) |
| M9 package report: pin filter always true | killed (4 tests) |
| M10 D3 first-pass line back in | killed (`Upsert_KeepsShadowPropertyValues`) |
| M11 D2 default back in | killed (`AuditedEntity_HasNoDatabaseSpecificDefaultOnCreatedOn`) |
| M12 cancellation walk stops at the top exception | killed (2 tests) |

Not done: the non-Windows fixture branches could not be run (Windows-only machine). The reference baseline was not rebuilt (its content is consistent with the reference source everywhere I checked, apart from the `T?` rendering above).

*Status: COMPLETE — reviewer (second reader), 2026-10-08*
