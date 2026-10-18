# F3-GatewayFeedback — lessons

## Architect Lessons

- **Brainstorm round shown without its problem (2026-10-11).** Round 1 put idea tables on the duplicate-key and
  validation-shape topics without first saying, in plain words, what goes wrong today. The owner had not read the
  source file and could not react ("don't understand the problem, explain it"). Each round must open with the
  problem — what happens today, one concrete example, why it is hard — before any idea. Routed to the plugin by the
  owner (brainstorm-mode, § A round).

- **A spec clause that rests on another library's behaviour is probed before it is written.** The spec said the
  error handler would use "the same key conversion the endpoint-library set-up applies (camelCase, nested names
  included)"; a five-minute probe at plan time showed the library lowers only the first letter of a nested path,
  and the plan review found its set-up is process-wide. Both were facts about a third-party library that the spec
  review took on trust. **Apply:** at spec time, run the smallest probe for any "library X does Y" premise a
  requirement is built on, and quote its output in the spec.
- **A fold must never label its own additions as an owner ruling.** Step 9 read "Test-only packages (owner, 2026-10-13): Microsoft.Data.SqlClient and Npgsql", but the owner had ruled only on PostgreSQL; SqlClient came from the architect's fold of critic finding R1-07. The owner caught it mid-build ("not me!"). When a fold adds to an owner-ruled list, attribute the addition separately (`architect, fold of R1-07`). (done check, 2026-10-18)
- **A licence fact covers the package's whole resolved closure, not the package alone.** The plan's "Microsoft.Data.SqlClient is MIT" was true of the package and false of its native dependency `Microsoft.Data.SqlClient.SNI.runtime` (proprietary file licence), which blocked step 9 on an owner question. At plan time, restore the package in a scratch project and run the repo's licence test (or list `project.assets.json`) before claiming a licence test will pass. (done check, 2026-10-18)

## Skill Gaps

- No skill for a source-compatibility test project (a frozen project of old consumer code whose build pins
  "existing code keeps compiling") — F3 plan step 1.
- No skill for a block read-me — F3 plan step 10 (also F1 step 9).
- No skill for a public-API surface comparison against a baseline dump — F3 plan step 11 (also F1 step 12).

## Developer Lessons
- A run in a git worktree has no `.claude/verify.json` when the main checkout's copy is untracked: `verify-run.js`
  follows the session's `.worktree-target` to the worktree and records `undeclared`, so every step close silently
  drops to the fallback unless whoever creates the worktree copies the untracked `.claude/` config across. A plan
  written in the main checkout may also describe uncommitted edits (here another session's test traits) that the
  worktree does not carry. — F3 Phase 1.
- A sed mutation battery that restores the file with `mv` from a plain `cp` copy gives the restored source an older
  timestamp than the mutant's build output, so MSBuild skips the rebuild and every later run tests the stale mutated
  DLL — a green baseline then reads red (or a red one green). Restore with `touch` after the `mv` (or `cp -p` the
  other way round) and re-assert a green baseline after each project's mutants. — F3 step 4.
- FastEndpoints 8.3.0's `HttpContext.Resolve<T>()` reads the process-wide static `ServiceResolver.Instance`, not
  `HttpContext.RequestServices`, so code that uses it cannot be called without an endpoint host (or the static set-up
  `Factory` provides). Block code that must be unit-testable without a host resolves from `RequestServices`. — F3
  step 4.
- FastEndpoints 8.3.0's `UseFastEndpoints` casts the app to `IEndpointRouteBuilder` (`Cannot cast [app] to
  IEndpointRouteBuilder`), so a test host for it is a `WebApplication` with `WebHost.UseTestServer()`, never
  `HostBuilder().ConfigureWebHost(...).Configure(app => ...)`. Its `Config` options expose `RoutePrefix` and
  `ResponseBuilder` through internal getters only, and a fresh `Config` has no naming policy: the library takes the
  host's JSON options when the host starts. A test of a `Config` extension that runs without a host sets the policy
  itself and reads the two members by reflection. — F3 step 6.
- A hand-written mutant that only reformats its anchor reports SURVIVED while mutating nothing. A deletion mutant's
  replacement must drop the guarded branch, not re-spell it. — F3 step 6.
- `verify-run.js` keeps only the head and tail of a failing run (`output_excerpt`, about 1.6k characters), so a failing
  xUnit assertion's message and an exception thrown in a fixture constructor are usually elided, and the test-entry
  gate blocks a raw `dotnet test` to read them. What worked: one `--files` run per failing class (a shorter tail), or
  a throwaway probe test that writes `ex.ToString()` to a file under `D:\temp`, removed afterwards. Plugin-side, the
  record could keep each failed test's message. — F3 steps 8–9.
- A plan fact "package X is MIT" covers the package, not what it brings: `Microsoft.Data.SqlClient` (MIT) always
  brings `Microsoft.Data.SqlClient.SNI.runtime` under a proprietary file licence, and 7.1.1 also brings a
  `System.Security.Cryptography.Pkcs` version with a fresh advisory that fails restore under `-warnaserror`. Check a
  new package's whole transitive set (`dotnet list package --include-transitive` plus each nuspec's `<license>`)
  before a plan names it. — F3 step 9 (Q2).
- EF Core 10 refuses `Migrate`/`MigrateAsync` when the context has migrations but no model snapshot
  (`PendingModelChangesWarning` is an error by default). A test with a hand-written migration ignores that warning in
  its context options. — F3 step 8.
- A later feature's surface diff against a fixed reference mixes the two features' lines, and the earlier feature's
  own lines drop out where the later one changes them again. Building `git archive HEAD` in a temp folder (read-only
  git) and dumping it reproduces the earlier counts (here 102 / 108) and separates the two features' lines exactly
  (24 removed, 79 added). — F3 step 11.
- A read-me test of "must hold / must no longer hold" phrases guards only what the plan names, so a rewrite can
  silently drop a true line nobody listed (here the Domain read-me's value-objects bullet, caught by the second
  reader). After rewriting a read-me, diff it against `git show HEAD:{path}` and account for every removed line. —
  F3 fix round 1.

## Reviewer Lessons

- Members a new type copies from a sibling (a `CreatedOn` default, a `HasConcurrencyToken` switch, a constructor-lookup order) are the class an author's mutation battery skips: the author mutates the branches it reasoned about, and a copied default reads as "same as the sibling". In F3 (GatewayFeedback) three such members survived an independent battery; aim the first mutants at them. Evidence: [F3-GatewayFeedback]

## Team Lead Lessons

- **Worktree carry-over must include the session's own untracked settings.** At the worktree election I carried the slug's untracked spec/plan folder but not `.claude/verify.json`, which is untracked as a whole in this repo; the developer found the gap at Phase 1 and every step close would otherwise have refused. Carry-over checklist: the slug folder, plus any untracked file a hook or runner reads from its working folder (`.claude/verify.json`, `.claude/test-times.json`). Filed as plugin feedback.
- **Check provenance labels before relaying a licence or dependency question.** The plan labelled both test libraries "(owner, 2026-10-13)"; the owner had ruled only on PostgreSQL. Re-reading the cited ruling line before the first ask would have put the right facts ("the architect added SqlClient") in the first question instead of the second.
- **A slow-test exclusion living in another session's uncommitted work does not reach a worktree.** The unit filter excluded nothing in the F3 tree, so step closes ran up to 71.9 s of a 75 s limit. Worth naming at the election as a cost of the worktree option (it was named).
