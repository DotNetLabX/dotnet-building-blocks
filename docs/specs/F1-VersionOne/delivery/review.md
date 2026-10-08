# F1-VersionOne — Review

## Step 2 — Code Review

**Verdict:** REQUEST CHANGES
Primary: Codex — NO-GO, persisted by team-lead
**Model:** codex (Codex CLI default; job task-muzhze2z-7vfl5c)
Source: `review-codex.md` (verbatim). Second reader: `review-second.md`. Done check: `done-check.md`.

### [HIGH] Lock contents are never validated against their recorded commit
**File:** `tools/Blocks.Sync/SyncSession.cs:33`
**Issue:** only commit existence is checked; a syntactically valid lock with wrong file lists or fingerprints becomes a false comparison base and can let an unsent app edit be overwritten.
**Fix:** compare each lock entry with the block's normalized file set and fingerprints at its recorded commit; reject inconsistencies before any write.

### [HIGH] Adoption can overwrite edits hidden from git status
**File:** `tools/Blocks.Sync/FirstTake.cs:12`
**Issue:** a committed file marked `assume-unchanged` can hold local edits while the check reports a clean folder; `forward --adopt` then replaces content that history cannot recover (Rule 7).
**Fix:** verify the physical file set and contents against committed blobs; add a temporary-repo test with an edited `assume-unchanged` file.

### [MEDIUM] Mirroring fails when a file becomes a directory or the reverse
**File:** `tools/Blocks.Sync/SafeFiles.cs:36`
**Issue:** new files are written before obsolete ones are removed, so `Data` → `Data/item.json` fails after earlier writes already happened.
**Fix:** preflight topology changes and remove conflicting obsolete entries before creating replacements, preserving excluded files; test both directions forward and back.

### [MEDIUM] Package-report errors occur after blocks and the lock changed
**File:** `tools/Blocks.Sync/ForwardCommand.cs:94`
**Issue:** malformed app package XML or a missing source central file exits 2 after the copy and lock refresh.
**Fix:** build and validate the package report before the first write; test that these failures leave both repos unchanged.

### [MEDIUM] The app/source root itself bypasses the junction check
**File:** `tools/Blocks.Sync/Containment.cs:41`
**Issue:** `CheckNoLinks` starts below `root`, so a junction passed as `--app` or `--source` permits writes through it.
**Fix:** include the root in link validation and preflight the lock's parent as well as block destinations.

### [MEDIUM] Surface dump misses unconstrained generic nullability changes
**File:** `tools/Blocks.SurfaceDump/TypeNames.cs:89`
**Issue:** `DeserializeCaseInsensitive<T>` changed `T` → `T?` but both dumps print `T?`; cache return/out annotation changes are hidden too, so `docs/public-names.md` is incomplete.
**Fix:** preserve declaration-level generic nullable annotations, add unconstrained `T` → `T?` fixtures, regenerate the comparison and names list.

### [LOW] Extension-class naming pass is incomplete
**File:** `src/Blocks.Core/Mapster/DependencyInjection.cs:7`
**Issue:** Core's and Messaging's `DependencyInjection` classes and Hasura's `HasuraRegistration` lack the `Extensions` suffix.
**Fix:** rename, update references and public names, add an extension-class naming check.

### [LOW] Commented-out inheritance remains
**File:** `src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs:5`
**Issue:** `//:IMemoryCache` violates the comment clean-up and escapes the whole-line comment gate.
**Fix:** remove it; extend the gate to inline commented-out declarations.

### [LOW] Invalid CLI paths can escape the exit-code contract
**File:** `tools/Blocks.Sync/SyncCli.cs:54`
**Issue:** `Path.GetFullPath` runs outside the handled `try`; an empty path throws instead of returning 2.
**Fix:** validate paths inside handled parsing and return the documented usage error.

Carry-over findings table: all 12 rows confirmed by Codex (see `review-codex.md`).

## Step 2 — Fix list, cycle 1
Bar: all

Merged by team-lead from Codex (primary, `review-codex.md`), the second reader (`review-second.md`) and the done check (Check 1 — PASS, no items); rulings Q5–Q7 from `questions.md`.

