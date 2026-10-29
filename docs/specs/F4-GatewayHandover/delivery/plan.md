# F4-GatewayHandover

**Feature Spec:** None — technical feature; definition = backlog row `F4-GatewayHandover` + `docs/specs/F4-GatewayHandover/definition/source-feedback.md` (copy of knowledge-gateway's hand-over of 2026-10-29) + the owner's rulings of 2026-10-29 below
**Model:** claude-opus-5-5
**Size:** 5 developer-built steps · 13 files · fullChecks: none — no fullChecks list declared
**Lane:** light — owner's level
**Build lane:** fastest — lighter checks — recommended: fastest — lighter checks
**Skills per step:** on
**Rule list:** not applicable — no rule-shaped spec; the behaviour is pinned by the named tests
**Owner rulings (2026-10-29, each the recommended option):** point 1 — the optional delegate on `UseCustomFastEndpoints` (over a second method or no change); point 3 — the settings class with defaults, unbound by the block, and no warning on streamed replies; point 4 first — declare a named 400 reply type to the API document; point 4 second — accept a client's correlation id only up to 128 characters of letters, digits and `- _ . :`, else use the trace id, with no setting for the limit; release — push the commit once every test passes; the Read points (point 2 tests only, test placement, knowledge-gateway-only files left out, the other session's dirt untouched) — "Nothing, go on".
**Related:** `docs/specs/F3-GatewayFeedback` (Done 2026-10-18) — already split `UseCustomFastEndpoints` into `UseEnumsAsText()` and `UseFieldMapValidationReply()` and already sends every error key through the serializer's naming (`ValidationFailureExtensions.ToFieldMap`), which is the hand-over's point 2. Knowledge-gateway's copy predates F3 (taken at `9a55da0`), so its diff mixes F3's changes with the hand-over's; only the hand-over's points are ported, onto F3's shape.

## Goal
Answer knowledge-gateway's hand-over in the blocks: an optional settings delegate on `UseCustomFastEndpoints` (point 1), the 400 key naming pinned by tests (point 2, code already here), settings for the slow-request limits and no slow warning on streamed replies (point 3), the 400 reply declared to the API document and a bound on the client's correlation id (point 4), then release the commit. Everything generic: no knowledge-gateway names in code, tests or read-mes.
Out of scope: knowledge-gateway's own files in its copy (`TimedActionExtensions.cs`, the `Grpc` folder), its sync of the new commit, and the other session's uncommitted test edits on `main`.

## Implementation Steps

### Step 1 — `UseCustomFastEndpoints` takes the caller's settings, reads names case-insensitively, and declares its 400 reply
Accept: `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` asserts (a) a global pre-processor the `EndpointHost` delegate sets is in force — `/api/sign-up` (or another existing endpoint) answers with the response header it writes, `/sign-up` answers 404; (b) the delegate saw the block's enum converter already in the serializer options (a flag `EndpointHost` exposes, set inside the delegate); (c) a body whose property names differ in case (`{"NAME":…,"EMAIL":…}`) binds; and it holds one compile-only member (never run) calling `app.UseCustomFastEndpoints()` with no argument; `tests/Blocks.FastEndpoints.SetupTests/SplitSetupTests.cs` asserts (d) `UseFieldMapValidationReply()` sets `config.Errors.ProducesMetadataType` to `FieldMapValidationReply` (restored after, as its other tests restore what they set) and (e) the built reply, serialized with `PropertyNamingPolicy = null` and with camelCase, both read `statusCode`, `message`, `errors` in that order; every existing test in both projects passes unchanged, `FailuresOnFlatFields_AnswerAPerFieldMap` included · Files: `src/Blocks.FastEndpoints/Extensions.cs`, `src/Blocks.FastEndpoints/FieldMapValidationReply.cs`, `src/Blocks.FastEndpoints/README.md`, `tests/Blocks.FastEndpoints.Tests/EndpointHost.cs`, `tests/Blocks.FastEndpoints.Tests/CustomSetupEndpoints.cs`, `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs`, `tests/Blocks.FastEndpoints.SetupTests/SplitSetupTests.cs` · Follow error-handling, create-building-blocks-package · TDD: yes · Effort points: 5

