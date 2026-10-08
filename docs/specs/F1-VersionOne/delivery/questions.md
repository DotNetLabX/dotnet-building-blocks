# F1 — Version One of the generic building blocks — Questions

## Q1: What produces the foreign-repo snapshot
**From:** developer
**To:** architect
**Status:** Answered
**Step:** 1 — Repo skeleton and the foreign-repo snapshot (re-used by step 13)
**File:** `docs/specs/F1-VersionOne/delivery/foreign-repos-before.txt`

**Context:** Step 1 asks for the record (HEAD, `status --porcelain --ignored`, SHA-256 of `diff HEAD --binary`, SHA-256 per untracked file, last-write time per ignored entry) but does not say what writes it. Step 13 must take "the same record" again and compare it for equality, so the producer has to be re-runnable with byte-identical output formatting. The plan rules out PowerShell 7 ("every tool is C#"); no tool project for this is planned.

**Question:** Which producer should write the record so step 13 can re-run it unchanged?

**Recommendation:**
A small console project `tools/Blocks.ForeignSnapshot/` (net10.0, in `Blocks.slnx` under `tools`) that takes the three repo paths and an output file, runs only `--no-optional-locks` git reads, and writes a deterministic sorted text record (Recommended) — one more project, but step 13's test calls the same code and a format drift is impossible.
A .NET 10 file-based app `tools/foreign-repos-snapshot.cs` run with `dotnet run` — no new project, but it sits outside the solution build, so step 1's zero-warnings gate never compiles it.
One-off shell commands now, with step 13 re-implementing the same format in its test — fastest today, but two implementations of one format must agree byte for byte.
**Confidence:** medium — the plan is silent; the recommendation follows its "every tool is C#" decision and step 13's equality check.

### Answer
Architect, 2026-10-08: your first option — a console project. Plan step 1 is amended to match; the binding points:

- `tools/Blocks.ForeignSnapshot/` (net10.0 console, in `Blocks.slnx` under `tools`, **no package references**),
  command `record --repo <path> --repo <path> --repo <path> --out <file>`. It is built in step 1 with zero
  warnings (so step 1 now evaluates the props files itself). No `compare` command yet — how step 13 compares
  waits on Q2; step 13 re-runs `record` to `foreign-repos-after.txt` and adds the compare then.
- Every git call carries `--no-optional-locks`, `rev-parse` included. Sources, each verified read-only at plan
  time on the three repos (2026-10-08):
  - HEAD: `git rev-parse HEAD`;
  - the listing: `git status --porcelain --ignored`, kept whole as planned;
  - changed tracked content: SHA-256 of `git diff HEAD --binary -- <path>` **per changed path** (paths from
    `git diff HEAD --name-only`), not one hash of the whole diff — so step 13 can compare a folder alone,
    whatever the owner answers on Q2, and a whole-repo comparison is still all lines equal (verified: two runs
    on one sprint-rituals path give the same hash). Git prints line-ending warnings on stderr for those repos —
    the record takes stdout only;
  - untracked files: `git ls-files --others --exclude-standard` — one line per file, so each can be hashed (the
    porcelain listing collapses an untracked folder to one `??` line: e.g. `?? .claude/audit/` in the reference
    is 2 files there; counts today 3 / 18 / 79 files for reference / sprint-rituals / knowledge-gateway);
  - ignored entries: the `!!` lines of the listing, last-write time of each (folders stay collapsed — the
    accepted gap in Must-NOT-Change is unchanged).
- Deterministic text: one section per repo in argument order; lines inside a section sorted ordinal; hashes
  lowercase hex; times UTC, round-trip format, invariant culture; UTF-8 without byte-order mark, LF.
  Accept: two back-to-back `record` runs on the reference alone give byte-identical files.
- **Change from the plan: the record is not committed.** The repo is public and the record lists the file names
  of the owner's private work in knowledge-gateway and sprint-rituals. Step 1 adds
  `docs/specs/F1-VersionOne/delivery/foreign-repos-*.txt` to `.gitignore`; the file stays at the planned path on
  disk. (Decisions row added.)

## Q2: End check on your other repos
**From:** developer (reshaped for the owner by the architect)
**To:** user
**Status:** Answered
**Step:** 1 (record scope) and 13 (the equality check)
**File:** `docs/specs/F1-VersionOne/delivery/foreign-repos-before.txt`

