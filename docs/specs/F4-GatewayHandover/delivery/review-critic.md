## Round 1 — 2026-10-29 — plan — primary (codex)

Header note: light-lane plan review, round 1 only, readers one effort level lower. Codex is the primary; its verdict is persisted in `review-critic-codex.md` (NO-GO, 6 findings), the reader section for this round. No `--effort` was passed to Codex: the repo sets no `codex.effort`, so no baseline was known and the user's own Codex setting decided. The second reader is Sonnet at medium (its high baseline one step lower).

## Second reader — round 1 (sonnet)

# F4-GatewayHandover — Critic Review (second reader, round 1)

## Mode: Plan Review
## Verdict: REVISE

Pre-commitment predictions: a wire change from the named reply type (found, F4-01); Accept lines that cannot prove what they claim (found, F4-02 and F4-04); binary and method-group compatibility of the optional parameter (found, F4-02); the 128 and character-set values with no stated origin (found, F4-03); knowledge-gateway names leaking into the plan (none found in the plan's Accept lines; the ported files were not read).

Coverage ledger. Opened: `source-feedback.md`; `src/Blocks.FastEndpoints/Extensions.cs`; `tests/Blocks.FastEndpoints.Tests/EndpointHost.cs`, `CustomSetupTests.cs`, `CustomSetupEndpoints.cs`; `src/Blocks.AspNetCore/Extensions/ValidationFailureExtensions.cs`; `RequestDiagnosticsMiddleware.cs`; `src/Blocks.Core/Configurations/ConfigurationExtensions.cs`; `src/Blocks.Grpc/GrpcServicesOptions.cs`; `src/Blocks.AspNetCore/README.md`; `tests/Blocks.AspNetCore.Tests` (`.csproj`, `HttpContextProviderTests.cs`); FastEndpoints 8.3.0 `FastEndpoints.xml`; `tests/Blocks.FastEndpoints.SetupTests/SplitSetupTests.cs`; `RequestContextMiddleware.cs`; `Directory.Build.props`; `tests/Blocks.Compatibility.Tests` (`.csproj`, grep). Searched only: `docs/public-names.md`, `surface-reference.txt`. NOT OPENED: `tools/Blocks.SurfaceDump`, the body of `docs/public-names.md`, the rest of `src/Blocks.AspNetCore`, the knowledge-gateway copy, `docs/critic-calibration/ruler.md`.

### [HIGH] F4-01 — The named record can change the 400 envelope's key names for a host whose naming policy is not camelCase, and it breaks three existing tests
Anchor: unranked — no ruler yet. Band: MEDIUM–HIGH. Untouchable: silent-wrong-output. Today the builder returns an anonymous object with hard-coded lowercase names. A record's PascalCase members are written under the serializer's `PropertyNamingPolicy`. A policy the new delegate sets (for example `null`) writes `StatusCode`. `SplitSetupTests.Build` serializes with a bare `JsonSerializer.SerializeToElement`, so its `PropertyNames(reply).Should().Equal("statusCode", …)` (line 50) would see `StatusCode`. Fix: `[JsonPropertyName]` on the members, an Accept for a null naming policy, and say `SplitSetupTests` keeps passing.

### [HIGH] F4-02 — Two Step 1 Accept lines cannot prove what they claim, and the optional parameter breaks F3's compatibility convention
Anchor: unranked — no ruler yet. Band: MEDIUM–HIGH. `Blocks.Compatibility.Tests` references only `Blocks.Domain`, `Blocks.Core` and `Blocks.EntityFrameworkCore`, so the "still builds" clause is vacuous. F3's plan, § C6, set "the existing signatures stay as they are (no optional parameter, so method groups compile)". An optional parameter replaces the one-parameter method: a compiled caller gets a missing-method error and a method-group use stops compiling. The plan does not record the trade-off. Fix: an overload, or a Decisions row recording the departure; either way a real compile probe, or drop the clause.

### [MEDIUM] F4-03 — The owner's rulings and the origin of the 128 and character-set values are not recorded
Anchor: unranked — no ruler yet. `## Decisions and why` holds only P1. The hand-over asks only for "a length cap and a character check"; 128, `-_.:` and ASCII-only letters have no stated source. Fix: record each ruling, and mark who chose the values.

### [MEDIUM] F4-04 — Step 4's header-echo assertions cannot be proven on `DefaultHttpContext`
Anchor: unranked — no ruler yet. The header is written in `Response.OnStarting`, which does not fire on a bare `DefaultHttpContext`, the context Where to Look points at. `Activity.Current` can be non-null under the test runner, so the fallback value is unstable. A repeated header joins with a comma. Fix: run the tests through `TestServer` or invoke the callback, control `Activity.Current`, add a multi-valued-header case.

### [MEDIUM] F4-05 — Step 1's delegate test depends on shared process-wide state the plan does not manage
Anchor: unranked — no ruler yet. A global `Endpoints.Configurator` applies to every endpoint in the test process; "harmless" is asserted, not checked; where the delegate records what it saw is unstated. Fix: name the capture field on `EndpointHost`, and name the tests to re-run.

### [LOW] F4-06 — Step 3's validation Accept needs a real trigger
Anchor: unranked — no ruler yet. `AddAndValidateOptions` validates on start or on `.Value`, not during the call; a missing section throws `InvalidOperationException` instead. Fix: state that the test binds a present section and resolves `IOptions<…>.Value`.

### [LOW] F4-07 — The hand-over's "release a new version" has no plan step or out-of-scope note
Anchor: unranked — no ruler yet. Fix: add the step, or say the release happens elsewhere.

### [LOW] F4-08 — Step 5's diff baseline wording does not match its procedure
Anchor: unranked — no ruler yet. Fix: "the lines no existing row covers (F1 or F3)".

Gap analysis: the streamed-reply match should say starts-with; real-time delays in the slow-request tests could be flaky; the ported tests were not read for knowledge-gateway identifiers, so the leak check on ported content is open.

### Merge — round 1
Coverage: `tools/Blocks.SurfaceDump`, the body of `docs/public-names.md` (Codex's ledger is not itemised; Sonnet left them unopened, and Codex's findings cite neither)

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Named reply record changes key names under a non-camel policy and breaks SplitSetupTests | wire contract | 400 envelope reads `StatusCode` for some hosts; red tests | quoted | silent-wrong-output | wire | plan Step 1; `SplitSetupTests.cs`; `Extensions.cs` | codex-1, F4-01 |
| R1-02 | codex, sonnet | contested: MEDIUM / HIGH → MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Compatibility Accept is vacuous; optional parameter departs from F3's convention | unprovable acceptance | the no-argument call is unproven | path | — | compatibility | plan Step 1; `Blocks.Compatibility.Tests.csproj`; F3 plan C6 | codex-2, F4-02 |
| R1-03 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | 128 and the character set have no recorded source; owner rulings not logged | decision log | a reader cannot tell owner choices from plan choices | quoted | — | decisions | plan Step 4, § Decisions and why | codex-4, F4-03 |
| R1-04 | sonnet | MEDIUM | unranked — no ruler yet | Header echo is unprovable on a bare `DefaultHttpContext`; trace-id fallback unstable | unprovable acceptance | risky step's tests pass without checking | path | — | test set-up | plan Step 4 | F4-04 |
| R1-05 | sonnet | MEDIUM | unranked — no ruler yet | Global configurator in the shared host; capture point unstated | shared state | flaky or surprising endpoint tests | path | — | test set-up | plan Step 1; `EndpointHost.cs` | F4-05 |
| R1-06 | codex, sonnet | contested: MEDIUM / LOW → MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Validation Accept names the wrong trigger | acceptance ambiguity | test may validate nothing | path | — | acceptance | plan Step 3 | codex-3, F4-06 |
| R1-07 | codex, sonnet | contested: MEDIUM / LOW → MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Release has no step | dropped instruction | no release evidence | quoted | — | scope | plan steps | codex-5, F4-07 |
| R1-08 | sonnet | LOW | unranked — no ruler yet | Step 5 wording vs procedure | wording | developer hunts covered rows | quoted | — | wording | plan Step 5 | F4-08 |
| R1-09 | codex | LOW | unranked — no anchor fits; Codex severity LOW | File count 13 vs 14 | count | header off by one | quoted | — | size | plan header | codex-6 |

Contested settlements: R1-02 MEDIUM — the owner ruled the optional delegate after seeing the overload option; no method-group or compiled consumer exists (grep: one caller, `EndpointHost.cs`), so the risk is a recorded trade-off, not a defect. R1-06 MEDIUM — a validation test that validates nothing is a real gap on a step's core Accept. R1-07 MEDIUM — the owner asked for the release, so it needs a step.

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-29)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | plan Step 1 (binding surface: `[JsonPropertyName]` on the three members; Accept (d) null-policy case; `SplitSetupTests` named) |
| R1-02 | fixed-real | MEDIUM | — | plan Step 1 (Accept: compile-only no-argument call in `CustomSetupTests.cs`, Compatibility clause dropped); § Decisions row (departure from F3's no-optional-parameter rule, owner-ruled) |
| R1-03 | fixed-real | MEDIUM | — | plan header (rulings line); § Decisions row (ASCII-only); § Decisions and why P2 |
| R1-04 | fixed-real | MEDIUM | — | plan Step 4 (TestServer, `Activity.Current` cleared, multi-valued header case); § Where to Look Step 4 |
| R1-05 | fixed-real | MEDIUM | — | plan Step 1 (capture field on `EndpointHost`; header-only pre-processor; whole project re-run) |
| R1-06 | fixed-real | MEDIUM | — | plan Step 3 Accept (resolve `IOptions<…>.Value` with the section present) |
| R1-07 | fixed-real | MEDIUM | — | plan Step 6 (Owner: close — commit, complete run, push) |
| R1-08 | fixed-real | LOW | — | plan Step 5 Accept |
| R1-09 | churn | LOW | LOW · unranked · the size rule counts no file under `docs/`; 13 is correct without `docs/public-names.md`, 14 with it | plan header (unchanged) |

Reconciled: plan Step 1 ↔ § Where to Look Step 1 ↔ § Decisions rows 1–2; Step 3 ↔ Where to Look Step 3; Step 4 ↔ Where to Look Step 4 ↔ § Decisions row (ASCII) ↔ P2; Step 6 ↔ the header's Size line (close-owned, not counted) ↔ Goal; Step 5 ↔ Where to Look Step 5.
