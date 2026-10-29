# Plan review — Codex (primary), round 1 — 2026-10-29

Persisted by the standalone architect session from the Codex job `task-mvtov8vu-orhms8` (label `F4-GatewayHandover plan-review round 1`), read-only, no `--effort` passed: the repo sets no `codex.effort`, so the light lane's lowering had no baseline and Codex ran at the user's own Codex setting.

## Verdict: NO-GO

1. **HIGH — [plan.md:21](/D:/src/dotnet-building-blocks/docs/specs/F4-GatewayHandover/delivery/plan.md:21): Named reply serialization is unresolved.** The proposed record has PascalCase properties, but `SplitSetupTests.Build` serializes without options and existing assertions require lowercase names. The bare record breaks those tests. With a null naming policy, it also changes the existing wire envelope: [FastEndpoints uses the configured serializer options](https://raw.githubusercontent.com/FastEndpoints/FastEndpoints/d3aaa0ea93df5857e474bba0eb3a3a114a84a5f9/Src/Library/Config/SerializerOptions.cs). Specify how names remain compatible and cover that policy case.

2. **MEDIUM — [plan.md:19](/D:/src/dotnet-building-blocks/docs/specs/F4-GatewayHandover/delivery/plan.md:19): The compatibility acceptance proves nothing about the no-argument call.** `Blocks.Compatibility.Tests` neither references `Blocks.FastEndpoints` nor calls `UseCustomFastEndpoints()`. Step 1 also changes `EndpointHost` to pass a delegate. Add an actual compilation probe and its required project reference to the step.

3. **MEDIUM — [plan.md:29](/D:/src/dotnet-building-blocks/docs/specs/F4-GatewayHandover/delivery/plan.md:29): The validation failure trigger is incorrect as written.** `AddAndValidateOptions` registers validation; invalid limits do not make that registration call throw. The source test throws when its `Bind` helper resolves `IOptions<RequestDiagnosticsOptions>.Value`. Name options resolution or host startup explicitly in the acceptance.

4. **MEDIUM — [plan.md:48](/D:/src/dotnet-building-blocks/docs/specs/F4-GatewayHandover/delivery/plan.md:48): Step 4 lacks an `Adds:` decision.** The hand-over requests a length cap and character check; the recorded owner rulings approve them and prohibit configuration. Neither records the specific 128-character limit or ASCII allowlist. Add an `Adds:` row explaining that policy, or record the owner ruling that supplies those exact choices.

5. **MEDIUM — [plan.md:38](/D:/src/dotnet-building-blocks/docs/specs/F4-GatewayHandover/delivery/plan.md:38): Release work is missing.** The hand-over explicitly requests "release a new version," but the plan ends with surface documentation. No step identifies the delivered version/commit or release completion evidence, and release is not explicitly deferred.

6. **LOW — [plan.md:5](/D:/src/dotnet-building-blocks/docs/specs/F4-GatewayHandover/delivery/plan.md:5): The file count is incorrect.** The step file lists contain **14 distinct files**, not 13.

Read-only review completed; no files modified or build/test artifacts generated.