**Context:** You ruled on 8 October that this work edits none of your other three repos, so it checks them at the end.
Your own work in sprint-rituals and knowledge-gateway changes them every day, so a check of everything will fail.

**Question:** Which changes in sprint-rituals and knowledge-gateway should stop the work at the end?

**Recommendation:**
Only changes in their shared code folders (Recommended) — Those folders are where this work takes code from, so a change there stops it and comes to you. Every other change is listed for you to confirm as your own.
Any change at all — Nothing is left out, but your own edits will stop the work until you confirm them, and that will happen.
Everything except their Claude session folders — The session noise goes, but your own file edits there still stop the work.
**Confidence:** medium — the changes are measured today, and the shared code folders are the only place there this work copies from.

**Architect note (agent-facing, not part of the ask):** the developer's analysis, kept for the record.
Measured 2026-10-08 during this analysis. The reference (`dotnet-microservices`, HEAD `fda2eb7`) looks dormant: its only untracked/ignored `.claude/` entries date from 2026-09-10. sprint-rituals has 8 modified and 6 untracked paths and `.claude/audit/` written 2026-10-05; knowledge-gateway has 6 modified and 13 untracked paths and its `.claude/.current-agent`, `.personas.json` and `audit/` were written today at 08:31 — another session is active there. The owner's own work or any Claude session in those repos between step 1 and step 13 changes the untracked-file hashes, the porcelain list or the ignored-entry times, so step 13's "equals it" fails for reasons this run did not cause, and the check cannot tell the two apart.

Developer's original options: (1) compare strictly the HEADs, the whole reference, and in the other two repos
their `src/BuildingBlocks/` folders, listing every other difference in `implementation.md` for the owner to
attribute; (2) compare everything strictly — the plan as written; (3) everything except each repo's `.claude/`.

Architect's reading, re-verified read-only 2026-10-08: sprint-rituals HEAD `4affe6a` with 8 modified and 6
untracked entries (one under `src/Services/Poker/`), knowledge-gateway HEAD `11b37258` with 6 modified and 13
untracked entries, the reference `fda2eb7` with only `.claude/` noise. Why this goes to the owner: the guard is
the owner's (spec § Out of Scope, "Editing the reference, sprint-rituals or knowledge-gateway — this repo only"),
and options 1 and 3 narrow what the plan's pin of it fails on. Why option 1 still holds the guard: in those two
repos the plan reads only `src/BuildingBlocks/` (pattern files for steps 5–7), sprint-rituals' test project files
(setup pattern) and knowledge-gateway's hand-off doc; the one way this run could plausibly write there is a build
or restore in a block folder, which lands in `src/BuildingBlocks/**/bin|obj` — inside the strict scope, ignored
entries' times included. The listed-not-failed differences still reach the owner, who confirms them as theirs
before the close. Under option 1 the strict scope is: HEAD of all three; the whole reference; in sprint-rituals
and knowledge-gateway every listing line, diff content and untracked/ignored entry under `src/BuildingBlocks/`.
Not needed before slice 3: step 1 records the full state under every option.

### Answer
User, 2026-10-08 (relayed verbatim by the team lead):

"option 1: only changes in their shared code folders. Confidence: high. The build's only realistic way to touch
those repos is the sync tool writing into a blocks folder, and option 1 watches exactly those folders.

Why the other two are worse:
- Option 2 (any change at all) would stop the work for no reason. Your other sessions, including seven idle
  knowledge-gateway ones, keep changing those repos, so a full check would fail on your own edits.
- Option 3 (everything except session folders) fails the same way, just with less noise.

The reference repo isn't part of this question and stays fully checked, which is right since nobody edits it day
to day."

Architect, applied 2026-10-08: plan step 13's Accept, Must-NOT-Change row and Open Questions amended to the
option-1 scope stated in the architect note above.

## Q3: Hygiene tests named as gates for steps 3 and 4 do not exist until step 13
**From:** developer
**To:** architect
**Status:** Answered
**Step:** 3 — Names; 4 — Comments
**File:** `tests/Blocks.Hygiene.Tests/`

