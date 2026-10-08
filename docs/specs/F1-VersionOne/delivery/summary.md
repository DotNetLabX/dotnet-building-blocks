# F1 — Version One of the generic building blocks — Summary

## Status: COMPLETE

## What Was Built
- The reference's 11 generic blocks (`Blocks.*`) moved onto .NET 10 under `src/`, cleaned (namespaces, Async names, product words, comments), with the agreed behaviour fixes (error mapper 403/409/502/499, conflict helper, D2, A2, A4, D4) and a read-me per block.
- Two-way sync tool `tools/Blocks.Sync` (forward / back / status, lock file, refuse-before-write guards), a public-surface tool with `docs/public-names.md`, a foreign-repo snapshot tool, and repo-wide hygiene and portability checks.

## Key Outcomes
- 213 files: 211 created, 2 modified (`.gitignore`, `README.md`), plus the feature's docs.
- Build: `dotnet build Blocks.slnx -warnaserror` — 0 warnings, 0 errors.
- **Full suite:** undeclared — no duration (recorded 2026-10-08T16:03:51Z; complete red on a fast-green tree: not measured; filtered: no). The owner skipped the verify-roles declaration for this run; the main session ran `dotnet test Blocks.slnx -warnaserror` after every round instead — last run 189 tests, 189 passed, exit 0.
- **Map delta:** none
- Done check: PASS (Check 3, after FAIL on 8 skill slips in Check 2). Code review: Codex primary + Sonnet second reader; 2 fix rounds of 3; no CRITICAL or HIGH open; closed on the close predicate.
- Your other repos: the step-13 end check found 0 differences (reference in full; HEAD and `src/BuildingBlocks/` of sprint-rituals and knowledge-gateway).
- Registry: nothing to promote — the owner chose to skip the rule list for this release (2026-10-08); no business-rule registry exists in this repo.

## Deviations from Plan
- Step 6 (D3 upsert): the owner reversed the 8 October ruling (Q4) — the reference's update line is kept; a shadow-property test proves hidden values survive.
- Step 5: 499 only when the client aborted the request (Q5, owner); an upstream timeout wrapped as bad gateway answers a logged 502.
- Step 2: the unused `System.ServiceModel.Primitives` pin dropped (Q6). Step 9: read-me notes that a 5xx other than 500 shows its own message (Q7).
- Steps 3–4: hygiene tests created early (Q3). Step 10: some refusal tests written with their code, proven by guard-removal. Step 13: the suppression test skips Markdown; hygiene tests skip `.claude` folders.
- Block files are LF (repo `.gitattributes`), not the reference's CRLF.

## Notes
- `Blocks.Core.Guard` keeps its name (mixed class; `GuardExtensions` exists) — carry-over in implementation.md.
- Temp baseline folders `%TEMP%\blocks-baseline-fda2eb7*` remain outside the repo.
- Untracked tooling state not committed: `.claude/` and `docs/specs/F1-VersionOne/delivery/.claude/` (hook output); the foreign-repo records are git-ignored.
- The persistence-patterns skill still prescribes the dropped upsert line (logged as a skill gap in lessons.md); sprint-rituals carries the same fix today.
- Lessons: recorded in lessons.md, not yet processed by the learner.
- **Under the bar:**
  - [MEDIUM] The empty-folder deletion helper can traverse a junction at a replacement target and delete external empty directories — `tools/Blocks.Sync/SafeFiles.cs:75` (Codex)
  - [LOW] Adoption misses hidden deletions (a committed file deleted under `--skip-worktree`) — `tools/Blocks.Sync/FirstTake.cs:57` (Codex)
  - [LOW] `Blocks.SurfaceDump` crashes with an unhandled `DirectoryNotFoundException` on a missing assembly path; should report it and find the packages folder via `dotnet nuget locals` — `tools/Blocks.SurfaceDump/SurfaceDumper.cs:19` (team-lead)
  — **Carry-over row:** BUG-1-F1-VersionOne-carry-over
