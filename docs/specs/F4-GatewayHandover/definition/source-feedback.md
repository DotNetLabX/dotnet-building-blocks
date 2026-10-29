# dotnet-building-blocks — fixes from knowledge-gateway, 2026-10-29

**For:** the blocks repo, `D:\src\dotnet-building-blocks` (`https://github.com/DotNetLabX/dotnet-building-blocks`), and
its owner. **From:** knowledge-gateway (copies at `src/BuildingBlocks/Blocks.FastEndpoints` and
`src/BuildingBlocks/Blocks.AspNetCore`, taken per `blocks.lock.json`). Format: the `change-copied-library` hand-over.

**Status:** points 1 and 3 change a block's public surface; the knowledge-gateway owner had them built in the copy
ahead of the blocks owner's ruling (F258, 2026-10-29: "Yes, build all three now"). Point 2 is a plain defect. Point 4
holds observations only, not fixed here. Built in feature F258-HostsReferenceShape, step 1.

## Prompt to paste

```
Apply the knowledge-gateway hand-over D:\src\knowledge-gateway\docs\library-feedback\dotnet-building-blocks-2026-10-29.md
to Blocks.FastEndpoints and Blocks.AspNetCore: port points 1-3 and their tests from the knowledge-gateway copy
(src/BuildingBlocks/Blocks.FastEndpoints, src/BuildingBlocks/Blocks.AspNetCore, src/Tests/Blocks.Tests), keep them
generic (no knowledge-gateway names), build and test, release a new version. Put points 1 and 3's public surface to
the owner first, and point 4 as two separate rulings.
```

## 1. `UseCustomFastEndpoints` takes the caller's own configuration

- **Observed:** a host that needs its own endpoint-library setting — a global pre-processor that stamps the caller's
  id (`c.Endpoints.Configurator`) — had no way to add it, so it wrote `UseFastEndpoints(...)` itself and lost the
  block's route prefix, enum and validation-reply settings. The block also left case-insensitive reading to the
  library's default rather than setting it.
- **What changed:** `UseCustomFastEndpoints(this IApplicationBuilder app, Action<Config>? configure = null)` — the
  delegate runs after the block's own settings. The block now sets `Serializer.Options.PropertyNameCaseInsensitive =
  true` explicitly beside the enum converter.
- **Files:** `Blocks.FastEndpoints/Extensions.cs`, `Blocks.FastEndpoints/README.md` (the delegate, case-insensitive
  reading; written at F258 step 5).
- **Test:** `Blocks.Tests/FastEndpoints/CustomFastEndpointsTests.cs` —
  `TheCallersConfiguration_RunsAfterTheBlocks_AndThePrefixStaysApi` (a configurator the delegate sets is in force;
  `/api/rate` answers, `/rate` does not), `ABodyWhoseNamesDifferInCase_Binds`, `AReply_WritesEnumsAsText`; the test
  app is `CustomFastEndpointsApp.cs` beside it.

## 2. The validation reply's `errors` keys follow the serializer's naming

- **Observed:** the response builder wrote each failure's raw property name. A validator failure arrives already
  camelCased by the library (`rating`), but a malformed body arrives as `SerializerErrors`, so the block answered
  `errors.SerializerErrors` where the library's own reply writes `serializerErrors`.
- **What changed:** each key goes through `Serializer.Options.PropertyNamingPolicy` (read when the reply is built, so a
  policy the caller's delegate sets is the one used); an already-converted key is unchanged.
- **Files:** `Blocks.FastEndpoints/Extensions.cs`, `Blocks.FastEndpoints/README.md` (the `message` and the key
  naming; F258 step 5).
- **Test:** `Blocks.Tests/FastEndpoints/CustomFastEndpointsTests.cs` —
  `AMalformedBody_Answers400_WithTheSerializerErrorsKeyInCamelCase` (fails on the copy as it was: the key is
  `SerializerErrors`); `OneValidatorFailure_ItsTextIsTheMessage_AndItsKeyIsCamelCase` and
  `TwoValidatorFailures_TheMessageNamesSeveralFields` pin the rest of the reply.

## 3. `RequestDiagnosticsMiddleware`: settings for the two limits, and no warning on a streamed reply

- **Observed:** the slow-request warning used fixed limits (1000 ms, 3000 ms for a file transfer), and every streamed
  reply (`text/event-stream`, an answer streamed for 10–60 seconds) logged a warning, burying real slowdowns.
- **What changed:** a new settings type `Blocks.AspNetCore.RequestDiagnosticsOptions` — `SlowRequestMs` (default 1000)
  and `SlowFileTransferMs` (default 3000), each `[Range(1, int.MaxValue)]` — read by the middleware through
  `IOptions<RequestDiagnosticsOptions>`. The block does not bind it: a host binds the section
  (`AddAndValidateOptions<RequestDiagnosticsOptions>`); unbound, the defaults apply. A reply whose content type is
  `text/event-stream` is never warned as slow.
- **Files:** `Blocks.AspNetCore/RequestDiagnosticsOptions.cs` (new), `Blocks.AspNetCore/Middlewares/RequestDiagnosticsMiddleware.cs`,
  `Blocks.AspNetCore/README.md` (the settings type, the streamed-reply exemption, the middleware order and the
  binding line; F258 step 5).
- **Test:** `Blocks.Tests/AspNetCore/RequestDiagnosticsMiddlewareTests.cs` —
  `AStreamedReplySlowerThanTheLimit_IsNotWarned_WhileAnOrdinaryOneIs` (fails on the copy as it was: the streamed
  reply is warned), `TheOrdinaryLimit_ComesFromTheBoundSettings`, `TheFileTransferLimit_ComesFromTheBoundSettings`,
  `TheDefaults_AreOneAndThreeSeconds`, `ANonPositiveLimit_FailsTheSettingsCheck_NamingIt`.

## 4. Observations for the owner, not fixed here

- **The OpenAPI 400 type.** The block sets its own `Errors.ResponseBuilder` but not `Errors.ProducesMetadataType`, so
  an endpoint's generated 400 schema still describes the library's own error reply, not `{statusCode, message,
  errors}`. For the owner: set it to a named reply type, or leave the document as it is.
- **The correlation id is echoed unbounded.** `RequestContextMiddleware` takes a client's `X-Correlation-ID` as it
  comes and writes it back on the reply and into the log scope, with no length or character bound. For the owner: a
  length cap and a character check (falling back to the trace id), or keep the echo as shipped.
