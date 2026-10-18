# F3 — The blocks' answer to knowledge-gateway's findings — Questions

## Q1: No verify roles in the worktree
**From:** developer
**To:** team-lead
**Status:** Answered
**Step:** Phase 1 analysis (affects every step close, steps 1–4 first)
**File:** `.claude/verify.json` (absent in `D:\src\dotnet-building-blocks-F3-GatewayFeedback`)

**Context:** The plan's Testing Strategy and the step-close rule assume `.claude/verify.json`, but it is an untracked file of the main checkout (`D:\src\dotnet-building-blocks\.claude\verify.json`), so the worktree has no `.claude/` folder at all. `verify-run.js` resolves the tree through the session's `.worktree-target` line to the worktree, finds no file, and records `undeclared`; `--files` refuses (no runner). The worktree also lacks the other session's `[Trait("Speed","Slow")]` lines the plan's Context describes, so the `unit` role's `Speed!=Slow` filter excludes nothing here — a step close on that role would run the slow licence test too and may be cut at 75 s (an `Unfinished:` line, not red, since `full` is declared). The plan's staging note for `PackageTests.cs` (step 9) is likewise moot in this tree.

**Question:** Should the step closes run through `verify-run.js` with the main checkout's `verify.json` copied unchanged into the worktree's `.claude/`, or through the undeclared fallback?

**Recommendation:**
Copy `verify.json` into the worktree's `.claude/` unchanged (Recommended) — Step closes and `--files` runs write records under `--slug F3-GatewayFeedback`; a close may be cut by the slow tests and gets an `Unfinished:` line.
Leave it absent — Each step closes on the narrowest command (`dotnet test {touched test projects} -warnaserror`, as the plan's Testing Strategy names), which writes no record; the close gate sees no step-close records for this feature.
**Confidence:** medium — the copy is not an edit of the file (plan Decisions keep it unedited), but whether the gear needs step-close records is the coordinator's knowledge, not mine.

### Answer
team-lead (2026-10-18): option 1 taken — the main checkout's `.claude/verify.json` is copied unchanged into the worktree's `.claude/` (untracked; never staged). Step closes run through `verify-run.js`; a cut at the step limit is "unfinished", not red.

## Q2: The SQL Server library brings a package whose licence the licence test refuses
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 9 — A duplicate value at save answers already-exists (C12)
**File:** `tests/Blocks.Hygiene.Tests/PackageTests.cs` (`EveryResolvedPackage_CarriesAnAllowedLicence`)

**Context:** Step 9 is otherwise built and green: the recogniser, both save paths, the EF and end-to-end tests (`tests/Blocks.EntityFrameworkCore.Tests/DuplicateValueTests.cs`, `--files` pass), `PostgreSQL` added to `AllowedExpressions`, and the two test-only packages at the latest stable versions (`Microsoft.Data.SqlClient` 7.1.1, `Npgsql` 10.0.3). The licence test still fails, on one package only. `Microsoft.Data.SqlClient` 7.1.1 (itself MIT) depends on `Microsoft.Data.SqlClient.SNI.runtime` `[7.1.0, 8.0.0)`, the native network library. Its nuspec carries `<license type="file">LICENSE.txt</license>`, and that file is the proprietary "MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT.DATA.SQLCLIENT.SNI LIBRARY" (redistributable inside your own application). The test reads it as `file LICENSE.txt` and refuses it. Version 6.0.2 of the SNI package has the same licence, so an older SqlClient does not help. I checked every other resolved package of the EF test project, and each carries an allowed expression. The plan's fact "Microsoft.Data.SqlClient is MIT" is true of the package but not of its dependency. The owner's 2026-10-13 ruling allowed only the PostgreSQL licence. A second, smaller fact: SqlClient 7.1.1 also brings `System.Security.Cryptography.Pkcs` 9.0.18, which fails restore under `-warnaserror` (NU1902/NU1903, advisory published 2026-10-13, fixed in 10.0.13). I pinned `System.Security.Cryptography.Pkcs` 10.0.13 in `Directory.Packages.props` (test-only, transitive pinning is on). Tell me if that pin should not stay.

**Question:** How should the licence test treat the SQL Server library's native dependency?

**Recommendation:**
Allow that one package by name (Recommended) — `PackageTests` accepts `Microsoft.Data.SqlClient.SNI.runtime` with its file licence through a named exception, because only the EF test project references it and no block ships it. The plan's acceptance holds as written; the owner rules on a licence again, as on 2026-10-13.
Stand in for SqlClient's error type — the SqlClient package is dropped. The tests declare their own `Microsoft.Data.SqlClient.SqlException` with a `Number`, which the recogniser accepts by full name. No new licence is needed, but the SQL Server cases then no longer use the library's own type, as the plan asks.
Leave the licence test red — the step closes with the one slow test failing, and the full suite at handover fails with it.
**Confidence:** medium — the exception is narrow and test-only, but licence additions were the owner's rulings, so the owner may want to rule on this one too.

### Answer
**Answered by:** user (owner), 2026-10-18, question box, verbatim: "Allow it by name (Recommended)" — after asking who introduced the library ("who introduced this test library? not me! … let's see who is using it. check knowledge-gateway repo").
**Facts put to the owner before the ruling (team lead):** the SQL Server library entered the plan in the architect's fold of critic finding R1-07 (2026-10-13); the owner's ruling (2) that day named only the PostgreSQL library, so the plan's "Test-only packages (owner, 2026-10-13)" label over-attributes SqlClient. knowledge-gateway already ships `Microsoft.Data.SqlClient` 7.0.2 (with the same SNI package) in `KnowledgeGateway.Persistence` for its Fabric connection; its EF saves go to PostgreSQL.
**Ruling:** `PackageTests` accepts `Microsoft.Data.SqlClient.SNI.runtime` with its file licence through a named, test-only exception (only `tests/Blocks.EntityFrameworkCore.Tests` references it). The `System.Security.Cryptography.Pkcs` 10.0.13 pin stays (team lead: a security-advisory fix on a test-only transitive package; not an owner question).
**Status:** Answered