**Context:** Step 3 says "The gates are step 13's hygiene tests (namespace per folder, Async names, product words), run at the end of this step too"; step 4 says "Step 13's marker and commented-out-code tests pass". The project `tests/Blocks.Hygiene.Tests/` is created in step 13, which belongs to the third slice. The Async-name check is defined over the loaded block assemblies, which a grep cannot reproduce exactly.

**Question:** At the end of steps 3 and 4, should I create those hygiene tests early, or check with equivalent scans and leave the tests to step 13?

**Recommendation:**
Create `tests/Blocks.Hygiene.Tests/` in step 3 with the three tests it names (namespace per folder, Async names over the loaded block assemblies, product words), add the marker and commented-out-code tests in step 4, and let step 13 add the rest (Recommended) — steps 3 and 4 get the exact gates the plan names, and step 13 shrinks; each test is seen failing on a planted violation when written.
Run equivalent scans (grep for namespaces, markers, product words; the step-2 surface dump filtered for task-returning members without "Async") and record their output in `implementation.md`; write the tests in step 13 as planned — keeps step boundaries, but the gate is an approximation of the test.
**Confidence:** medium — both satisfy the intent; the first matches the plan's wording "run at the end of this step too".

### Answer
Architect, 2026-10-08: your first option — the plan meant the tests to exist by then ("run at the end of this
step too"); the ordering was a plan defect. Steps 3, 4 and 13 are amended:

- **Step 3** creates `tests/Blocks.Hygiene.Tests/` (xUnit v3 + AwesomeAssertions, in `Blocks.slnx` under `tests`,
  referencing all 11 block projects) with three of step 13's tests: namespace per folder in `src/`, Async names
  over the loaded block assemblies, product words in `src/`. Each is written **before** the renames and seen
  failing on the real pre-step code — that counts as its planted violation for step 13's Accept (record the red
  output in `implementation.md`). TDD is now `yes` for step 3.
- **Step 4** adds the marker test and the commented-out-code test the same way: red on the pre-step-4 code, green
  after. The deterministic-DLL check is unaffected (the test project is not a block). TDD `yes` for step 4.
- **Step 13** adds the remaining tests (byte-order mark, suppressions, package versions, licences, read-mes) to
  the existing project; its Accept is unchanged — it covers all of them.
- The tests then run in every later build, which is what the Testing Strategy already says.

