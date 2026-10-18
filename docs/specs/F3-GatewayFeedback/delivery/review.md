# F3-GatewayFeedback — Review

## Step 2 — Code Review

**Verdict: APPROVED**

Primary: Codex — GO, persisted by team-lead session d5dce87e-8321-43c1-963c-6371a56217f9
**Model:** codex
Reader pair: codex + sonnet (picked 2026-10-11); second reader's verdict in `review-second.md`. Full Codex output: `review-codex.md`.
Run record: complete run `dotnet test Blocks.slnx -warnaserror` — pass, 98.4 s, 2026-10-18T13:32:53Z.

No findings.

No actionable implementation defects found. Reviewed the diff against `c4211cc`, including untracked files listed by `git status --short`.

All five carry-over rows were assessed:

| Carry-over row | Assessment |
|---|---|
| Licence test runs during step closes | **Confirmed.** The unit command includes it, and it lacks `Speed=Slow`. Operational concern; no F3 fix required. |
| Pre-processor uses `RequestServices` | **Confirmed**, with an evidence qualification: the publisher test does not exercise claims resolution. Direct pre-processor tests cover assignment; no defect found. |
| Test-only Pkcs 10.0.13 pin | **Confirmed.** Q2 authorizes retaining it; no production block resolves it. Keep until the dependency supplies an acceptable version. |
| Two licence-exception narrowings lack mutants | **Confirmed coverage limitation.** Both guards remain intact. Optional improvement: focused negative tests for production-block resolution and non-file licences. |
| Mutation batteries are author-produced | **Confirmed as disclosed provenance.** No independent mutation execution was performed during this read-only review. |

The supplied full-suite pass was verified against all **249 audit hash entries**, with no differences. The public surface comparison reproduces the documented counts, and the compatibility fixture hashes match their frozen values. The suite was not rerun; no files were modified.

*Status: COMPLETE — Codex primary, persisted by team-lead, 2026-10-18*

## Step 2 — Fix list, cycle 1
Bar: all

1. [MEDIUM] The Domain read-me lost its "Value objects" bullet though `ValueObject`, `StringValueObject` and `SingleValueObject<T>` still ship; restore it — `src/Blocks.Domain/README.md:10-13` (second reader)
2. [MEDIUM] Three plan instructions have no test that fails without them: the two-type `CreatedOn` default, the three-type configuration's concurrency (`RowVersion`) switch, and the design-time factory's constructor order; add three small tests — `src/Blocks.Domain/Entities/AggregateRoot.cs:48`, `src/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs:43,54`, `src/Blocks.EntityFrameworkCore/DbContextDesignTimeFactory.cs:31-34` (second reader)
3. [LOW] A blank line detaches the three new AspNetCore rows from their table — `docs/public-names.md:24-25` (second reader)
4. [LOW] The MediatR read-me splits its bullet list around a new paragraph — `src/Blocks.MediatR/README.md:12-15` (second reader)

Follow-up review: none due
