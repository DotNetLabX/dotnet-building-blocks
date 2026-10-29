# F4-GatewayHandover — done check

## Step 1 — Done-Check

### Check 1 — 2026-10-29 — after the build

**Model:** claude-opus-5-5

Lane: light. Session `880a56cb-4ca8-451c-a3c1-ea492db418fb`, window from 2026-10-29T15:41:24Z, tree `D:\src\dotnet-building-blocks.worktrees\F4-GatewayHandover`. Logs read in both homes (main checkout `.claude/audit/` and the tree's `.claude/audit/`). Step windows from `handoff.jsonl` handbacks: step 1 15:41:24–15:44:21, step 2 –15:46:02, step 3 –15:47:45 (developer `a320bf…`), step 4 –15:49:58, step 5 –15:52:16 (developer `a9e171…`).

Pre-commitment predictions: (1) step 5's Release-build procedure not followed — found, but graded on outcome (light lane), not a fail; (2) a missing step-run record where the worktree lacked the runner's settings — found, step 1; (3) step 2 not red-first — expected by the plan, not a slip.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — `UseCustomFastEndpoints` takes the caller's settings, reads names case-insensitively, declares its 400 reply | Implemented | Signature `Action<Config>? configure = null`, delegate last (`Extensions.cs:12,20`); `PropertyNameCaseInsensitive = true` (`:17`); `ProducesMetadataType = typeof(FieldMapValidationReply)` (`:35`); record with `[property: JsonPropertyName]` on all three members. Tests (a)+(b) `TheCallersConfiguration_RunsAfterTheBlocks_AndThePrefixStaysApi`, (c) `ABodyWhoseNamesDifferInCase_Binds`, compile-only `NoArgumentCall_Compiles`, (d) `FieldMapValidationReply_DeclaresItsReplyType`, (e) `FieldMapValidationReply_KeepsItsOwnNames_UnderAnyNamingPolicy` present. Skills logged in window: error-handling, create-building-blocks-package, tdd. No step-run record in the window — see process slips |
| 2 — the 400 reply's key naming and message pinned | Implemented | `AMalformedBody_Answers400_WithTheSerializerErrorsKeyAndItsTextAsTheMessage`; step run `mvtptfcz-4165252b` (pass, 15:45:45) names `CustomSetupTests.cs`. Skill logged: error-handling |
| 3 — slow-request limits from settings, streamed reply never warned | Implemented | `RequestDiagnosticsOptions` (`[Range(1,int.MaxValue)]`, defaults 1000/3000, `init`); fourth ctor parameter `IOptions<RequestDiagnosticsOptions>`; `text/event-stream` `OrdinalIgnoreCase`. Step runs `mvtpuxdu-6583a19a` (fail, red) then `mvtpvjk8-c023fc03` (pass) name `RequestDiagnosticsMiddlewareTests.cs`. Skills logged: service-infra-conventions, create-building-blocks-package, tdd |
| 4 — client correlation id accepted only when short and plain | Implemented | `MaxCorrelationIdLength = 128`, `values.Count == 1 && IsPlain(...)` (`RequestContextMiddleware.cs:11,39,49–52`). Step runs `mvtpx3ok-5de0a756` (fail, wrong-reason compile red — recorded on the step's `Test problem:` line) then `mvtpxwtu-4b990d17` (pass) name `RequestContextMiddlewareTests.cs`. Skills logged (second developer, bare names): service-infra-conventions, create-building-blocks-package, tdd |
| 5 — public names updated from the surface dump | Implemented | F4 rows in `docs/public-names.md` (`Blocks.AspNetCore` lines 29–30, `Blocks.FastEndpoints` lines 111–112) and header counts (121 / 199). Re-run here: `git diff --quiet …/surface-reference.txt` exits 0; `git grep -niE "knowledge" -- src tests` zero hits (also with `--untracked`). The Debug-assembly dump instead of a Release build is an approach difference — graded on outcome per the light lane, not a deviation. Step run `mvtq11yu-f4a36c88` `skipped` (document-only), on its `Test problem:` line |
| 6 — Release | N/A (close-owned) | `Owner: close` |

**Process slips:**
- test slip — step 1: no acceptance record

Step 1 changed `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` and `tests/Blocks.FastEndpoints.SetupTests/SplitSetupTests.cs`, and no step-run record (`step_run: true`) naming either sits in step 1's window in either log home; the developer's own run was refused at 15:43:10 (`violations.log`, `test-entry`, `blocked: true` — the tree had no `.claude/verify.json` yet), and the step block carries no `Test problem:` line. Mitigating evidence, not a substitute under the rule: the coordinator's tied run `mvtpryb3-27bbea20` (15:44:36, pass) listed both files as changed and ran their projects, `CustomSetupTests.cs` was step-run green in step 2's window, and the handover run `mvtq1ycd-a04fe60f` passed. Clearing it needs no code: a step run of each file by name (`verify-run.js --profile fast --files {file} --slug F4-GatewayHandover`). Cause sits with the worktree carry-over (the runner's settings were copied in only after step 1), not with the developer's work.

The two `test-entry` lines in the window (15:43:10, 15:50:23) are both `blocked: true` — corrected attempts, not slips.

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no rule list; no step carries `Satisfies:`)

Plan hygiene: `## Decisions` present with 8 rows; `## Decisions and why` present (P1, P2). Skill log present in the main home; every non-`None` mapped skill, `tdd` on each `TDD: yes` step included, is in its own step's window.

**Verdict: FAIL — test slip — step 1: no acceptance record**

*Status: COMPLETE — architect, 2026-10-29*