1. [HIGH] Validate lock contents (file lists, fingerprints) against the block at the lock's recorded commit before any write — `tools/Blocks.Sync/SyncSession.cs:33` (Codex)
2. [HIGH] `forward --adopt` must verify physical files against committed blobs (an `assume-unchanged` edit refuses); temp-repo test — `tools/Blocks.Sync/FirstTake.cs:12` (Codex)
3. [MEDIUM] File↔directory topology changes: remove conflicting obsolete entries before writing replacements, keep excluded files; test both directions, forward and back — `tools/Blocks.Sync/SafeFiles.cs:36` (Codex)
4. [MEDIUM — contested LOW/MEDIUM: a failure after writes leaves the app half-changed, against the refuse-before-writing rule] Build and validate the package report before the first write; failures leave both repos unchanged — `tools/Blocks.Sync/ForwardCommand.cs:94` (Codex, second reader)
5. [MEDIUM] Include the `--app`/`--source` root and the lock's parent in the link/junction check — `tools/Blocks.Sync/Containment.cs:41` (Codex)
6. [MEDIUM] Surface dump keeps declaration-level nullable annotations on unconstrained generics; `T` vs `T?` fixture; regenerate the comparison and `docs/public-names.md` — `tools/Blocks.SurfaceDump/TypeNames.cs:89` (Codex, second reader)
7. [LOW] Rename Core's and Messaging's `DependencyInjection` and Hasura's `HasuraRegistration` to the `…Extensions` convention; naming check — `src/Blocks.Core/Mapster/DependencyInjection.cs:7` (Codex)
8. [LOW] Remove `//:IMemoryCache`; extend the comment gate to inline commented-out declarations — `src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs:5` (Codex)
9. [LOW] Validate CLI paths inside the handled parse so an empty path returns exit 2 — `tools/Blocks.Sync/SyncCli.cs:54` (Codex)
10. [LOW] `blocks/` and `./blocks` are refused with a message naming the wrong cause — `tools/Blocks.Sync/Containment.cs` (second reader)
11. [LOW] Pin by tests: the mapper logs only 500+; delete-by-id reads the model's key column; the default `--source` — test projects (second reader)
12. [LOW] Remove the narration comments the spec's comment rule (item 5) covers, across all blocks — `src/` (second reader)
13. ruling Q6 — drop the `System.ServiceModel.Primitives` pin and its read-me mentions — `Directory.Packages.props` (architect)
14. ruling Q7 — one read-me line: a 5xx other than 500 shows its own message outside development — `src/Blocks.AspNetCore/README.md` (architect)
15. ruling Q5 (owner) — 499 only when the client went away (`RequestAborted` filter); 502 test red first — `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs:40` (owner)

Follow-up review: due — covers steps none

## Step 2 — Re-review (cycle 1)

**Verdict:** COMMENT
Primary: Codex — NO-GO, persisted by team-lead
**Model:** codex (Codex CLI default; job task-muzmbli0-gp7bjx)
Source: `review-codex.md` (second `## Verdict:` section). Rows 1, 4–15 closed; rows 2 and 3 partial (below).

### [MEDIUM] Empty directories bypass the topology preflight
**File:** `tools/Blocks.Sync/SafeFiles.cs:51`
**Issue:** `EntriesUnder` omits ordinary directories; replacing `Data/Item.cs` with a file `Data` while an empty `Data/Empty/` exists passes preflight, then `Mirror` deletes `Item.cs` and fails writing, leaving the destination half-changed.
**Fix:** account for empty directories in preflight and remove safely replaceable trees before writing, or refuse before any mutation; cover forward and back with empty target and sibling folders.

### [LOW] The physical comparison misses hidden deletions
**File:** `tools/Blocks.Sync/FirstTake.cs:57`
**Issue:** a committed file deleted under `--skip-worktree` is never visited, so adoption proceeds despite a physical file-set difference.
**Fix:** compare both file sets through the same exclusions and refuse missing physical files; add a hidden-deletion regression case.

## Step 2 — Fix list, cycle 2
Bar: MEDIUM

Merged by team-lead from the done check (Check 2 — FAIL) and the Codex re-review (cycle 1).

1. skill slip — steps 2, 3, 4, 5, 6, 10: central-package-management, tdd, error-handling, persistence-patterns not invoked in fix round 1 (done check)
2. [MEDIUM] Empty directories bypass the topology preflight; refuse or remove before any mutation, forward and back — `tools/Blocks.Sync/SafeFiles.cs:51` (Codex)

Follow-up review: due — covers steps none

**Under the bar — recorded, not fixed:**
3. [LOW] Adoption misses hidden deletions (a committed file deleted under `--skip-worktree`) — `tools/Blocks.Sync/FirstTake.cs:57` (Codex)
4. [LOW] `Blocks.SurfaceDump` crashes with an unhandled `DirectoryNotFoundException` on a missing assembly path (two Windows crash events, 2026-10-08 10:29Z); it should report the path and find the packages folder via `dotnet nuget locals` — `tools/Blocks.SurfaceDump/SurfaceDumper.cs:19` (team-lead)

## Step 2 — Re-review (cycle 2)

**Verdict:** COMMENT
Primary: Codex — NO-GO, persisted by team-lead
**Model:** codex (Codex CLI default; job task-muzn2hut-3u80zg)
Source: `review-codex.md` (third `## Verdict:` section). Cycle-2 rows 1 and 2 closed; one new finding.

### [MEDIUM] The empty-folder deletion helper can traverse a junction and delete external empty directories
**File:** `tools/Blocks.Sync/SafeFiles.cs:75`
**Issue:** when the source adds a file named `bin` and the app's `bin` is a junction to an external tree holding empty subfolders, preflight allows the replacement and `DeleteEmptyFolders` (which checks only children for `ReparsePoint`) enumerates through the starting junction and deletes the external empty subfolders — against Rule 4.
**Fix:** reject a linked replacement target in preflight, before any mutation; guard the helper's starting directory; forward/back regressions asserting external directories, block files and the lock stay unchanged.

## Step 2 — Merge, no fix round
Bar: HIGH

Merged by team-lead from the done check (Check 3 — PASS) and the Codex re-review (cycle 2). Nothing reaches round 3's bar (always-in items, HIGH and above), so no round runs.

Merged verdicts: done check PASS (Check 3); Codex NO-GO resting only on an under-bar MEDIUM (review.md verdict COMMENT).

**Under the bar — recorded, not fixed:**
1. [MEDIUM] The empty-folder deletion helper can traverse a junction at a replacement target and delete external empty directories — `tools/Blocks.Sync/SafeFiles.cs:75` (Codex)
