# F4-GatewayHandover — Implementation

**Model:** claude-opus-5-5

## Step 1 — `UseCustomFastEndpoints` takes the caller's settings, reads names case-insensitively, and declares its 400 reply
**Files:** `src/Blocks.FastEndpoints/Extensions.cs` — `UseCustomFastEndpoints(this IApplicationBuilder app, Action<Config>? configure = null)`: sets `PropertyNameCaseInsensitive = true` after the `api` prefix and runs the delegate last, after `UseEnumsAsText()` and `UseFieldMapValidationReply()`; `UseFieldMapValidationReply()` now returns a `FieldMapValidationReply` and sets `config.Errors.ProducesMetadataType = typeof(FieldMapValidationReply)`
`src/Blocks.FastEndpoints/FieldMapValidationReply.cs` — new public sealed record `FieldMapValidationReply(int StatusCode, string Message, Dictionary<string, string[]> Errors)`, each member carrying `[property: JsonPropertyName]` (`statusCode`, `message`, `errors`) so the envelope never follows a naming policy
`src/Blocks.FastEndpoints/README.md` — the delegate and its order, case-insensitive reading, the named reply type, why it is declared to the API document, and a registration line with a delegate
`tests/Blocks.FastEndpoints.Tests/EndpointHost.cs` — passes a delegate that records whether the enum converter was already present (`CallersConfigurationSawTheBlocksEnumSetting`) and sets `c.Endpoints.Configurator` to add `ConfiguredHeader` as a global pre-processor
`tests/Blocks.FastEndpoints.Tests/CustomSetupEndpoints.cs` — `ConfiguredHeader`, a global pre-processor that only writes the `X-Configured: yes` response header
`tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` — (a)+(b) `TheCallersConfiguration_RunsAfterTheBlocks_AndThePrefixStaysApi`, (c) `ABodyWhoseNamesDifferInCase_Binds`, the compile-only `NoArgumentCall_Compiles` (internal, never called), and a raw-text `PostRawAsync` helper
`tests/Blocks.FastEndpoints.SetupTests/SplitSetupTests.cs` — (d) `FieldMapValidationReply_DeclaresItsReplyType` and (e) `FieldMapValidationReply_KeepsItsOwnNames_UnderAnyNamingPolicy`, both saving and restoring the response builder and `ProducesMetadataType`; a `ProducesMetadataType` reader through the existing reflection helper
**Skills:** error-handling, create-building-blocks-package, tdd
**Decisions:** (a) uses `/api/sign-up` with a valid body (204) and `/sign-up` (404), as the plan names. `ProducesMetadataType` is read through the test file's existing reflection helper rather than the property getter, because its getter's visibility could not be confirmed without a build (the step run was refused, below); the helper's comment now says the getters are not all public. Test (c) is not red-first by nature: the library's default serializer options already read names case-insensitively (the plan's Decisions row 5 says the setting restates that default), so it pins the behaviour rather than drives it.
**Deviations:** None
**Verified:** the developer's step run was refused (the worktree had no `.claude/verify.json`); the coordinator's tied run `mvtpryb3-27bbea20` over step 1's five files built every project and passed every test, the new ones included

## Step 2 — The 400 reply's key naming and message are pinned (hand-over point 2)
**Files:** `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` — `AMalformedBody_Answers400_WithTheSerializerErrorsKeyAndItsTextAsTheMessage`: posts `{not json` as raw `application/json` text through step 1's `PostRawAsync`, asserts 400, `statusCode` 400, `errors` holding exactly `serializerErrors` with one non-blank parser text, and that text as `message`
**Skills:** error-handling
**Decisions:** None — the hand-over's other two point-2 tests stay covered by `OneFailure_AnswersItsOwnMessage` and `FailuresOnFlatFields_AnswerAPerFieldMap`, as the plan says
**Deviations:** None — pins F3's existing behaviour, not test-first (plan); green on attempt 1, run `mvtptfcz-4165252b`

## Step 3 — Slow-request limits come from settings, and a streamed reply is never warned
**Files:** `src/Blocks.AspNetCore/RequestDiagnosticsOptions.cs` — new public settings class in `Blocks.AspNetCore`: `SlowRequestMs` (default 1000) and `SlowFileTransferMs` (default 3000), each `init` and `[Range(1, int.MaxValue)]`
`src/Blocks.AspNetCore/Middlewares/RequestDiagnosticsMiddleware.cs` — takes `IOptions<RequestDiagnosticsOptions>` as a fourth constructor parameter and reads the two limits from it instead of the 1000/3000 literals; a reply whose content type starts with `text/event-stream` (case-insensitive) is never warned
`src/Blocks.AspNetCore/README.md` — the settings type and defaults, the optional binding line, the streamed-reply exemption, the middleware order (`RequestContextMiddleware` first) and the diagnostics middleware's registration line
`tests/Blocks.AspNetCore.Tests/RequestDiagnosticsMiddlewareTests.cs` — new: the streamed-reply exemption beside a warned `application/json` reply (theory over `text/event-stream; charset=utf-8` and `Text/Event-Stream`), each limit from the bound section (an under-limit arm and an over-limit arm each), the defaults, and 0 / -1 on each member failing `AddAndValidateOptions` + `IOptions<>.Value` with an `OptionsValidationException` naming the member; a private collecting logger and web-host environment in the file (the project had neither)
**Skills:** service-infra-conventions, create-building-blocks-package, tdd
**Decisions:** the stream test is a theory that adds a mixed-case content type, pinning the case-insensitive match the plan names; each limit test pairs a not-warned arm with a warned one so neither passes vacuously
**Deviations:** None — red on attempt 1 (`mvtpuxdu-6583a19a`, the test file did not compile without the settings type and the fourth parameter), green on attempt 2 (`mvtpvjk8-c023fc03`, 9 tests)

## Step 4 — A client's correlation id is accepted only when short and plain
**Files:** `src/Blocks.AspNetCore/Middlewares/RequestContextMiddleware.cs` — `ResolveCorrelationId` keeps the client's `X-Correlation-ID` only when the header holds exactly one value of 1–128 characters, each an ASCII letter or digit or one of `- _ . :`; otherwise the existing fallback (trace id, else `TraceIdentifier`). The limit and the character test are private (`MaxCorrelationIdLength`, `IsPlain`, `IsAllowed`)
`src/Blocks.AspNetCore/README.md` — one bullet on the rule and the fallback
`tests/Blocks.AspNetCore.Tests/RequestContextMiddlewareTests.cs` — new, `TestServer` host (middleware + a terminal handler writing `RequestContext.CorrelationId` into the body): a GUID and `order_42.retry:1` kept; exactly 128 characters kept; 129 characters, a space, `<`, `é` replaced; a header sent twice replaced; absent, empty and blank fall back — every replaced or fallback case asserts header equals context and matches `^[A-Za-z0-9_.:-]{1,128}$`, never the fallback's value
**Skills:** service-infra-conventions, create-building-blocks-package, tdd
**Decisions:** a header carrying two values is rejected as a whole (`values.Count == 1`) rather than taking the first; the twice-sent test also rules out both joined forms (`first,second`, `first, second`) so it holds whether the test client sends two values or one joined line. An empty or whitespace value now fails the character test, so the old `IsNullOrWhiteSpace` check folds into it
**Deviations:** None
**Test problem:** `RequestContextMiddlewareTests.cs` build — attempt 1 (`mvtpx3ok-5de0a756`) red for the wrong reason: the test used `NotBeOneOf`, which AwesomeAssertions' string assertions lack (CS1061), so the right-reason red (129 characters, `<`, space, `é`, two values all kept by the old code) was never observed on a run — fixed (a `NotContain` over the rejected forms) together with the implementation, green on attempt 2 (`mvtpxwtu-4b990d17`, 11 tests)

## Step 5 — Public names updated from the surface dump
**Files:** `docs/public-names.md` — header: F4 named beside F3, the new counts (121 reference lines changed or gone, 199 current lines changed or new; F4 removed 2 lines and added 19) and what an F4 row names; `## Blocks.AspNetCore`: rows for `RequestDiagnosticsOptions` (4 lines) and the diagnostics middleware's changed constructor (1 line), and the note under the table now says the diagnostics middleware's current constructor line is the F4 row's; `## Blocks.FastEndpoints`: rows for the optional-parameter `UseCustomFastEndpoints` (1 + 1 lines) and `FieldMapValidationReply` with its compiler-given record members (13 lines)
**Skills:** None — the plan's `None` (no skill covers the surface dump)
**Decisions:** the F4 delta was checked by arithmetic against F3's recorded counts, not by a second dump of HEAD: F3's 120 / 181 plus F4's changes give exactly 121 / 199 — the reference's old `UseCustomFastEndpoints` line now differs (+1 reference side), and on the current side F4 adds 19 lines (4 + 1 + 1 + 13) and removes version one's 3-parameter diagnostics constructor (−1). `UseFieldMapValidationReply`'s line did not change (it still returns `void`), so F3's row still covers it. Output and sorted sets in `D:\temp\f4-surface` (`surface-f4.txt`, 762 lines), on D: like F3's
**Deviations:** the plan says to build `Blocks.slnx` in Release first; the test-entry gate refused that build (a developer runs no separate build). The dump read the Debug assemblies the step runs and the coordinator's tied run had already built (every block's `bin/Debug/net10.0`, all newer than their last source edit), with the four test projects' output folders as probe folders, and ran the dump tool already built in `D:\src\dotnet-building-blocks\tools\Blocks.SurfaceDump\bin\Debug` (tool source unchanged in this tree). The public surface does not depend on the build configuration. Gates: `git diff --quiet docs/specs/F1-VersionOne/delivery/surface-reference.txt` exits 0; `git grep -niE "knowledge" -- src tests` gives zero hits
**Test problem:** step run `mvtq11yu-f4a36c88` — `skipped`, nothing to run (the step changed a document only); the complete run covers the build

