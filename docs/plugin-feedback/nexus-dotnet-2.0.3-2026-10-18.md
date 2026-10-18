# nexus-dotnet 2.0.3 — feedback from dotnet-building-blocks (2026-10-18)

Source: F3-GatewayFeedback (plan step 12, owner: close).

## error-handling skill teaches a validation reply and an argument-error status these blocks no longer use

- **Skill:** `nexus-dotnet:error-handling`.
- **What it teaches today:** the validation reply as a flat list (`List<ValidationError>` of property and message), and an argument error (`ArgumentException`) answered as 400.
- **What the blocks do after F3:**
  - an argument error is a programming fault and answers **500** (fixed message outside development, logged at Error) — change C4;
  - the validation reply's `errors` is a **per-field map** (`{ "field": ["message", …] }`), with every part of a nested name lower-cased by the naming policy (`Address.Street` → `address.street`, `Items[0].StreetName` → `items[0].streetName`), written the same way by the global error handler and the FastEndpoints reply builder — change C7 (owner ruling 2026-10-13).
- **Ask:** update the skill's reply shape, its status table and its examples to match, or name both shapes and say which the shared blocks use.
- **Evidence:** `src/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`, `src/Blocks.AspNetCore/Extensions/ValidationFailureExtensions.cs` (`ToFieldMap`), `src/Blocks.FastEndpoints/Extensions.cs`; the read-me statements in `src/Blocks.AspNetCore/README.md` and `docs/public-names.md` (wire table, "Since F3" column).
