# F3 — The blocks' answer to knowledge-gateway's findings — Summary

Mode: team pipeline (spec)

## Status: COMPLETE

## What Was Built
- The seven block fixes knowledge-gateway's F201-SharedBase named: domain events from aggregates of any key type (C1), a text "who" type for audit fields and caller ids (C2, C11), already-started replies left alone or aborted cleanly (C3), argument errors answered 500 (C4), a design-time factory that builds memory-cache contexts (C5), stored-JSON helpers with options and value comparers (C6), and a duplicate unique value at save answered 409 on SQL Server and PostgreSQL (C12).
- The validation reply as a per-field map in both writers (C7), the endpoint-library set-up split into `UseEnumsAsText` / `UseFieldMapValidationReply` (C8), gRPC client registration moved to its own block `Blocks.Grpc` (C9), and the smaller points: `UseGlobalExceptionMiddleware`, `MigrateAsync` (C10).
- A frozen version-one compatibility project, read-mes corrected, and `docs/public-names.md` updated (120 reference lines changed or gone, 181 current lines changed or new).

## Key Outcomes
- 74 files outside the feature's docs: 35 created (two of them plugin-feedback files), 37 modified, 2 moved; 12 blocks (was 11); new test projects Compatibility, MediatR, FastEndpoints.SetupTests, Grpc.
- **Full suite:** pass — 82.9 s (recorded 2026-10-18T13:50:21.865Z, `dotnet test Blocks.slnx -warnaserror`, tree D:/src/dotnet-building-blocks-F3-GatewayFeedback, hash d4229883…; filtered: no). Handover run before the fix round: pass, 98.4 s.
- **Map delta:** none
- Done check: PASS (Check 1). Code review: Codex primary GO (no findings) + Sonnet second reader APPROVED; 1 fix round of 3 (2 MEDIUM, 2 LOW, all fixed; follow-up review none due — no step rebuilt, no CRITICAL or HIGH); closed on the close predicate.
- Registry: nothing to promote — `Rule list: not applicable` (infrastructure fixes, each pinned by its own test); no business-rule registry in this repo. Anchor check: no manifest. Distill: nothing owed. No M3 flag.
- Step 12 (Owner: close): the plugin note filed — `docs/plugin-feedback/nexus-dotnet-2.0.3-2026-10-18.md` (error-handling skill: 400 → 500 for argument errors, flat list → per-field map).

## Deviations from Plan
- Step 4: the FastEndpoints pre-processor reads the claims reader from the request's services instead of `HttpContext.Resolve` (FastEndpoints 8.3.0 resolves through one process-wide setting, which made the plan's host-less test impossible). Confirmed by the done check and both reviewers.
- Step 9: `Microsoft.Data.SqlClient.SNI.runtime` (proprietary file licence, pulled in by SqlClient) allowed by name in the licence test as a test-only exception — owner ruling 2026-10-18 (Q2); test-only pin `System.Security.Cryptography.Pkcs` 10.0.13 for a published advisory (team lead).
- Built in a separate worktree (owner's choice at launch); `.claude/verify.json` copied there untracked (Q1).

## Notes
- **Plan attribution corrected:** the plan labelled both test libraries "(owner, 2026-10-13)"; the owner had ruled only on PostgreSQL — SqlClient came from the architect's critic fold. Recorded in lessons and plugin feedback.
- **Open, by design:** the minimal-API filter keeps a caller-sent text id (plan R1-02, same rule as the existing `int` ids); the second reader flagged it as a design call, not a defect. The pre-processor is not exercised inside a live endpoint host by any test.
- **Slow-test tag delta pass skipped:** the slow-test tags and their exclusion live only in another session's uncommitted work on `main`; tagging here would collide with it. Re-time after that work lands.
- All mutation batteries were the developers' own; the second reader ran 11 independent mutants (the two survivors became tests in fix round 1).
- Plugin feedback filed: `docs/plugin-feedback/nexus-2.4.37-2026-10-18.md` (worktree carry-over of untracked settings, read-tracker paths in a worktree, runner output truncation, fold attribution).
- Usage record: partial (`usage-record.json`).
- Lessons: recorded in lessons.md, not yet processed by the learner.