## Fix Round 1
**Files:** `docs/specs/F4-GatewayHandover/delivery/implementation.md` — this block only; no source or test file changed
**Skills:** None — no item needed a code change
**Decisions:** Item 1 (test slip, step 1 had no acceptance record): ran the step test rule on `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` and `tests/Blocks.FastEndpoints.SetupTests/SplitSetupTests.cs` by name. Attempt 1 is run `mvtq8d6z-9e5d4f0e` (unit `fix-c1`). It passed: 14 tests, the step 1 tests (a)–(e) among them, nothing timed out and nothing over the speed-up line, so no attempt 2. Both test projects were built. The run was labelled with `label-tests.js --run mvtq8d6z-9e5d4f0e`. This is step 1's acceptance record. Item 2 (Step 4's red never seen on a run): accepted as documented, no code change. The second reader also refuted it as a risk: the old code kept every rejected input. Item 3 (the surface dump read Debug assemblies, not Release): the substitution is accepted, no code change
**Deviations:** item 1 asked for the run id in step 1's block. It is recorded here instead, because a closed step block is never reopened (implementation-format, § A fix round has no step of its own). This block carries step 1's acceptance record

## Key Decisions
- Step 1: (a) uses `/api/sign-up` (204) and `/sign-up` (404); `ProducesMetadataType` read through the test file's reflection helper; test (c) pins the library's default case-insensitive reading rather than driving it
- Step 3: the stream test is a theory adding a mixed-case content type; each limit test pairs a not-warned arm with a warned one
- Step 4: a header with two values is rejected as a whole (`values.Count == 1`), accepted by the coordinator as matching the plan's Accept; the twice-sent test also rules out both joined forms; blank values fall to the character test
- Step 5: the F4 delta verified arithmetically against F3's recorded counts (121 / 199)

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | error-handling, create-building-blocks-package, tdd | |
| 2 | error-handling | not test-first by the plan (pins existing behaviour) |
| 3 | service-infra-conventions, create-building-blocks-package, tdd | |
| 4 | service-infra-conventions, create-building-blocks-package, tdd | |
| 5 | None | plan: `None` — no skill covers the surface dump |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Step 4's right-reason red never observed on a run | low | reviewer | attempt 1 `mvtpx3ok-5de0a756` failed to compile (CS1061); attempt 2 `mvtpxwtu-4b990d17` green with the fix | by reading, the old code kept every rejected id, so each rejected-case test asserts `header != sent` against behaviour the change removed |
| Surface dump read Debug, not Release, assemblies | low | reviewer | step 5 Deviations | the test-entry gate refuses a developer's separate build; the close may re-dump from a Release build if it wants the plan's exact procedure |

## Unfinished Tests
- Step 4: Test problem: `RequestContextMiddlewareTests.cs` build — attempt 1 (`mvtpx3ok-5de0a756`) red for the wrong reason (CS1061, `NotBeOneOf`) — fixed together with the implementation, green on attempt 2 (`mvtpxwtu-4b990d17`, 11 tests)
- Step 5: Test problem: step run `mvtq11yu-f4a36c88` — `skipped`, nothing to run (document-only step); the complete run covers the build

## Deviations from Plan
- Step 5: the surface dump read the step runs' Debug assemblies through the already-built dump tool, because the test-entry gate refuses a developer's separate Release build

*Status: COMPLETE — developer, 2026-10-29*
