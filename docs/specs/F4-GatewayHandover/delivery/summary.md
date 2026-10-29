# F4 — Knowledge-gateway's hand-over of 2026-10-29 — Summary

Mode: architect-led fast lane (light, plan only)

## Status: COMPLETE

## What Was Built
- Point 1: `UseCustomFastEndpoints(app, Action<Config>? configure = null)` — the caller's settings run after the block's; request property names are read case-insensitively, set explicitly.
- Point 4, first ruling: a named 400 reply type, `FieldMapValidationReply`, declared to the API document (`Errors.ProducesMetadataType`); its three names are pinned, so the reply on the wire is unchanged under any naming policy.
- Point 2: already delivered by F3; a test now pins the malformed-body reply (`errors.serializerErrors`, its text as `message`).
- Point 3: `RequestDiagnosticsOptions` (`SlowRequestMs` 1000, `SlowFileTransferMs` 3000, each at least 1) read by the diagnostics middleware; a `text/event-stream` reply is never warned as slow.
- Point 4, second ruling: a client's `X-Correlation-ID` is kept only when sent once, 1–128 characters of ASCII letters, digits and `- _ . :`; otherwise the trace id is used.
- Read-mes of both blocks and `docs/public-names.md` updated (121 reference lines changed or gone, 199 current lines changed or new; F4 removed 2 and added 19).

## Key Outcomes
- 15 files outside the feature's docs: 4 created, 11 modified (two blocks, three test projects, two read-mes, `docs/public-names.md`, `docs/backlog.md`). No knowledge-gateway name in `src` or `tests`.
- **Full suite:** pass — 63.8 s (run `mvtq9ojx-d5506215`, `dotnet test Blocks.slnx -warnaserror`, tree `D:/src/dotnet-building-blocks.worktrees/F4-GatewayHandover`; filtered: no; 0 failed, 0 timed out). Handover tied run `mvtq1ycd-a04fe60f`: pass.
- **Map delta:** none
- Risky steps: Step 3 (recheck: shared with Step 4), Step 4 (architect) · Unplanned: .claude/verify.json, docs/backlog.md
- Plan review (round 1, light): Codex NO-GO + Sonnet REVISE, 9 findings (1 HIGH, 6 MEDIUM, 2 LOW) folded before the build; the owner gave the go on the folded HIGH (the named reply type would have renamed reply fields).
- Done check: FAIL (Check 1) on one process slip — step 1 had no step-run record of its own. Code review: Codex primary GO (2 LOW) + Sonnet second reader APPROVED (steps 3–4 and the skills check). One fix round (light lane), no code change: the slip's step run `mvtq8d6z-9e5d4f0e` passed; the two LOWs accepted.
- Registry: nothing to promote — `Rule list: not applicable`; no business-rule registry in this repo. Anchor check: no manifest. Distill: nothing owed.
- Release (Step 6): this commit on `main` is the version knowledge-gateway takes with its sync tool; pushed per the owner's ruling once the full suite was green.

## Deviations from Plan
- Step 5: the surface dump read the Debug assemblies the step runs had built, not a Release build — a spawned developer may not run a separate build. Public declarations do not depend on the configuration; Codex confirmed the 121/199 counts from the saved dump.
- The fix round recorded step 1's test run in its own `## Fix Round 1` block, not in step 1's block (the format never reopens a finished step).
- The interview's Read point said the delegate test would need its own test project; the plan corrected it to the existing single endpoint host (plan § Decisions).

## Notes
- Fixed, not re-verified (light lane, no follow-up review): fix-round item 1 (the step-1 test record) — evidence: run `mvtq8d6z-9e5d4f0e` and the full suite.
- Under-bar items: [LOW] step 4's expected red was never seen on a run (first attempt failed to compile; both code readers confirmed the tests distinguish the old code); [LOW] Debug instead of Release surface dump. Both accepted, no carry-over row (no MEDIUM).
- The optional parameter departs from F3's "no optional parameter, so method groups compile" rule — owner-ruled, recorded in plan P2; a method-group caller would need an overload.
- Built in a separate worktree (owner's pick) to stay clear of another session's uncommitted test edits on `main`; `.claude/verify.json` copied into the worktree untracked and not committed. F3's plugin feedback already reported this carry-over gap.
- Rule dispositions: none listed (no rule list).
- Usage record: complete (`usage-record.json`).
- Lessons: recorded in lessons.md, not yet processed by the learner.
