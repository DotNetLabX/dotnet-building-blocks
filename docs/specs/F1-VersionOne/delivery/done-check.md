# F1 — Version One of the generic building blocks — Done-Check

## Step 1 — Done-Check

### Check 1 — 2026-10-08 — after the build

**Model:** claude-opus-5-5

Inputs: `plan.md` (13 steps, build slices 1–4 · 5–9 · 10–13), `implementation.md` (footer `COMPLETE — developer, 2026-10-08`), `.claude/audit/skill-invocations.log` and `.claude/audit/violations.log` (main checkout; no worktree — `git rev-parse --git-common-dir` is this checkout's `.git`), `.claude/audit/handoff.jsonl`. Handover run: `verify-run --profile complete` → `undeclared` (no `roles.full`); the team lead's `dotnet test Blocks.slnx -warnaserror` → 148/148 across 7 test projects, exit 0 (SurfaceDump 9, Hygiene 12, AspNetCore 22, EntityFrameworkCore 9, FastEndpoints 2, Sync 83, Portability 11 — the same per-project counts `implementation.md` reports).

Pre-commitment predictions: (1) mapped skills on the Follow steps invoked from memory, not logged — not found; (2) step 12 diff lines left unaccounted or spec rows missing — not found (93/99 lines accounted per block, the spec's table section present); (3) step 13 `compare` listing foreign-repo differences owed to the owner — not found (`cmp` re-run here: before and after records byte-identical).

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Repo skeleton and the foreign-repo snapshot | Implemented | Both record files git-ignored (`git check-ignore` → `.gitignore:65`). Props file at the repo root as the plan says (the skill shows `src/`) — plan-sanctioned. |
| 2 — Move the 11 blocks onto .NET 10; surface tool and reference baseline | Deviated (valid) | LF instead of the reference's CRLF — required by the repo's own `.gitattributes eol=lf` (step 1). Every warning fix listed with file and line. `surface-reference.txt` present; `Directory.Packages.props:25` MediatR 12.5.0. |
| 3 — Names: namespaces, convention renames, product wording | Implemented | Three hygiene tests red on the pre-step code (output recorded), green after. Grep of `src tools tests` for every old name in the rename table (`FastEnpoints`, `Behaviours`, `Blocks.Linq`, `Blocks.Core.Extensions`, `Blocks.Domain.Entities`, `RegexExtension`, `GetArticleId`, `articleCommand`): zero hits. |
| 4 — Comments to the conventions | Deviated (valid) | Extra removals (`[Course.AdvancedC#]` tags, two to-do-like lines) rest on spec item 5; the comment-only invariant is pinned by 11 byte-identical deterministic DLLs. All 8 kept `///` blocks listed with reasons. Marker grep over `src tools`: zero hits. |
| 5 — Error types and the error mapper (D1, A1) | Implemented | Three new exception files present; 20 mapper tests, red-first recorded per case, born-green cases named. |
| 6 — EF Core fixes (D2, D3, A4) and the conflict helper | Superseded → Implemented | First-pass D3 superseded by the owner's Q4 ruling (`questions.md` Q4; plan step 6 amended); the redo block restores `Repository.cs:58` `SetValues(entity)` and adds `Upsert_KeepsShadowPropertyValues`, red on the first-pass line. `GETUTCDATE`/`DefaultDateSql`: zero hits. Departure from the persistence-patterns skill's line is the plan's (owner) decision, logged as a skill gap. |
| 7 — FastEndpoints publisher (D4) | Implemented | `DomainEventPublisher.cs:9` casts to `IEvent`; both tests red first. |
| 8 — Claims provider fix (A2) | Implemented | `HttpContextProvider.cs:24–25` reads `claimName`; red first. |
| 9 — Block read-mes | Implemented | 11 `src/*/README.md`; gated by step 13's read-me tests. |
| 10 — The sync tool and the root read-me | Deviated (valid) | Some refusal-branch tests born green (written with the first forward slice); disclosed, and each such branch is killed by the recorded mutation battery. `tdd` is in the log for this step. One named test per plan item, mapped in the step's table. |
| 11 — Portability check | Implemented | 11/11 in the handover run; planted failure (empty central file → NU1010) recorded. |
| 12 — Public-surface comparison and the names list | Implemented | `docs/public-names.md` (141 lines): per-block sections, wire names, the spec's table; every diff line accounted, no question raised. |
| 13 — Repo-wide hygiene tests | Deviated (valid) | Suppression test skips `*.md` (plan/spec name the four forms as prose — a literal whole-repo scan fails on the plan itself); walker skips `.claude` (tooling state). Each test seen failing on a planted violation; `compare` exercised on hand-made records and run on the real pair → exit 0, nothing listed. |

Skill conformance (scored against the log): every non-`None` mapping is in the scoped window — token `developer:implement`, session `49e5d440…`, agent `developer` — and lines up with the hand-off times by step: central-package-management (step 1, 09:51Z); framework-currency + central-package-management (step 2, 10:21Z); tdd (step 3, 10:32Z; step 4, 10:36Z); error-handling + tdd (step 5, 10:40Z); persistence-patterns + tdd (step 6, 10:45Z; redo 11:35Z); domain-patterns + tdd (step 7, 10:50Z); service-infra-conventions + tdd (step 8, 10:52Z); tdd (step 10, 11:38Z, the slice-3 developer spawned 11:38:19Z). `## Skills Used` agrees with the log row for row. No slips.

Test-entry conformance: `violations.log` holds no `rule: "test-entry"` line in either home (only read-discipline re-read notices). No slips.

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist — no rule list (spec header `Rule list: not landed`), no step cites a rule.

Plan hygiene: `## Decisions` present with 11 rows.

Notes for the close (not step findings):
- Untracked tooling state not covered by `.gitignore`: `.claude/.worktree-target`, and a stray `docs/specs/F1-VersionOne/delivery/.claude/audit/` (`fleet-state.json`, `lessons-writes.log`, created 09:41–09:51Z by hook tooling, not by the developer). Neither should be staged.
- Carry-over findings in `implementation.md` (11 rows, all low) are for the reviewer and the architect; none changes a disposition here.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-08*

### Check 2 — 2026-10-08 — after fix cycle 1

**Model:** claude-opus-5-5
**Re-checks:** check 1

Why it runs: Check 1 failed nothing, but plan steps 2, 5 and 9 were amended after it (`questions.md` Q5–Q7), and the fix round rebuilt work under steps that map skills. Window: from `Fix dispatched: 2026-10-08T13:53:07Z`; the round's developer is `a5d57cc44a2635465` (handoff: spawned 13:53:13Z, last hand-back 14:13:16Z), token `developer:implement`, session `49e5d440…`. Inputs read: amended plan steps 2, 5, 9 and `## Decisions`; `implementation.md` § Fix Round 1 and § Skills Used; both skill-log homes; `violations.log`. Handover run after the round: `dotnet test Blocks.slnx -warnaserror` 183/183, exit 0 (AspNetCore 27, FastEndpoints 2, EntityFrameworkCore 10, SurfaceDump 10, Hygiene 13, Sync 110, Portability 11).

| Step | Disposition | Notes |
|------|-------------|-------|
| 2 — amended (Q6: no `System.ServiceModel.Primitives` pin) | Implemented | `ServiceModel`: zero hits in `Directory.Packages.props`, the block project files and the block read-mes. Surface tool fix (row 6) and the regenerated `surface-reference.txt` recorded, red first. |
| 5 — amended (Q5: 499 only on a client abort) | Implemented | `GlobalExceptionMiddleware.cs:40` is `when (context.RequestAborted.IsCancellationRequested && IsCausedByCancellation(ex))`; `UpstreamTimeoutWrappedAsBadGateway_Answers502` (`GlobalExceptionMiddlewareTests.cs:89`) and `CancellationWithoutClientAbort_IsNot499` (`:78`) exist, both recorded red on the old filter; the existing 499 tests now cancel the abort token. |
| 9 — amended (Q7: only a 500 gets the fixed text) | Implemented | `src/Blocks.AspNetCore/README.md:9–10` states it; the AspNetCore and FastEndpoints read-mes now name only `protobuf-net` and `protobuf-net.Core` as pins. |
| Other steps the round touched (3 row 7, 4 rows 8/12, 6 row 11, 10 rows 1–5/9–11, 12, 13) | Implemented | Content per § Fix Round 1. The three renamed registration classes are on disk and in `docs/public-names.md` (lines 35, 83, 95). |

Skill conformance for the round: the scoped window (both homes, `ts >= 13:53:07Z`) is **empty** — no `Skill` invocation of any kind. The developer disclosed it (§ Fix Round 1 Deviations; § Skills Used, row "Fix round 1"). The `tdd`, `error-handling` and `persistence-patterns` entries earlier under the same token belong to other developers (`af1f42…`, `a38e…`), before the fix was dispatched. They are outside this re-check's window and cannot stand for a fresh spawn that never loaded the skill. A disclosed red-first record is a self-report, and the log is what proves an invocation.

**Process slips:**
- skill slip — step 2: central-package-management (row 13 edited the central versions file; framework-currency not exercised — no version moved)
- skill slip — step 3: tdd (row 7 — new `EveryClassOfExtensionMethodsInTheBlocks_EndsInExtensions` test and the renames)
- skill slip — step 4: tdd (row 8 — the widened commented-out-code rule)
- skill slip — step 5: error-handling (rows 11, 15)
- skill slip — step 5: tdd (rows 11, 15)
- skill slip — step 6: persistence-patterns (row 11 — `DeleteById_UsesTheKeyColumnTheModelMaps`)
- skill slip — step 6: tdd (row 11)
- skill slip — step 10: tdd (rows 1–5, 9–11)

Test-entry conformance: no `rule: "test-entry"` line in the window (only two read-discipline notices). No test slips.

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist — no rule list.

Each slip clears when the skill-use log holds the mapped skill for that step in the next round's window. The content above needs no rework: the next round's developer invokes each mapped skill, checks the round's work on that step against it, and records any change in `implementation.md`.

**Verdict: FAIL — skill slips: step 2 central-package-management; steps 3, 4, 5, 6, 10 tdd; step 5 error-handling; step 6 persistence-patterns (fix round 1 invoked no skill). No missing step; amended steps 2, 5, 9 implemented.**

*Status: COMPLETE — architect, 2026-10-08*

### Check 3 — 2026-10-08 — after fix cycle 2

**Model:** claude-opus-5-5
**Re-checks:** check 2

Window: from `Fix dispatched: 2026-10-08T14:23:45Z`. The round's developer is `a85fc533ca9fefb20` (handoff: spawned 14:23:50Z, hand-back 14:34:19Z), token `developer:implement`, session `49e5d440…`, and no other developer ran in the window. Both skill-log homes were read: the main checkout's `.claude/audit/`, and the stray `docs/specs/F1-VersionOne/delivery/.claude/audit/`, which holds no skill log. Handover run after the round: `dotnet test Blocks.slnx -warnaserror` 189/189, exit 0 (AspNetCore 27, FastEndpoints 2, EntityFrameworkCore 10, SurfaceDump 10, Hygiene 13, Sync 116, Portability 11).

Check 2's items, each against the window's skill log:

| Check 2 item | Log entry in the window | Status |
|---|---|---|
| step 2: central-package-management | `nexus-dotnet:central-package-management` 14:24:00Z | cleared |
| step 3: tdd | `nexus:tdd` 14:24:02Z | cleared |
| step 4: tdd | `nexus:tdd` 14:24:02Z | cleared |
| step 5: error-handling | `nexus-dotnet:error-handling` 14:24:03Z | cleared |
| step 5: tdd | `nexus:tdd` 14:24:02Z | cleared |
| step 6: persistence-patterns | `nexus-dotnet:persistence-patterns` 14:24:04Z | cleared |
| step 6: tdd | `nexus:tdd` 14:24:02Z | cleared |
| step 10: tdd | `nexus:tdd` 14:24:02Z | cleared |

All four calls come before the round's first edit, as § Fix Round 2 states. One `tdd` load serves the five steps: a single developer in one window, and the mapped skill is present for each step it redid. `## Skills Used` (row "Fix round 2") matches the log. § Fix Round 2 records a check against each skill. The tdd check found one real survivor (the default-source root) and added `DefaultSource_IsTheRootOfThatCheckout`; the other skills changed nothing.

The round's other row (fix-list row 2, step 10 — empty folders in the topology preflight) is covered by the same `tdd` load. Its first test was recorded red, and four born-green topology tests are backed by the recorded battery. This is review material; it adds no done-check item.

Test-entry conformance: `violations.log` has no line at all in the window. No test slips.

No step disposition changes from Checks 1–2: all 13 steps are Implemented, Deviated (valid) or Superseded → Implemented, and amended steps 2, 5 and 9 are implemented (Check 2).

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist — no rule list.

Note for the coordinator: `review.md` § Step 2 — Fix list, cycle 2 writes its items as a table and its under-bar block under a `### Under the bar` heading. The `review-format` layout asks for numbered rows and a bold line, so that a heading does not enter the section map. This is format only and is not a done-check item.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-08*
