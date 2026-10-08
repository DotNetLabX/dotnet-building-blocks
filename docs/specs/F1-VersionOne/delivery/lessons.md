# F1 — Version One of the generic building blocks — Lessons

## Architect Lessons
- A hand-off written in another repo stated an owner ruling ("a copy is never edited in an app") the owner says
  they never made. Treat a hand-off's "settled" list as claims to confirm with the owner when a design turns on
  one, not as rulings.
- The first ruling question on the 20 conflicts was not understood ("don't get this"): a list of code-shape
  conflicts needs a one-page picture first (here `conflicts.drawio`), then the question.
- Both spec readers found the same core gap: a two-way file sync needs a three-way comparison (app copy, last-copied
  base, other side) stated in the spec, plus explicit first-take, malformed-lock and dropped-entry rules. A
  two-way lock design without them reads complete and is not.
- Plan-time grep beat the reviewers' own rename list: three renames they filed under one block live in another
  (Redis, not EF Core). Re-grep any reviewer-supplied list before it becomes a table.
- **A gate named "step N's test, run at the end of this step too" must exist by that step.** The plan named step 13's hygiene tests as the gates of steps 3 and 4, which run two slices earlier; the developer had to ask. At plan time, for every acceptance line that cites another step's artifact, check the step order (and the build-slice line) puts the artifact first. [F1-VersionOne]
- **A before/after record of foreign repos must be shaped for the narrowest compare a likely ruling could ask for.** One SHA-256 over the whole `git diff` could only be compared whole; when live owner drift forced a scoped compare, the record needed per-path hashes. Record per path; whole-repo equality is still all lines equal. [F1-VersionOne]
- **Delivery evidence that lists another repo's file names must not be committed to a public repo.** The foreign-repo snapshot sat in the tracked delivery folder of a public repo and would have published the owner's private untracked file names; git-ignore such records and keep them on disk. [F1-VersionOne]
- **A fix ported from a sibling repo is a claim, not evidence — probe it on this repo's library version before planning it.** D3 ("upsert copies tracked values") came from sprint-rituals as a bug fix; on EF Core 10 the old line already saved field-backed values, and the ported line erased shadow-property values (shadow FKs to null). A five-minute scratch probe at plan time, old line vs new line on the target version, would have caught both. When a plan says "red first on the old code" for a ported fix, run that red at plan time. [F1-VersionOne]
- **A plan's `.gitignore` list for pipeline tooling state was written from memory and missed files.** Step 1 ignored `.claude/.current-agent`, `.claude/audit/` and `.claude/.pipeline-state`, but `.claude/.worktree-target` (published by the coordinator) and a stray `docs/specs/{slug}/delivery/.claude/audit/` (hook tooling writing relative to its working folder) were left untracked and un-ignored at done-check time. When a plan writes ignore rules for tooling state, ignore the whole ephemeral class (e.g. every `.claude/` entry the tooling writes, or `**/.claude/audit/`), not a remembered subset. Evidence: [F1-VersionOne]
- **A fresh fix-round developer invoked no mapped skill.** It rebuilt work under steps 2, 3, 4, 5, 6 and 10 and offered its red-first record as the tdd evidence. Earlier invocations under the same token came from other developer spawns, so they cannot cover it: the re-check window starts at `Fix dispatched`. Check 2 failed on eight skill slips with no content gap. The fix-round dispatch should repeat "the done-check scores the skill log, not the self-report" and name the mapped skill of every step its fix-list rows touch. Evidence: [F1-VersionOne]

## Skill Gaps

### public-surface-diff
- **Kind:** missing
- **Searched for:** a way to list a library's public and protected surface and diff two builds of it
- **Why it would help:** a rename or move pass on shared libraries needs a mechanical "only these names changed"
  gate; the plan builds a one-off dump tool instead (plan step 12)

