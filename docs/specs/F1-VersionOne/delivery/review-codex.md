# F1-VersionOne — Codex code review (primary)

Persisted by team-lead from codex-job collect (job task-muzhze2z-7vfl5c, Codex session 01a11b6e-b785-7711-96ee-86a30d9cabf8), 2026-10-08. Read-only run; model: Codex CLI default.

## Verdict: NO-GO

Read-only review completed. No files were written, and no builds or tests were run. The supplied handover record confirms **148/148 tests passed, exit 0**; the cases below are not covered by those tests.

1. **HIGH — [SyncSession.cs:33](D:/src/dotnet-building-blocks/tools/Blocks.Sync/SyncSession.cs:33): Lock contents are never validated against their recorded commit.** Only commit existence is checked. A syntactically valid lock with incorrect file lists or fingerprints becomes a false comparison base and can allow an unsent app edit to be overwritten. **Suggested fix:** compare each lock entry with the block’s normalized file set and fingerprints at its recorded commit, rejecting inconsistencies before any writes.

2. **HIGH — [FirstTake.cs:12](D:/src/dotnet-building-blocks/tools/Blocks.Sync/FirstTake.cs:12): Adoption can overwrite edits hidden from git status.** A committed file marked `assume-unchanged` can contain local edits while this check reports a clean folder. `forward --adopt` then replaces content that cannot be recovered from history, violating Rule 7. **Suggested fix:** independently verify the physical file set and contents against committed blobs; add a temporary-repository test with an edited `assume-unchanged` file.

3. **MEDIUM — [SafeFiles.cs:36](D:/src/dotnet-building-blocks/tools/Blocks.Sync/SafeFiles.cs:36): Mirroring fails when a file becomes a directory, or a directory becomes a file.** New files are written before obsolete files are removed. For example, replacing `Data` with `Data/item.json` fails because `Data` still exists as a file. Earlier writes may already have occurred. **Suggested fix:** preflight topology changes and remove conflicting obsolete entries before creating replacements, preserving excluded files. Cover both directions in forward and back tests.

4. **MEDIUM — [ForwardCommand.cs:94](D:/src/dotnet-building-blocks/tools/Blocks.Sync/ForwardCommand.cs:94): Package-report errors occur after blocks and the lock have been changed.** Malformed app package XML, or a missing source central file, produces exit 2 after the copy and lock refresh. **Suggested fix:** build and validate the package report before the first write; test that these failures leave both repositories unchanged.

5. **MEDIUM — [Containment.cs:41](D:/src/dotnet-building-blocks/tools/Blocks.Sync/Containment.cs:41): The app/source root itself bypasses the junction check.** `CheckNoLinks` starts checking below `root`. Passing a junction as `--app` or `--source` therefore permits writes through it, despite the plan requiring existing directories on write paths to be refused. **Suggested fix:** include the root in link validation and preflight the lock’s parent as well as block destinations.

6. **MEDIUM — [TypeNames.cs:89](D:/src/dotnet-building-blocks/tools/Blocks.SurfaceDump/TypeNames.cs:89): The surface dump misses real unconstrained generic nullability changes.** The reference declares `DeserializeCaseInsensitive<T>` returning `T`, and this implementation changes it to `T?`, yet both dumps print `T?`. Cache return/out annotation changes are similarly hidden, leaving `docs/public-names.md` incomplete. **Suggested fix:** preserve declaration-level generic nullable annotations, add unconstrained `T` → `T?` fixtures, and regenerate the comparison and names list.

7. **LOW — [DependencyInjection.cs:7](D:/src/dotnet-building-blocks/src/Blocks.Core/Mapster/DependencyInjection.cs:7): The extension-class naming pass is incomplete.** Core’s and Messaging’s `DependencyInjection` classes, and Hasura’s `HasuraRegistration`, still lack the required `Extensions` suffix. **Suggested fix:** rename these classes, update references and public names, and add an extension-class naming check.

8. **LOW — [IThreadSafeMemoryCache.cs:5](D:/src/dotnet-building-blocks/src/Blocks.Core/Cache/IThreadSafeMemoryCache.cs:5): Commented-out inheritance remains.** `//:IMemoryCache` violates the comment cleanup requirement and escapes the whole-line comment gate. **Suggested fix:** remove it and extend the gate to detect inline commented-out declarations.

9. **LOW — [SyncCli.cs:54](D:/src/dotnet-building-blocks/tools/Blocks.Sync/SyncCli.cs:54): Invalid CLI paths can escape the exit-code contract.** `Path.GetFullPath` runs outside the handled `try`; an empty path passed to the public `Run` entry point throws instead of returning 2. **Suggested fix:** validate paths within handled parsing and return the documented usage/environment error.

The **Carry-Over Findings** table was checked row by row:

| Row | Result |
|---|---|
| 1 — ServiceModel pin | **Confirmed.** None of the 11 existing block assets files resolves `System.ServiceModel.Primitives`; the unused annotated pin remains at `Directory.Packages.props:45`. |
| 2 — MapToConstructor | **Confirmed.** `Extensions.cs:12` uses `GetConstructors().First()`, and the documentation now describes that behavior. |
| 3 — Nullable route value | **Confirmed.** Interface and implementation return `string?`; `docs/public-names.md:18` records it. |
| 4 — Comment gate gaps | **Confirmed.** `CommentTests.cs:40` misses fluent continuation lines and lines ending with `,` or `(`. Finding 8 identifies another surviving gap. |
| 5 — Hasura SQL line endings | **Confirmed.** Block files use LF, including the raw SQL at `HasuraMetadataService.cs:16`. |
| 6 — Parameterized reseed name | **Confirmed in source.** `DbContextExtensions.Seed.cs:90` parameterizes the table name, and failures are caught and printed. SQL Server execution was not independently verified; the README identifies the helper as SQL Server only. |
| 7 — Development 500 message | **Confirmed.** `GlobalExceptionMiddleware.cs:76` masks the message only outside development, matching the plan. |
| 8 — Messaging narration comments | **Confirmed.** They remain at `DependencyInjection.cs:29` and `RabbitMqOptions.cs:5`; the specified marker gate permits them. |
| 9 — Duplicate git wrappers | **Confirmed.** Both tools contain their own process wrapper; the sync version adds its non-throwing variant. |
| 10 — Cache parameter annotations | **Confirmed observation.** The two dump changes occur despite unchanged parameter source. Finding 6 shows that the nullable surface accounting needs correction. |
| 11 — Containment terminal guard | **Confirmed for the lexical failure branch.** After `CheckRelative` succeeds, the starts-with failure is redundant; it does not provide filesystem-link containment. |
| 12 — Git/mklink tests | **Confirmed.** Windows tests use `cmd /c mklink`; non-Windows branches use symbolic-link creation and `ln`. The supplied run provides Windows evidence only. |


<!-- Re-review after fixes, cycle 1 — job task-muzmbli0-gp7bjx, Codex session 01a11bdd-eaee-7392-9755-eef5395f8f44, persisted by team-lead 2026-10-08 -->

## Verdict: NO-GO

1. **MEDIUM — [tools/Blocks.Sync/SafeFiles.cs:51](D:/src/dotnet-building-blocks/tools/Blocks.Sync/SafeFiles.cs:51): Empty directories bypass the topology preflight.** `EntriesUnder` omits ordinary directories. When replacing `Data/Item.cs` with a file named `Data`, an additional empty `Data/Empty/` passes preflight but prevents `Delete` from removing `Data`. `Mirror` deletes `Item.cs`, then fails writing its replacement, leaving the destination partially changed. **Suggested fix:** account for empty directories during preflight and remove safely replaceable directory trees before writing, or refuse before any mutation. Cover forward and back with empty target folders and empty sibling directories.

2. **LOW — [tools/Blocks.Sync/FirstTake.cs:57](D:/src/dotnet-building-blocks/tools/Blocks.Sync/FirstTake.cs:57): The physical comparison misses hidden deletions.** It checks only files still present on disk. Delete a committed file marked `--skip-worktree`, leaving other block files present: Git treats the absent file as unchanged, and this check never visits its committed entry. Adoption proceeds despite a physical file-set difference, leaving row 2 incompletely closed. [Git documents this behavior](https://git-scm.com/docs/git-update-index#_skip_worktree_bit). **Suggested fix:** compare both file sets, filtering committed paths through the same exclusions, and refuse missing physical files. Add a hidden-deletion regression case.

Each fix-list row was checked:

| Row | Result |
|---|---|
| 1 | Closed — lock file lists and fingerprints compared against recorded commits before writes. |
| 2 | Partial — hidden content edits caught; hidden deletions remain unchecked, finding 2. |
| 3 | Partial — covered topology transitions work; empty-directory case remains, finding 1. |
| 4 | Closed — package report validated before copying or writing the lock. |
| 5 | Closed — root junction checks include the lock-only forward case and back destinations. |
| 6 | Closed — unconstrained nullable annotations distinguished; fixtures and inventory updated. |
| 7 | Closed for the three requested registration renames; naming gate added. |
| 8 | Closed — inheritance comment removed; inline declaration detection added. |
| 9 | Closed — invalid CLI paths return usage exit 2. |
| 10 | Closed — relative path forms normalized; empty results name the cause. |
| 11 | Closed — logging threshold, mapped key column and default-source tests present. |
| 12 | Closed — narration cleanup applied across the blocks. |
| 13 | Closed — unused ServiceModel pin and README mentions removed. |
| 14 | Closed — non-500 message behavior documented. |
| 15 | Closed — Q5 abort-token filter and revised cancellation tests implemented. |

**Guard carry-over confirmed:** `Guard.cs:19` retains one extension method beside ordinary helpers; `GuardExtensions` exists, and `NamingTests.cs:73` deliberately excludes mixed classes.

The read-only surface dump reproduces **684 reference / 690 current lines**, with **102 removed or changed / 108 added or changed**. The supplied run confirms **183/183 passed, exit 0**, with profile `undeclared`. No files were written; no builds or tests were run.


<!-- Re-review after fixes, cycle 2 — job task-muzn2hut-3u80zg, Codex session 01a11bf1-11df-7090-94f2-1643dec8fd81, persisted by team-lead 2026-10-08 -->

## Verdict: NO-GO

1. **MEDIUM — [tools/Blocks.Sync/SafeFiles.cs:75](D:/src/dotnet-building-blocks/tools/Blocks.Sync/SafeFiles.cs:75): The deletion helper can traverse a junction and delete external empty directories.** If the source adds a file named `bin` while the app’s `bin` is a junction to an external tree containing empty subfolders, `BlockVersion` skips that directory and topology preflight allows replacement. `DeleteEmptyFolders` checks only children for `ReparsePoint`, so it enumerates through the starting junction and deletes the external subfolders, violating Rule 4. This follows from static tracing and the [.NET enumeration implementation](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/Enumeration/FileSystemEnumerator.Windows.cs). **Suggested fix:** reject a linked replacement target during preflight, before any mutation; also guard the deletion helper’s starting directory. Add forward/back regressions asserting that external directories, block files and the lock remain unchanged.

Cycle-2 row 1 is closed according to Done-Check 3. Row 2 fixes the previously reported empty-target and sibling-folder cases; the finding above is new.

The supplied run confirms **189/189 passed, exit 0**, profile `undeclared`. No files were written or builds/tests run during this review.
