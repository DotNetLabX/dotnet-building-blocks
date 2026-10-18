## Step 1 — Done-Check

### Check 1 — 2026-10-18 — after the build

**Model:** claude-opus-5-5

Predictions before reading `implementation.md`: (1) the skill log would sit in the main checkout, not the worktree, and need the two-home read; (2) step 1's frozen hashes or step 7's namespace-only diff would have drifted under later steps; (3) step 11's row coverage would be asserted but not mechanically checkable. Outcome: (1) held — the log is in the session root, scoped cleanly by token and session; (2) did not hold — both re-run green here; (3) held in part — the per-row sums are the developer's, the baseline-untouched gate re-run here.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Version-one consumer code that must keep compiling | Implemented | The three files' SHA-256 re-computed here equal the recorded values; the folder holds only them plus `bin`/`obj`; in `Blocks.slnx`. |
| 2 — Domain events from every aggregate (C1) | Implemented | `IHasDomainEvents` new; dispatch selects it; `DomainEventDispatchTests.cs` covers the four aggregate shapes and the synchronous save. |
| 3 — The audit "who" type on entities (C2, first half) | Implemented | Two-type interface, aggregate and three-type configuration; `TwoTypeAuditTests.cs`. Choice that the new interface extends `IEntity<TPrimaryKey>` was confirmed at Phase 1. |
| 4 — The audit "who" type on commands and the three steps (C2, second half) | Deviated | Valid reason: the pre-processor resolves `IClaimsProvider` from `HttpContext.RequestServices` instead of FastEndpoints' `HttpContext.Resolve`, because the library's resolver is a process-wide static that makes the plan's host-free test impossible, and setting it would reopen the R1-01 problem. Inside a host it is the same scope; the endpoint-host test passes. The reviewer is asked to confirm (Carry-Over Findings). Everything else as planned, `Blocks.MediatR.Tests` included. |
| 5 — The error handler: started replies, argument errors, the `Use…` extension (C3, C4, C10) | Implemented | `grep -n "ArgumentException =>"` on the handler re-run here: no hits. `ApplicationBuilderExtensions.cs` new. |
| 6 — One shape for validation errors; the split endpoint set-up (C7, C8) | Implemented | `ToFieldMap`, the two split methods, the shared assembly fixture, `Blocks.FastEndpoints.SetupTests`, the C7 fixture. C8 tests written green before the split. |
| 7 — The new block `Blocks.Grpc` (C9) | Implemented | Re-run here: `git show HEAD:… \| diff -` shows only the namespace line for both files; `grep -n "Grpc"` on the AspNetCore project file finds nothing; no `src/Blocks.AspNetCore/Grpc`; pins read `Blocks="Blocks.Grpc"`; the 12-block lists updated; `PinReportTests.cs` new. |
| 8 — Design-time factory, stored-JSON helpers, async migrate (C5, C6, C10) | Implemented | Factory hook, the options overloads and comparer, the complex-type repair, `MigrateAsync`, with their tests. |
| 9 — A duplicate value at save answers already-exists (C12) | Deviated | Valid reason, both resolved through `questions.md` Q2: (a) a test-only `System.Security.Cryptography.Pkcs` 10.0.13 pin the plan did not name — SqlClient 7.1.1's transitive 9.0.18 fails restore under `-warnaserror` on a security advisory; kept by the team lead; (b) a named, test-only licence exception for `Microsoft.Data.SqlClient.SNI.runtime`, the owner's ruling of 2026-10-18. The provider packages are referenced only by the EF test project (no `src/` project references them). End-to-end replies tested. |
| 10 — Read-mes (C11) | Implemented | `EachReadMe_SaysWhatTheBlocksDoNow` holds every cell of the plan's binding table, verbatim, with the plan's folding. |
| 11 — Public names and the surface comparison (Rule 2) | Implemented | `## Blocks.Grpc` section and the new counts (120 / 181) in `docs/public-names.md`; `git diff --quiet` on `surface-reference.txt` re-run here: exit 0. The per-row line sums are the developer's record, not re-derived here. |
| 12 — The plugin note | N/A (close-owned) | `Owner: close`; no `docs/plugin-feedback/` file yet, as expected. |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (the spec carries `Rule list: not applicable`; no step cites rules)

Skill log: `.claude/audit/skill-invocations.log` in the session root (the worktree holds none), scoped to `agent: developer`, token `developer:implement`, session `d5dce87e-…`. Every non-`None` mapped skill and `tdd` for every `TDD: yes` step (2–9) is logged in step order; steps 1, 10, 11 map `None`. The self-report in `## Skills Used` matches the log.
Test entry: three `test-entry` lines for this session in `violations.log`, all `blocked: true` — corrected attempts, no slip.
Plan hygiene: `## Decisions` present with rows. `## Decisions and why` is absent, exempt — the plan (2026-10-13) predates that section (nexus 2.4.19, 2026-10-15).
Notes, not findings: `questions.md` Q2's header still reads `**Status:** Open` while its Answer block reads Answered; the plan's step 9 label "Test-only packages (owner, 2026-10-13)" over-attributes SqlClient to the owner (recorded in the communication log; an architect lesson).

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-18*