### block-sync-tool
- **Kind:** missing
- **Searched for:** a pattern for copying whole source projects between repositories with a lock file and refusal on edits
- **Why it would help:** every app that takes blocks needs the same tool; the plan specifies it inline (step 10)
- **References:** `tools/Blocks.Sync/` (three-way rule in `ForwardCommand.cs` / `BackCommand.cs` / `StatusCommand.cs`, containment in `Containment.cs`, temp-file-then-move writes in `SafeFiles.cs`); `tests/Blocks.Sync.Tests/SyncFixture.cs` (temp git repos, before/after tree listing, junction and hard-link set-up)
- **Evidence:** [F1-VersionOne]

### convention-sweep
- **Kind:** missing
- **Searched for:** a skill for applying comment and naming conventions across copied code (markers, doc comments, Async suffixes, namespace-follows-folder)
- **Why it would help:** steps 3 and 4 are mechanical sweeps written inline with their greps

### block-readme
- **Kind:** missing
- **Searched for:** a template for a generic block's read-me (purpose, dependencies, registration)
- **Why it would help:** convention A11 asks for one per block; step 9 describes it inline

### block-portability-check
- **Kind:** missing
- **Searched for:** a check that copies a library project alone into an empty solution and builds it
- **Why it would help:** proves a shared block carries no hidden dependency on its home repo; step 11 inline

### persistence-patterns (UpsertAsync value-copy rule)
- **Kind:** ill-fitting
- **Searched for:** the upsert copy line for `RepositoryBase.UpsertAsync`
- **Why it would help:** the skill prescribes `SetValues(Entry(entity).CurrentValues)` and says `SetValues(object)` skips private-field properties. On EF Core 10.0.12 the reverse holds: `SetValues(entity)` copies field-mapped properties, and the prescribed line overwrites shadow values (a foreign key with no CLR property, shadow columns) with the detached instance's nulls. F1 kept `SetValues(entity)` by owner ruling (Q4); the skill rule should be corrected or made version-specific.
- **References:** `src/Blocks.EntityFrameworkCore/Repositories/Repository.cs` (`UpsertAsync`); `tests/Blocks.EntityFrameworkCore.Tests/RepositoryTests.cs` (`Upsert_UpdatesAPropertyStoredInAPrivateField`, `Upsert_KeepsShadowPropertyValues`)
- **Evidence:** [F1-VersionOne]