Binding public surface: `public static IApplicationBuilder UseCustomFastEndpoints(this IApplicationBuilder app, Action<Config>? configure = null)` — the delegate runs last, after the `api` prefix, `PropertyNameCaseInsensitive = true`, `UseEnumsAsText()` and `UseFieldMapValidationReply()`; `public sealed record FieldMapValidationReply(int StatusCode, string Message, Dictionary<string, string[]> Errors)` in namespace `Blocks.FastEndpoints`, each member carrying `[JsonPropertyName]` (`statusCode`, `message`, `errors`) so the envelope's own names never follow a naming policy, exactly as today's anonymous object; the response builder returns it, and `UseFieldMapValidationReply()` sets `config.Errors.ProducesMetadataType = typeof(FieldMapValidationReply)`. FastEndpoints 8.3.0 has `Errors.ProducesMetadataType` (checked in the installed package's XML docs at plan time). The test pre-processor only writes a response header, so the other endpoint tests in the process see nothing else; the whole `Blocks.FastEndpoints.Tests` project is re-run. Read-me: the delegate and its order, case-insensitive reading, the named reply type and why it is declared.

### Step 2 — The 400 reply's key naming and message are pinned (hand-over point 2)
Accept: `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` asserts a malformed body (`{not json`, posted as raw `application/json` text) answers 400 with `errors` holding exactly the key `serializerErrors`, a non-empty parser text, and that text as `message` (the F3 code already passes this — the test pins it, it is not test-first) · Files: `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` · Follow error-handling · Effort points: 1

The hand-over's other two point-2 tests (one failure: its text is the message, key camelCase; two failures: the multiple-fields message) are already covered by `OneFailure_AnswersItsOwnMessage` and `FailuresOnFlatFields_AnswerAPerFieldMap` — not duplicated.

### Step 3 — Slow-request limits come from settings, and a streamed reply is never warned
Accept: `tests/Blocks.AspNetCore.Tests/RequestDiagnosticsMiddlewareTests.cs` (new) asserts: a reply whose content type starts with `text/event-stream` (case-insensitive; the test uses `text/event-stream; charset=utf-8`) slower than the limit logs no warning while an `application/json` one logs exactly one; the ordinary limit and the file-transfer limit each come from the bound `RequestDiagnosticsOptions` section; the defaults are 1000 and 3000; with the `RequestDiagnosticsOptions` section present and a member set to 0 or -1, `AddAndValidateOptions` followed by resolving `IOptions<RequestDiagnosticsOptions>.Value` throws `OptionsValidationException` whose message names that member · Files: `src/Blocks.AspNetCore/RequestDiagnosticsOptions.cs`, `src/Blocks.AspNetCore/Middlewares/RequestDiagnosticsMiddleware.cs`, `src/Blocks.AspNetCore/README.md`, `tests/Blocks.AspNetCore.Tests/RequestDiagnosticsMiddlewareTests.cs` · Follow service-infra-conventions, create-building-blocks-package · TDD: yes · Effort points: 3

Binding public surface: `public class RequestDiagnosticsOptions` in namespace `Blocks.AspNetCore`, `[Range(1, int.MaxValue)] public int SlowRequestMs { get; init; } = 1000;` and the same for `SlowFileTransferMs` (= 3000); the middleware takes `IOptions<RequestDiagnosticsOptions>` as a fourth constructor parameter. The block does not bind the settings: unbound, the defaults apply; a host binds with `AddAndValidateOptions<RequestDiagnosticsOptions>` (section `RequestDiagnosticsOptions`, required once bound). Read-me: the settings type, the binding line, the streamed-reply exemption, the middleware order (`RequestContextMiddleware` before `RequestDiagnosticsMiddleware`) and the registration line for the diagnostics middleware, which the read-me's registration block does not show today. Test helpers (a collecting logger, a fake web-host environment) live in the test file or beside it; reuse any the project already has.

### Step 4 — A client's correlation id is accepted only when short and plain
Accept: `tests/Blocks.AspNetCore.Tests/RequestContextMiddlewareTests.cs` (new), running the middleware in a `TestServer` host (the project already references `Microsoft.AspNetCore.TestHost`) so the `OnStarting` header write fires, asserts: a GUID-shaped id and an id of exactly 128 allowed characters come back unchanged on the reply's `X-Correlation-ID` header and as `RequestContext.CorrelationId` (read by a terminal handler that writes it into the body); an id of 129 characters, an id holding a space, an id holding `<`, an id holding a non-ASCII letter (`é`), and a header sent twice (two values) each come back as an id different from the one sent, equal on the header and in `RequestContext`, and matching the allowed shape; an absent or blank header falls back as today. The test does not predict the fallback's value (trace id or `TraceIdentifier`, depending on `Activity.Current`), only that the sent value was not kept and that header and context agree · Files: `src/Blocks.AspNetCore/Middlewares/RequestContextMiddleware.cs`, `src/Blocks.AspNetCore/README.md`, `tests/Blocks.AspNetCore.Tests/RequestContextMiddlewareTests.cs` · Follow service-infra-conventions, create-building-blocks-package · TDD: yes · Risk: risky — client input that reaches every log line and the reply header; the boundary (128 kept, 129 replaced) and the character set must be exact · Effort points: 3

No new public surface: the limit and the character set (ASCII letters and digits, `-`, `_`, `.`, `:`) are private constants of the middleware. Read-me: one line on the rule and the fallback.

### Step 5 — Public names updated from the surface dump
Accept: `docs/public-names.md` has a row for every line of the new surface dump's diff against `docs/specs/F1-VersionOne/delivery/surface-reference.txt` that no existing (F1 or F3) row covers — the optional-parameter `UseCustomFastEndpoints` signature, `FieldMapValidationReply` and its members, `RequestDiagnosticsOptions` and its two members, the diagnostics middleware's changed constructor — each row naming F4 and the lines it covers; the header's line counts re-derived from the new diff; `git diff --quiet docs/specs/F1-VersionOne/delivery/surface-reference.txt` exits 0; `git grep -niE "knowledge" -- src tests` gives zero hits (run alone, `; true` appended) · Files: `docs/public-names.md` · None — no skill covers the surface dump; after steps 1–4, build `Blocks.slnx` in Release, run `tools/Blocks.SurfaceDump` on the 12 block assemblies to a file under the OS temp folder and diff it against the reference · Effort points: 2

### Step 6 — Release (Owner: close)
Accept: the close's complete test run passes; one commit on `main`, subject `feat(F4-GatewayHandover): …`, staging only this feature's files (never the other session's dirt listed in the Goal); pushed to `origin/main` once the complete run is green (owner ruling 2026-10-29); `summary.md` names the commit id as the version knowledge-gateway takes with its sync tool · Files: `docs/specs/F4-GatewayHandover/delivery/summary.md`, `docs/backlog.md` · Owner: close

## Where to Look
- Step 1: `src/Blocks.FastEndpoints/Extensions.cs` — `UseCustomFastEndpoints`, `UseFieldMapValidationReply`; knowledge-gateway's version for reference only: `D:\src\knowledge-gateway\src\BuildingBlocks\Blocks.FastEndpoints\Extensions.cs` (do not copy its `ErrorKey`, F3's `ToFieldMap` stays); tests follow `D:\src\knowledge-gateway\src\Tests\Blocks.Tests\FastEndpoints\CustomFastEndpointsTests.cs` and `CustomFastEndpointsApp.cs` (`ConfiguredHeader`, `CallersConfigurationSawTheBlocksEnumSetting`), moved onto this repo's single `EndpointHost` and its existing endpoints in `CustomSetupEndpoints.cs`; `SplitSetupTests.cs` — `Build` and the save-and-restore pattern; new file `FieldMapValidationReply.cs` follows `src/Blocks.AspNetCore/Extensions/ValidationFailureExtensions.cs` for file layout.
- Step 2: `tests/Blocks.FastEndpoints.Tests/CustomSetupTests.cs` — `SignUpAsync` (post raw text instead of JSON for the malformed body); knowledge-gateway's `AMalformedBody_Answers400_WithTheSerializerErrorsKeyInCamelCase`.
- Step 3: `src/Blocks.AspNetCore/Middlewares/RequestDiagnosticsMiddleware.cs` — the threshold in the `finally` block; new `RequestDiagnosticsOptions.cs` follows `src/Blocks.Grpc/GrpcServicesOptions.cs`; tests follow `D:\src\knowledge-gateway\src\Tests\Blocks.Tests\AspNetCore\RequestDiagnosticsMiddlewareTests.cs` and its `MiddlewareHarness.cs` (`CollectingLogger`, `TestWebHostEnvironment`); binding via `src/Blocks.Core/Configurations/ConfigurationExtensions.cs` — `AddAndValidateOptions`.
- Step 4: `src/Blocks.AspNetCore/Middlewares/RequestContextMiddleware.cs` — `ResolveCorrelationId`; the `TestServer` host follows `tests/Blocks.AspNetCore.Tests/ErrorMapperHost.cs` — no precedent for this middleware's tests.
- Step 5: `docs/public-names.md` — `## Blocks.AspNetCore` and `## Blocks.FastEndpoints` tables and the header paragraph; `tools/Blocks.SurfaceDump`; F3's plan Step 11 (`docs/specs/F3-GatewayFeedback/delivery/plan.md`) for the procedure.
- Step 6: `docs/specs/F3-GatewayFeedback/delivery/summary.md` for the summary's shape.

## Decisions

| Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|
| The delegate test runs in the existing `EndpointHost` (it passes a delegate), not in a new test project | the endpoint library is set up once per process and this host is that one set-up; the test pre-processor only writes a header (corrects the Read point given to the owner, which said a new project) | a third FastEndpoints test project | high — one host already exists for exactly this reason | decided |
| The optional parameter departs from F3's "no optional parameter, so method groups compile" rule (F3 plan § C6) | the owner picked the optional delegate over a second method on 2026-10-29; the only caller is `EndpointHost.cs` (grep), apps copy source rather than a compiled assembly, and knowledge-gateway's copy already has this signature — see plan P2 | an overload beside the one-argument method | medium — a method-group use would stop compiling; none exists today | decided |
| `ProducesMetadataType` is set inside `UseFieldMapValidationReply()`, not only in `UseCustomFastEndpoints` | a host composing the setup by hand gets a truthful API document too | set it only in `UseCustomFastEndpoints` | high — the reply type and its declaration belong together | decided |
| The record's members carry `[JsonPropertyName]` | keeps the envelope's names fixed under any naming policy, as the anonymous object did | let the naming policy name them | high — the wire must not change | decided |
| Case-insensitive reading is set in `UseCustomFastEndpoints` only, as knowledge-gateway built it | it restates the library's default, so hand-composed setups lose nothing | a third `Config` method or putting it in `UseEnumsAsText` | medium — an explicit default is documentation, not behaviour | decided |
| F3's field-key conversion stays; knowledge-gateway's `ErrorKey` is not ported | F3's also converts nested and indexed names; point 2 is already met — see plan P1 | port knowledge-gateway's version | high — the F3 tests already pin the broader behaviour | decided |
| "Letters" in the owner-ruled character set means ASCII letters only | non-ASCII letters add nothing to an id and widen what reaches logs | any Unicode letter | high — standard ids are ASCII | decided |
| The correlation-id limit and character set are private constants | the owner ruled out a setting | a public constant or settings value | high — no consumer needs to read them | decided |

## Decisions and why

### P1 — Hand-over point 2 needs tests only, no code
- **By:** architect
- **Decided:** port only knowledge-gateway's malformed-body test; keep F3's `ToFieldMap` conversion.
- **Why:** F3 (2026-10-18) already sends every key through the serializer's naming policy, read when the reply is written, including `SerializerErrors` → `serializerErrors` and nested names; knowledge-gateway's copy predates F3.
- **Turned down:** porting knowledge-gateway's `ErrorKey` — it converts only the whole name, not each part of a nested one.
- **Rests on:** `ValidationFailureExtensions.ToFieldMap` being the conversion `UseFieldMapValidationReply` uses at `5bcc26f`.
- **Confidence:** high — read in the code at plan time; step 2's test proves it.
- **Source:** discussion of 2026-10-29 (Read point 6, owner: "Nothing, go on")
- **State:** current

### P2 — The optional delegate stands against F3's no-optional-parameter rule
- **By:** architect
- **Decided:** keep the owner-ruled optional parameter and record the departure from F3's rule; prove the no-argument call compiles with a compile-only member.
- **Why:** architect's reading of the owner's pick: the owner chose "Yes, the optional delegate" over "Yes, as a second method" after being told apps copy the source; the review then found F3's rule, which guards method-group uses — none exist (one caller, `EndpointHost.cs`).
- **Turned down:** an overload — same behaviour, two methods to document, and knowledge-gateway's copy would differ from the blocks on its next sync.
- **Rests on:** no consumer using `UseCustomFastEndpoints` as a method group or from a compiled assembly.
- **Confidence:** medium — true today; a future method-group caller would need the overload.
- **Source:** plan review round 1, 2026-10-29 (R1-02)
- **State:** current

## Plan Review
Round 1 (light lane, round 1 only): Codex primary NO-GO, Sonnet second reader REVISE; 9 merged findings — 1 HIGH, 6 MEDIUM, 2 LOW — all folded (8 fixed, 1 churn). Record: `review-critic.md` § Round 1, `review-critic-codex.md`. Folded HIGH for the owner's go: R1-01 — the named reply record would have renamed `statusCode` and `message` under a non-camelCase naming policy and broken three setup tests; fixed by `[JsonPropertyName]` on its members and a null-policy test.
Reconciled: Step 1 ↔ Where to Look Step 1 ↔ Decisions rows 1, 2, 4 ↔ P2; Step 3 ↔ Where to Look Step 3; Step 4 ↔ Where to Look Step 4 ↔ Decisions row 7; Step 5 ↔ Where to Look Step 5; Step 6 ↔ Goal ↔ Size (close-owned, not counted).

## Open Questions
None
