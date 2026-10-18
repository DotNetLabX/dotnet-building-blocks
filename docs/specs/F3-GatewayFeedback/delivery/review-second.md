## Step 2 — Code Review

## Reviewed By
Reviewer (second reader), first review. Diff: working tree against c4211cc, untracked files included.
**Model:** claude-sonnet-5-5

## Verdict: APPROVED

No CRITICAL or HIGH finding. Two MEDIUM (a deleted read-me bullet; three plan instructions on the new types that no test pins) and two LOW (a detached table and a split list), all listed below. Every Carry-Over row is addressed at the end.

## Pre-commitment Predictions
- Source break from `IAggregateRoot<TKey>` gaining `IHasDomainEvents` -> not found. The explicit-implementer class in the compatibility project builds against both c4211cc and the tree.
- A forged who-typed id passing through the minimal-API filter -> present by design (plan Decision R1-02); moved to Open Questions.
- Endpoint-library set-up shared across test hosts -> not found. One assembly-fixture host; the split methods are tested on a fresh `Config` in their own project.
- A read-me or `public-names.md` that is false or incomplete -> found (Findings 1, 2, 3). The statements checked against code were true (error arms, options helpers, gRPC key and channel rules).
- Defaults copied from a sibling type and left unpinned -> found (Finding 4).

## Findings

### [MEDIUM] Domain read-me loses the value-object bullet
**File:** `src/Blocks.Domain/README.md:10-13`
**Origin:** implementation
**Issue:** The edit replaced the line "Value objects: `ValueObject`, `StringValueObject`, `SingleValueObject<T>`" with the new "Audit who as a type" bullet. `grep ValueObject src/Blocks.Domain/README.md` now returns nothing, while `src/Blocks.Domain/ValueObjects/` still holds all three public types. No plan step or spec line asks for this removal, and the read-me test does not look for it. A block's public types are now undocumented.
**Fix:** Restore the bullet beside the new ones.
**Confidence:** 95/100

### [MEDIUM] Three plan instructions on the new types have no test that fails without them
**File:** `src/Blocks.Domain/Entities/AggregateRoot.cs:48` (`CreatedOn` default), `src/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs:43,54` (concurrency switch), `src/Blocks.EntityFrameworkCore/DbContextDesignTimeFactory.cs:31-34` (constructor order)
**Origin:** implementation
**Issue:** Found with a scratch-tree mutation battery (see Evidence; kills count only from a failing assertion, baseline subtracted).
1. `AggregateRoot<TKey, TUserId>.CreatedOn` initial value `DateTime.UtcNow` removed: 81/81 EF tests still pass. Plan step 3 says "the same `CreatedOn` default"; `TwoTypeAuditTests.cs:52` sets `CreatedOn` itself, so the default is never read. An app that saves without setting it would store year 0001.
2. `grep -rn "RowVersion\|HasConcurrencyToken" tests` returns nothing, so the "same `HasConcurrencyToken` switch" of `AuditedEntityConfiguration<T, TKey, TUserId>` (a copy of the two-parameter one) is untested. A try to mutate the `if` as a compile-clean guard failed to build (adjudication bucket, not a kill); the gap is shown by the grep.
3. Swapping the two constructor lookups in `CreateContext` (cache form tried before options-only): 81/81 pass. Every test context has exactly one matching constructor shape, so the plan's "options alone, else options and cache" order is unpinned.
**Fix:** Add three small tests: a new two-type aggregate has `CreatedOn` near now; a configuration with `HasConcurrencyToken => true` gives the model a `RowVersion` property; a context with both constructors is built through the options-only one.
**Confidence:** 85/100

### [LOW] The AspNetCore section of `public-names.md` detaches its three F3 rows from the table
**File:** `docs/public-names.md:24-25`
**Origin:** implementation
**Issue:** A blank line sits between the last pre-existing row and the three new "added" rows, so they render as loose text with pipes, not as table rows. The other sections add rows directly under their table.
**Fix:** Remove the blank line.
**Confidence:** 90/100

### [LOW] The MediatR read-me splits its bullet list around a new paragraph
**File:** `src/Blocks.MediatR/README.md:12-15`
**Origin:** implementation
**Issue:** The paragraph about the behaviour's rule sits between the "Pipeline behaviors" bullet and the `DomainEventPublisher` bullet, which then starts a second list. The binding sentence is present; only the layout is off.
**Fix:** Move the paragraph below the `DomainEventPublisher` bullet.
**Confidence:** 90/100