## Developer Lessons
- **NuGet global packages on this machine are at `D:\packages\nuget`**, not `~/.nuget/packages`. Any step that reads `.nuspec` files or package DLLs (step 13's licence test, the surface dump's `--probe`) must ask `dotnet nuget locals global-packages --list`, not hardcode the home-folder path.
- **Reading a class library's public surface with MetadataLoadContext** needs the package DLLs beside it. Build with `-p:CopyLocalLockFileAssemblies=true`; a plain library build leaves them in the NuGet cache and type loading fails.
- **`git mv -k` on an untracked file exits 0 and does nothing**, so `git mv -k a b || mv a b` silently skips the rename, and it is a git write a developer may not make anyway. Use plain `mv` for file renames.
- **The plan's commented-out-code rule** (ends in `;`, `{` or `}`, or starts with a statement keyword) misses fluent-chain lines (`//.Select(...)`) and lines in the middle of a commented block that end in `,` or `(`. A comment sweep needs a read of every `//` line, not only the test.
- **Exact multi-line source edits from Git Bash on Windows:** a perl script file with `index`/`substr`, an abort when an anchor is missing, and a uniqueness check worked first time. Inline `perl -pi -e` with `\\n` in the replacement and `$'…'` CR patterns failed silently or wrote literal newlines.
- **EF Core 10's `PropertyValues.SetValues(object)` is not the field-skipping bug the persistence-patterns skill describes** when the object is the entity's own CLR type: it reads through the model's getters, which honour `PropertyAccessMode.Field`, and even fills a field-only property from an anonymous object with a member of that name. A plan step that asks for a red-first upsert test on a field-backed property cannot get one on EF 10 (F1 Q4). Probe the claimed bug on the target EF version before planning a red-first test for it.
- **FastEndpoints' `UseFastEndpoints()` casts the app builder to `IEndpointRouteBuilder`**, so on a `HostBuilder` + `UseTestServer` host it throws `InvalidCastException`. Use `app.UseRouting(); app.UseEndpoints(e => e.MapFastEndpoints());`, and give the test assembly one endpoint: FastEndpoints refuses to start with none.
- **`python3` is the Microsoft Store stub, but `python` works** (fix round 1). A Python heredoc that writes C# turned `'\0'` and `'\t'` char literals into raw NUL and TAB bytes, and `'\\'` into `'\'`, in the written file. Use the Edit tool for any edit that carries a backslash escape, and grep the result for `\x00` after any scripted edit.
- **`NullabilityInfoContext` reports an unconstrained generic `T` as nullable** whether the source says `T` or `T?`, so a surface dump built on it cannot see a `T` → `T?` change. Read the declaration's `[Nullable]` bytes (falling back to `[NullableContext]` on the member, then the declaring types) in the compiler's pre-order instead (`tools/Blocks.SurfaceDump/NullableFlags.cs`). A step-12-style "every diff line is explained" check should also run the other way — every source-visible change in the step log has a diff line.
- **Simulating a client abort in a TestServer test:** set an `IHttpRequestLifetimeFeature` whose `RequestAborted` is already cancelled in a middleware ahead of the one under test; the reply can still be read. `HttpContext.Abort()` makes the client throw instead.
- **Hand mutation batteries under `TreatWarningsAsErrors`:** `if (x)` → `if (false)` raises CS0162 (unreachable code), and deleting the only statement of a `foreach` leaves a body that does not compile; both read as "killed" when a script scores only the test runner's exit code. Write the predicate mutant as `if (x && false)`, replace a deleted statement with `_ = variable;`, and have the script report `error CS` lines as COMPILE-FAIL, separate from kills.
- **A `dotnet build` started from inside a test** (the portability check, the licence test's `dotnet list`) should drop the inherited `MSBuild*` and `VSTEST*` environment variables, so the child resolves its own SDK and does not inherit the test host's MSBuild settings.
- **Test writers on Windows:** `StringWriter.WriteLine` writes CRLF, so tests that compare a CLI's captured output against `\n` text should normalise with `ReplaceLineEndings("\n")` in the harness, not in each assertion.
- **Junctions in temp test trees:** `Directory.Delete(root, recursive: true)` fails with access denied on a tree holding an `mklink /J` junction. Delete the junctions first (non-recursive `Directory.Delete(link)`), then clear read-only attributes (git objects) and delete the tree.
- **`cmd /c mklink /J` reads a forward-slash path segment as a switch** (`…\Data/Link` → `Invalid switch - "Link"`), so a link path built from a `/`-joined relative path fails before the test starts. Build link paths with `Path.Combine`.
- **A fix round still owes each touched step's mapped skills** (fix round 1 → 8 skill slips in done check 2). Map every fix-list row to the plan step whose code it touches, and invoke that step's Skill Mapping skills (and `tdd` on `TDD: yes` steps) through the Skill tool before the first edit; the done check scores the skill log, not the red-first record.

## Reviewer Lessons

- (second reader, F1-VersionOne) A generated-list gate is only as good as its instrument, and the plan's stop rule checked one direction only. `tools/Blocks.SurfaceDump` prints `T?` for an unconstrained generic `T` whatever the source says, in the baseline and the final dump alike, so source edits `T` to `T?` (Core cache members, `JsonExtensions.DeserializeCaseInsensitive<T>`) never became a diff line and never reached `docs/public-names.md`, while step 12's "every diff line is accounted for" still read green. When a plan makes a generated list an acceptance criterion, add the reverse check (every source-level public edit logged in implementation.md maps to a row) and give the instrument a differential fixture for each annotation class it claims to catch. Evidence: [F1-VersionOne]
