# F4-GatewayHandover — Second reader

## Verdict: APPROVED

**Model:** claude-sonnet-5-5

Scope: Step 3 (slow-request settings, streamed-reply exemption) and Step 4 (correlation-id bound), their changes and the code they call; skills check over steps 1–5.

## Pre-commitment Predictions
- Unbound `IOptions<RequestDiagnosticsOptions>` fails to resolve — not found; the options system is always registered in an ASP.NET host, and the defaults apply (read-me says so).
- Streamed-reply check reads `ContentType` at the wrong moment or is case-sensitive — not found; read in `finally`, `OrdinalIgnoreCase`, theory covers mixed case.
- Correlation-id boundary off by one, or a character class that is too wide — not found; `<= 128`, `char.IsAsciiLetterOrDigit` plus `- _ . :`; tests pin 128 kept, 129 replaced, `é` replaced.
- Multi-value header slipping through as the first value — not found; `values.Count == 1`, and the twice-sent test rules out the first value and both joined forms.
- A not-warned test that passes vacuously — not found; each limit test pairs a not-warned arm with a warned one.

## Findings
None at confidence 80 or above.

## Positive Observations
- `RequestContextMiddleware.cs:39-52`: the fallback chain is unchanged and the new rule is two private helpers with no new public surface, as ruled.
- Every rejected-id test asserts header equals context and matches the allowed shape, without predicting the fallback value.
- Skills check, steps 1–5:
  - service-infra-conventions: `RequestDiagnosticsOptions` is data only, named `{Subject}Options`, one class per file at the project root, `init` members, attribute validation, injected as `IOptions<T>` and read once into `_limits`. The unbound-by-the-block departure from "section always required" is the owner's ruling of 2026-10-29, not a defect.
  - create-building-blocks-package: read-mes updated, tests in the library's own test projects, no documentation comments on self-evident members; the one comment (`Compiled, never called`) is a constraint comment.
  - error-handling and tdd: no departure found in the changed code.

## Gaps
- The `values.Count == 1` guard is exercised only if the TestServer client delivers two separate header values. If it joined them, the character test would reject the joined line anyway and the guard would survive a deletion mutant. I did not run a mutation battery, so this is unverified and not a finding.

## Open Questions
- None blocking (see Gaps).

## Carry-Over Findings
- "Step 4's right-reason red never observed on a run" (low): refuted as a risk. The old code was `!string.IsNullOrWhiteSpace(value) → value.ToString()`, which keeps every rejected input (129 characters, space, `<`, `é`, two values joined). Each rejected-case test asserts `header != sent`, so each must fail against the old code.
- "Surface dump read Debug, not Release, assemblies" (low): outside my scope (step 5). The public surface does not depend on the build configuration, and I raise no objection.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | `mvtq1ycd-a04fe60f` — the handover's tied run over every changed file | cited as given; no suite re-run |
| Platform-conditional grep | none | no OS-conditional code in the diff | n/a |

*Status: COMPLETE — reviewer (second reader), 2026-10-29*

Review: docs/specs/F4-GatewayHandover/delivery/review-second.md