## Q4: Keep or undo the update fix
**From:** developer (reshaped for the owner by the architect — the architect's probe changed the question)
**To:** user
**Status:** Answered
**Step:** 6 — EF Core fixes (D3)
**File:** `src/Blocks.EntityFrameworkCore/Repositories/Repository.cs` (`UpsertAsync`), `tests/Blocks.EntityFrameworkCore.Tests/RepositoryTests.cs`

**Context:** You ruled on 8 October to take sprint-rituals' fix so values kept in private fields are saved on update.
On the .NET 10 data library the old code already saves those values, and a test shows it.
The fix also erases values the code keeps out of sight, such as a link to a parent record, on every update.

**Question:** Should version one keep the old update code instead of that fix?

**Recommendation:**
Keep the old update code (Recommended) — Private-field values are still saved, and a new test proves the hidden values survive too. The fix you approved is dropped because nothing here needs it.
Keep the fix as approved — Every update through this method erases hidden values, such as parent links, in the apps that take this block.
Keep the fix but leave hidden values alone — Both kinds of value survive, but it is new code that fixes nothing today.
**Confidence:** high — both versions were run today on the exact data library version this repo uses.

**Architect note (agent-facing, not part of the ask):** probe run 2026-10-08 in the session scratchpad (outside
this repo; no source touched), EF Core Sqlite 10.0.12. Entity `Note` with a field-only `_text` (field access), a
shadow foreign key `ParentId` (navigation, no CLR FK property) and a shadow column `Tag`; existing row
`ParentId=1, Tag=keep`; upsert of a detached `Note(1, "new")`:

| Line | `_text` | `ParentId` | `Tag` |
|---|---|---|---|
| reference: `CurrentValues.SetValues(entity)` | new | 1 | keep |
| D3 (sprint-rituals): `CurrentValues.SetValues(Entry(entity).CurrentValues)` | new | **null** | **null** |

Mechanism: `SetValues(object)` of the entity's own CLR type reads through the model's getters (field access
honoured) and skips shadow properties; `SetValues(PropertyValues)` copies every property, and a detached entry's
shadow values are defaults. So on EF Core 10 the D3 line fixes nothing for the shape the spec names and adds a
regression for any entity with a shadow property (shadow foreign keys are common). Not settled here because it
reverses the owner's D3 ruling (spec § What version one contains, row D3).

If option 1: revert `Repository.cs` `UpsertAsync` to the reference line; keep
`Upsert_UpdatesAPropertyStoredInAPrivateField` as a pin (born green — a must-not check, no waiver needed); add
`Upsert_KeepsShadowPropertyValues` (shadow FK and shadow column survive an upsert), red on the current D3 line
first. The spec's D3 row then needs the PO's write-back (spec is the PO's), and `docs/public-names.md` is
unaffected (no surface change). Separate heads-up for the owner: sprint-rituals carries the D3 line today.
If option 2: the developer's red-first waiver stands as asked, and the regression ships. If option 3: one
non-shadow copy loop in `UpsertAsync` plus both tests — new code with no shape that needs it.

Developer's original question, kept for the record. Step 6 says each test is red on the old code first. The D3 test the plan describes — an existing entity whose property is mapped to a private field (`PropertyAccessMode.Field`), updated through upsert, read back from a fresh context — passes on the old line `SetValues(entity)` with EF Core 10.0.12 (`Upsert_UpdatesAPropertyStoredInAPrivateField`; the entity has no CLR property at all, only the field `_text`).
EF Core's `SetValues(object)` reads through the model's own getters (which honour field access) when the object is the entity's own CLR type; a probe showed it even fills `_text` from an anonymous object with a member of that name. Reflection on CLR properties only happens for an object of another type, which `UpsertAsync(TEntity)` does not receive in any shape I could build without adding a proxy package.
The D3 line is changed anyway (`SetValues(_dbContext.Entry(entity).CurrentValues)`, sprint-rituals' line); the test passes on the new line too. In my mutation check, reverting the line is the one survivor.

Developer's question: should D3 stand with a test that pins the behaviour but cannot be shown red on the old code?

Developer's options:
Keep the changed line and the test as a regression pin, and record in `implementation.md` that red-first is impossible for this shape on EF Core 10 (Recommended) — the spec's acceptance item (upsert a field-backed entity, read it back from a fresh context) is met; only the plan's red-first is waived for this one test.
Look for an entity shape where the old line fails (for example lazy-loading proxies) and test that — adds a test-only package and an unrealistic set-up for a block that does not use proxies.
Revert D3 as not a bug on EF Core 10 — goes against the owner's D3 decision in the spec.
Developer's confidence: high — the spec's acceptance item is met either way, and the new line is the one the persistence-patterns skill prescribes.

### Answer
User, 2026-10-08 (relayed verbatim by the team lead): "Keep the old update code (Recommended)"

Architect, applied 2026-10-08: plan step 6 (D3 bullet: revert to the reference's line, keep the private-field pin,
add `Upsert_KeepsShadowPropertyValues` red first on the current line), its Skill Mapping row, Domain Model Changes,
Must-NOT-Change (16 → 17 kept conflicts) and Open Questions; spec § Departures (count and D3 row), the upsert
acceptance criterion, and § Answers for knowledge-gateway (question 2 and 3 lines).

## Q5: When a cancellation answers 499
**From:** reviewer (second reader, review-second.md § Open Questions 1), via the team lead; shaped by the architect
**To:** user
**Status:** Answered
**Step:** 5 — Error types and the error mapper (D1)
**File:** `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`

**Context:** You approved on 8 October that a cancelled request answers status 499, also when another error wraps it.
So a call to another service that times out, reported as a bad-gateway error, also answers 499 and logs nothing.

**Question:** Should status 499 apply only when the client actually went away?

**Recommendation:**
Only when the client went away (Recommended) — An upstream timeout then answers 502 and is logged as a failure. A real client hang-up still answers 499, wrapped or not.
Any cancellation, as approved — Upstream timeouts keep answering 499, and they stay out of the error log like a client hang-up.
**Confidence:** medium — it fits the reason you gave for the rule, but it changes a case your approved test list names.

**Architect note (agent-facing, not part of the ask):** today the catch is
`catch (Exception ex) when (IsCausedByCancellation(ex))` (GlobalExceptionMiddleware.cs:40), with no check of
`context.RequestAborted`. An `HttpClient` timeout raises `TaskCanceledException`; wrapped as
`BadGatewayException("…", ex)` it matches and answers 499, unlogged. D1's stated reason (spec § Departures, D1 "Why":
"a cancelled request was logged as a server failure") is about the client cancelling, and its text says "a cancelled
request". But the acceptance item "a cancellation wrapped in another error gives 499" is unconditional, and the
reference answered 499 for any bare cancellation — so the condition changes approved behaviour; not settled here.
If option 1: the filter becomes `IsCausedByCancellation(ex) && context.RequestAborted.IsCancellationRequested`,
for bare and wrapped alike; a cancellation without a client abort falls through to the normal mapping (a wrapping
`HttpException` → its own status; a bare cancellation → 500, logged). Tests: `Cancellation_Answers499` and
`CancellationWrappedInAnotherError_Answers499` cancel the request's abort token; new
`UpstreamTimeoutWrappedAsBadGateway_Answers502` (red first) and `CancellationWithoutClientAbort_IsNot499`. The
spec's D1 row and that acceptance item are rewritten to say "when the client's request was aborted" (the spec is
architect-authored). If option 2: nothing changes.

### Answer
User, 2026-10-08 (relayed verbatim by the team lead, after it showed the change as the line-40 filter
`context.RequestAborted.IsCancellationRequested && IsCausedByCancellation(ex)` with the side effect that an unwrapped
upstream timeout then falls through as a logged 500): "yes, that is the right aproach, agree" — option 1.

Architect, applied 2026-10-08: plan step 5 (mapper bullet and tests), plan Open Questions; spec § Departures D1 row
and the error-mapper acceptance item.

## Q6: The unused System.ServiceModel.Primitives pin
**From:** developer (implementation.md § Carry-Over Findings 1), confirmed by both readers; via the team lead
**To:** architect
**Status:** Answered
**Step:** 2 — central versions file; 9 — block read-mes
**File:** `Directory.Packages.props`, `src/Blocks.AspNetCore/README.md`, `src/Blocks.FastEndpoints/README.md`

**Context:** Plan step 2 carried the reference's three pins over. On .NET 10 `System.ServiceModel.Primitives` is in
no block's graph: it came only through protobuf-net 2.4.8, which the protobuf-net 3.x pin replaces.

**Question:** Drop or keep the pin?

**Recommendation:**
Drop it (Recommended) — the package report stops telling every app to pin a package it never resolves.
Keep it — harmless to the build, but a false entry in every app's package file.
**Confidence:** high — re-verified by the architect.

### Answer
Architect, 2026-10-08: **drop it.** Re-verified: `dotnet list Blocks.slnx package --include-transitive` resolves
protobuf-net 3.2.56, protobuf-net.Core 3.2.56 and protobuf-net.Grpc 1.2.2, and no ServiceModel package in any
project. A plan instruction, not an owner ruling (the spec does not name the pin), and no behaviour changes — a
central pin adds no reference. Fix round: remove the `System.ServiceModel.Primitives` line from
`Directory.Packages.props`; remove it from the "Pinned in the central versions file" lines of the AspNetCore and
FastEndpoints read-mes. Plan step 2 amended; Decisions row added.

## Q7: The client-facing message of a 5xx other than 500
**From:** reviewer (second reader, review-second.md § Open Questions 2), via the team lead
**To:** architect
**Status:** Answered
**Step:** 9 — block read-mes
**File:** `src/Blocks.AspNetCore/README.md`

**Context:** Outside development only a 500 gets the fixed text; a 502 or 503 carries its exception's own message,
as D1 words it.

**Question:** Change the behaviour or document it?

**Recommendation:**
Document it (Recommended) — the behaviour is the owner-approved D1; the application writes those messages.
**Confidence:** high — D1 limits the fixed text to 500.

### Answer
Architect, 2026-10-08: **leave the behaviour, document it.** Fix round: one line in `src/Blocks.AspNetCore/README.md`
(under `## Purpose` or `## Registration`): outside development only a 500 is replaced by the fixed text; every other
status carries its exception's message, so write those messages for the client. Plan step 9 amended.