## Positive Observations
- Plan conformance is tight: every public name the plan binds exists as named; the Step-1 project's three SHA-256 values equal the recorded ones, the folder holds no other file, and the project compiles and passes against a `git archive` of c4211cc (Rule 3 holds mechanically).
- The gRPC move is namespace-only: `git show c4211cc:... | diff -` shows line 8 / line 3 and nothing else for both files; no `Grpc` text in the AspNetCore project file; the old folder is gone.
- Surface comparison reproduced: Release build, 0 warnings; a fresh dump of the 12 blocks (745 lines) against the unchanged reference gives 120 reference lines changed or gone and 181 current lines changed or new, as the header states.
- The mutants aimed at the rulings all died on assertions: whole-path naming policy (the owner's 2026-10-13 ruling), the filter's keep-if-set rule, the behaviour's replace rule, the pre-processor's marker-only guard, the abort call, the PostgreSQL code check, the forwarded `ClearDomainEvents`.
- Negative assertions are reachable: the "marker-only request needs no claim" and "non-auditable request with no claims reader" tests run through the real constraint and the real guard.

## Gaps
- The pre-processor's who-typed and `int` paths are tested only by calling it with a fake context; no test runs it inside the endpoint host (see Open Questions).
- `PackageTests` narrowing of the SNI licence exception (name set, `file ` prefix, `!InBlock`) has no test that can fail today; the author names this. Unchanged by this review.
- The retry policy and the debug-certificate setting of the moved gRPC code are pinned only by the namespace-only diff (author states it; the plan makes no call).

## Open Questions
- **The who-typed path in the minimal-API filter keeps a caller-sent id** (`AssignUserIdFilter.cs`, `keepExisting: true`). For a text who-type a client can name another user in the request body. The plan's Decision R1-02 applies "each step's existing rule" and the read-mes say so, so this is ratified, not a defect; the architect or owner may want to confirm that spoofable text ids are acceptable on this one step. Confidence it is a defect: 40/100.
- **Pre-processor claims-reader resolution** (`AssignUserIdPreProcessor.cs`): `RequestServices.GetRequiredService` replaces the endpoint library's `Resolve`. Inside a host the two should read the same request scope, but no test exercises the pre-processor through a running endpoint host. Confidence it differs: 25/100.

## Carry-Over Findings (implementation.md)
| Title | Disposition | Evidence |
|---|---|---|
| The licence test runs in every step close in this worktree | Confirmed, no action | The worktree lacks the other session's `Speed=Slow` traits; the run record (98.4 s) covers the licence test. Moot once those traits land. |
| The pre-processor resolves the claims reader from `RequestServices` | Confirmed as a deviation; not refuted. Equivalence inside a host rests on the library, untested here | Open Question above; `int` path unchanged otherwise (read against the diff). |
| Test-only `System.Security.Cryptography.Pkcs` 10.0.13 pin | Confirmed, acceptable | The pin carries no `Blocks=` attribute and only the EF test project resolves the package; the complete run is green with `-warnaserror`. |
| Two narrowings in the licence exception have no mutant | Confirmed, LOW, accepted | A package outside the SNI name or with a non-file licence cannot occur today; testing it needs a seam the private helper does not offer. |
| Mutation batteries are the author's | Done | 11 mutants of mine, results under Evidence: 7 killed on assertions, 2 survived, 2 not scorable (Finding 2). |

## Evidence
| Check | Result | Command | Output |
|---|---|---|---|
| Run record | pass | `dotnet test Blocks.slnx -warnaserror` (brief's record) | complete run, 98.4 s, 2026-10-18T13:32:53Z, audit log `D:\src\dotnet-building-blocks\.claude\audit\`; whole suite not re-run |
| Compatibility project against the plan commit | pass | `git archive c4211cc` into `D:\temp\f3-rev-v1`, project copied in, `dotnet test -warnaserror` | 1 passed |
| Step-1 SHA-256 and folder | pass | `sha256sum` of the three files | equal to implementation.md; no other file |
| gRPC move | pass | `git show c4211cc:src/Blocks.AspNetCore/Grpc/{file} \| diff - src/Blocks.Grpc/{file}` | one namespace line each |
| Surface comparison | pass | Release build `-warnaserror`; `tools/Blocks.SurfaceDump` on 12 DLLs; `comm` against the reference | 745 lines; 120 / 181; reference file `git diff --quiet` exit 0 |
| Baseline in scratch copy (`D:\temp\f3-rev-mut`) | pass | `dotnet test` on AspNetCore, MediatR, FastEndpoints, FastEndpoints.SetupTests, EntityFrameworkCore tests | 52, 11, 12, 5, 81 passed |
| M1 naming policy on the whole path | killed (assertions) | scratch `ValidationFailureExtensions.FieldKey` | 5 failed |
| M3 filter `keepExisting: false` | killed | scratch | 1 failed |
| M4 behaviour `keepExisting: true` | killed | scratch | 1 failed |
| M5 pre-processor marker-only guard removed | killed | scratch | 1 failed |
| M6 `context.Abort()` removed | killed | scratch | 3 failed |
| M8 PostgreSQL code check -> any state | killed | scratch | 2 failed |
| M31 `ClearDomainEvents` forward emptied | killed | scratch | 1 failed |
| M26 two-type `CreatedOn` default removed | survived | scratch | 81/81 pass |
| M12b constructor lookup order swapped | survived | scratch | 81/81 pass |
| M14 / M14b concurrency switch | not scorable | scratch | compile error under `-warnaserror`; no test mentions `RowVersion` or `HasConcurrencyToken` (grep) |

*Status: COMPLETE — reviewer (second reader), 2026-10-18*
