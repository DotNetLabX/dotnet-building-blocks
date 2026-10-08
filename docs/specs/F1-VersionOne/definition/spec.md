# F1 — Version One of the generic building blocks

**Traces to:** none (new repo, no product doc) — source is the hand-off `D:\src\knowledge-gateway\docs\specs\F194-ReferenceFoundations\delivery\handoff-blocks-repo.md` and the owner's rulings of 2026-10-08 in this session
**Source:** Scratch (technical feature; architect-authored tech-spec)
**Dependencies:** None
**Status:** Ready
**Model:** claude-opus-5-5
**Plan:** `docs/specs/F1-VersionOne/delivery/plan.md`
Rule list: not landed — the owner chose to skip the rule list for this release (2026-10-08)

---

## Purpose

The generic building blocks (`Blocks.*`: one technology or concern each, no product knowledge) exist in three
diverged copies and have no home. This repo becomes their one home. Version one gives knowledge-gateway a
usable set now: the reference's blocks on .NET 10, cleaned and with known bugs fixed, plus a script that copies
whole blocks between this repo and an app, in either direction. Improvements come in later versions through the
same script.

## What version one contains

**The blocks.** The reference's 11 generic blocks (`D:\src\dotnet-microservices\src\BuildingBlocks`):
Core, Exceptions, Domain, AspNetCore, EntityFrameworkCore, FastEndpoints, MediatR, Messaging, Redis,
Http.Abstractions, Hasura. The reference's `Articles.*` projects are application blocks and stay out. The reference
is the "Articles" journal-publishing product.

Each block, compared with the reference:

1. Targets .NET 10; package versions come from one central file in this repo; no byte-order mark in any file.
2. Builds on its own when copied into an app: its project file states everything it needs, and nothing is
   inherited from a file at this repo's root except package versions.
3. Namespaces fixed: the misspelled one, and every namespace that disagrees with the other files in its own
   folder — aligned to the namespace most files in that folder use, or to the folder path on a tie, so the fewest
   public types move (§ Public names that differ from the reference).
4. The reference's product wording removed: the article-id route helper, a local variable named for articles,
   and a comment naming journals.
5. Comments brought to the settled conventions: no course-note markers, no to-do comments, no commented-out code,
   no member documentation except where behaviour cannot be read from the signature; a short read-me per block.
   The naming conventions that rename public members apply too: every asynchronous method ends in "Async", and a
   class of extension methods is named "…Extensions" (owner, 2026-10-08; the renames are in § Public names).
   The other code-shape conventions (for example, settings members set only at creation) wait for version two.
6. MediatR on its last free release (12.5.0, Apache-2.0); MassTransit on 8.x; no other commercial package,
   test projects included.
7. The naming changes of items 3–5 and the departures, additions and fixes below, and only these, change the
   reference's shapes or behaviour.

**Departures from the reference (owner, 2026-10-08).** The three copies conflict in 20 places
(`docs/specs/F1-VersionOne/definition/conflicts.drawio`). 16 keep the reference's shape. 4 change:

| # | Area | Change | Why |
|---|---|---|---|
| D1 | Error mapper | Adds forbidden 403, already exists 409 and bad gateway 502. Any other HTTP error uses its own status instead of becoming 500. Outside development a 500 carries the fixed text "An unexpected error occurred." and never the internal message; in development it also carries the error's details. Reply names in camelCase. The block's own "ensure not exists" repository helper throws the already-exists error, so it answers 409. A cancelled request answers 499 when the response has not started — also when the cancellation is the inner cause of another error; once the response has started, the status is left as it is. | the owner's 409 ruling; a 500 leaked internals; a new error type fell to 500; a cancelled request was logged as a server failure |
| D2 | Table set-up | The created-on default that works only on SQL Server is removed, with the overridable member that held it. The two seeding helpers that run SQL Server-only statements (the manual-id insert scope and the table reseed) stay, and the block's read-me says they need SQL Server (owner, 2026-10-08) | a block must work with any database its technology supports; the two helpers are opt-in |
| D3 | Repository upsert | Copies the tracked values, so properties stored in private fields are saved on update | bug, fixed in sprint-rituals |
| D4 | FastEndpoints event publisher | Publishes by the event's real type | sprint-rituals found that, on FastEndpoints 8.x, no specific handler ran; the reference makes the same call and version one moves to 8.x, so a test proves the fix there |

**Additions and fixes found while reading the reference:**

- **A1** — Three new error types: forbidden, already exists (conflict), bad gateway — each with its status.
- **A2** — The HTTP-context claims provider's "values of a claim" returns the claim it was asked for (today it
  always returns the role claims).
- **A3** — A short read-me per block: what it is for, what it needs, how to register it.
- **A4** — The repository's delete-by-id works: today the table name is sent as a query parameter, so the
  statement cannot run.

## The sync script

The script lives in this repo and runs from a checkout of it. It never commits, pushes, or edits an app's
package-versions file.

**The manifest** sits at an app's root and is written by a person. It names: this repo's address (the script
refuses to run from a checkout whose origin is a different repo), the commit to take, the folder the blocks go
in, the app's central package-versions file, and the blocks the app uses.

**The lock** sits beside the manifest and is written by the script: per block, the commit it was copied from and
a fingerprint of each of its files.

**What a block's files are:** every file under the block's folder except build output (`bin`, `obj`) and editor
folders. Tests live in a separate folder of this repo and are not part of a block. A file added, removed or
renamed counts as a change, as does a change of content. Line endings and a leading byte-order mark never count.

**The three-way rule.** For each block the script compares three versions: the app's copy, the version recorded
in the lock (the base), and the version on the other side.
- Only one side differs from the base → that side's version wins.
- Both sides equal each other → in step, whatever the base says.
- Both sides differ from the base and from each other → changed on both sides; the script refuses.

"This repo's side" is the working tree of the checkout, uncommitted changes included, when sending back; it is
the manifest's commit when taking forward. A commit named in the manifest or the lock that the checkout does not
have stops the run with "fetch first".

```
Flow 1: Take blocks forward into an app
1. A person lists the blocks in the app's manifest and runs the script against the app.
2. The script adds every block those blocks depend on. It checks every block the lock knows, listed or not.
3. Any block changed in the app (by the three-way rule) stops the run, listing its changed files; nothing is
   written. The app sends it back first (Flow 2).
4. The script copies each listed block whole, at the manifest's commit, into the app's blocks folder: adds,
   replaces and removes files so the copy equals this repo's block exactly.
5. It writes the lock and lists, for the copied blocks, every package version the app's central file lacks or
   has at another version, including the pinned ones this repo's central file holds for them.

Flow 2: Send a block back from an app
1. A developer edited a block in the app and runs the script in send-back mode.
2. For each block changed in the app, the script applies the three-way rule against this repo's working tree.
   Changed on both sides stops the run, listing the files on each side; nothing is written.
3. Otherwise it copies the app's block whole into this repo's working tree, without a leading byte-order mark.
4. A person commits it here and sets the app's manifest to that commit; the next forward run finds both sides
   equal, so the block is in step and the lock is refreshed.

Flow 3: Status
1. Per block: in step, changed in the app, changed here, changed on both sides, no longer listed, or not yet
   taken.
```

## Rules

1. A block is copied whole, never in part.
2. A forward copy never overwrites an unsent app edit; a send-back never overwrites a change made here.
3. Line-ending differences and a leading byte-order mark never count as a change; an added, removed or renamed
   file always does.
4. The script writes only inside the named app's blocks folder, its lock, and this repo's blocks folders.
5. The tests of a block stay in this repo; they are never copied into an app.
6. An app may edit its copy of a block; it sends the block back before it takes anything forward.
7. **First take.** A block folder that already exists in the app but has no lock entry is not overwritten unless
   the person says so explicitly, and then only when that folder has no uncommitted changes in the app's
   repository, so every replaced or removed file can be recovered from the app's history. The script lists the
   files it will remove before it does.
8. A block dropped from the manifest keeps its folder and lock entry; status reports it as no longer listed, and
   it still counts for Rule 6. Removing it is a person's act.
9. Every refusal names the block and lists the files that differ.
10. A refusal for "changed on both sides" says the way out: set the app's edit aside, set the app's manifest to
    the commit here that holds the other change, take the block forward, apply the edit again, send it back.
11. A lock the script cannot read, or whose entries disagree with the app (a block entry with no folder, a file
    list that is not a list of the block's files), stops the run before anything is written.

## Acceptance Criteria

- [ ] The solution builds on .NET 10 with zero warnings and no warning suppressions, and every test passes.
- [ ] **Portability check:** for each of the 11 blocks, an automated check copies it with the script, together
      with the blocks it depends on, into an empty folder outside this repo; it adds only a central
      package-versions file built from the script's package report, and a new class library that references the
      block; and it builds that library.
- [ ] No file in the repo starts with a byte-order mark.
- [ ] No block's code or comments contain the words "article", "articles", "journal" or "journals" (any case,
      whole words).
- [ ] No block contains `//insight`, `//talk` or `//todo` markers, or a commented-out line of code.
- [ ] MediatR resolves to 12.5.0; MassTransit to an 8.x release.
- [ ] Error mapper tests: each error type gives its status (400, 401, 403, 404, 409, 499, 500, 502); an unlisted
      HTTP error gives its own status; a cancellation wrapped in another error gives 499; a cancellation after
      the response started leaves the status unchanged; a 500 outside development carries the fixed text and no
      internal message; reply names are camelCase; the "ensure not exists" helper's error gives 409.
- [ ] A FastEndpoints test on the version this repo uses shows a handler for a specific event runs when the event
      is published through the block.
- [ ] A repository test updates an existing entity whose property is stored in a private field, through upsert,
      and reads the new value back from a fresh context.
- [ ] A repository test deletes an entity by id.
- [ ] A claims test asks the HTTP-context claims provider for two different claim types and gets each one's
      values.
- [ ] The table set-up of an audited entity carries no database-specific default.
- [ ] Sync tests on temporary repositories cover Flows 1–3 and Rules 1–11, including both refusals, the first
      take of an existing folder, a file added in the app, build output ignored, line endings and a byte-order
      mark ignored, a block dropped from the manifest, an unreadable lock, and the way out of a "changed on
      both sides" refusal followed to the end.
- [ ] Every block has a read-me; the data-access block's read-me says the two seeding helpers need SQL Server;
      the repo has a read-me that says how to run the sync script, including the way out of a "changed on both
      sides" refusal.
- [ ] `docs/public-names.md` lists every difference between the public and protected surface of this build and
      the reference's blocks built as they are, plus the error reply's wire names; every row of the table below
      appears in it.

## Public names that differ from the reference

The complete list knowledge-gateway and the plugin's skills cite is `docs/public-names.md`, produced by the build
from a comparison of the two surfaces (last criterion above). The table here is what was known when the spec was
written; the generated list may add rows (for example nullable annotations corrected to clear compiler warnings),
never drop one. Every public type of version one may become internal in version two (§ Out of Scope).

| Block | Reference | Version one |
|---|---|---|
| FastEndpoints | namespace `Blocks.FastEnpoints` | `Blocks.FastEndpoints` |
| MediatR | namespace `Blocks.MediatR.Behaviours` (folder `Behaviors`) | `Blocks.MediatR.Behaviors` |
| AspNetCore | `GlobalExceptionMiddleware` in `Blocks.AspNetCore`; `RequestDiagnosticsMiddleware` in `Blocks.AspNetCore.Middleware` (all in folder `Middlewares`) | all in `Blocks.AspNetCore.Middlewares` |
| Core | `AssemblyExtensions`, `RegexExtension`, `TypeExtensions` in `Blocks.Core.Extensions` (8 siblings in `Blocks.Core`) | `Blocks.Core` |
| Core | `IThreadSafeMemoryCache`, `ThreadSafeMemoryCache` in `Blocks.Core` (folder `Cache`, a tie) | `Blocks.Core.Cache` |
| Domain | `IAuditedEntity` in `Blocks.Domain.Entities` (6 siblings in `Blocks.Entities`) | `Blocks.Entities` |
| Domain | `IDomainObject` in `Blocks.Entities` (4 siblings in `Blocks.Domain`) | `Blocks.Domain` |
| Exceptions | `Extensions` (`SingleOrThrow`) in `Blocks.Linq` (4 siblings in `Blocks.Exceptions`) | `Blocks.Exceptions` |
| EntityFrameworkCore | `TransactionalDispatchDomainEventsInterceptor` in `Blocks.EntityFrameworkCore` (folder `Interceptors`) | `Blocks.EntityFrameworkCore.Interceptors` |
| EntityFrameworkCore | protected virtual `AuditedEntityConfiguration<,>.DefaultDateSql` | removed (D2) |
| AspNetCore | `IRouteProvider.GetArticleId()`, `HttpContextProvider.GetArticleId()` | removed — use `GetRouteValue(key)`, which returns text |
| AspNetCore | error reply properties `StatusCode`, `Message`, `TraceId`, `Details`; validation reply adds `Errors` with `PropertyName`, `ErrorMessage` | the same names in camelCase on the wire (D1) |
| Exceptions | — | new `ForbiddenException`, `ConflictException`, `BadGatewayException` |
| Core | `RegexExtension` | `RegexExtensions` |
| Redis | `Repository<T>.Exists` | `ExistsAsync` |
| Redis | `GenerateNewId` (repository and extension overloads) | `GenerateNewIdAsync` |
| Redis | `SetSequenceSeed` | `SetSequenceSeedAsync` |
| Redis | `SeedFromJson` | `SeedFromJsonAsync` |
| EntityFrameworkCore | `TransactionProvider.GetCurrentTransaction` | `GetCurrentTransactionAsync` |
| Hasura | `TrackObjectRelationship`, `TrackArrayRelationship` | `TrackObjectRelationshipAsync`, `TrackArrayRelationshipAsync` |

The rename rows come from the reviewers' sweep; the public-surface comparison (last criterion) adds any the sweep
missed, under the same two rules.

## Answers for knowledge-gateway

- **Conflicts (hand-off question 2):** the reference's shape for 16; D1–D4 above (owner, 2026-10-08). The hand-off
  says 12 conflicts; the comparison lists 20, and all 20 were ruled.
- **sprint-rituals improvements (question 3):** in version one — the upsert fix (D3), the publisher fix (D4), the
  403/409/502 errors and the mapper changes (D1, A1). In version two — the SQLite data-folder helper.
- **Blocks only knowledge-gateway has (question 6):** generic enough to move here — Authentication, Mcp.OAuth,
  security headers, readiness checks (Azure wording removed), the Azure Front Door origin check (named for Front
  Door), Git, Npgsql, Sql, the command-line argument reader. Rate limiting needs extracting from the store
  service first; its daily allowance is product logic and stays. All in version two.
- **Sending a change back (question 7):** Flow 2; a reviewed path is backlog row F2-ReviewedChangeRequests.
- **Supersedes in the hand-off:** an app *may* edit its copy and sends it back (owner, 2026-10-08); there is no
  stamp file in the block — the lock records the commit; MediatR's last free release is 12.5.0 (Apache-2.0) and
  AutoMapper's is 14.0.0 (MIT) — "9.x and later are commercial" is right for MassTransit only.
- **First take in knowledge-gateway:** its four same-named blocks hold files the reference lacks (Rule 7). Move
  those files out, commit, then take the block forward explicitly.
- Repo address: `https://github.com/DotNetLabX/dotnet-building-blocks`; the commit of version one is known once
  the owner pushes.

## Out of Scope

- **Helper types made internal** — version two (owner, 2026-10-08, when choosing the version-one scope); about 150
  public types need a decision each. Until then every public type is provisional.
- **Full test coverage** — version two; version one tests what it changes and the sync script.
- **How a block gets the time, and which id and time types an entity uses** — not settled by the owner; the
  blocks keep the reference's forms.
- **The SQLite data-folder helper from sprint-rituals** — version two.
- **Blocks only knowledge-gateway has, and the AI blocks** — version two, and a later step for the AI blocks.
- **The reference's file and email modules** — not in version one (owner, 2026-10-08).
- **A reviewed change path from apps** — F2-ReviewedChangeRequests.
- **The mapping library and JSON library the Core block carries** — kept as the reference has them; whether Core
  still needs them is a version-two question.
- **Editing the reference, sprint-rituals or knowledge-gateway** — this repo only.

## Handed to the plan

- Where the list of dependent blocks comes from (project references); the script's runtime; the manifest and
  lock file names and formats.
- Whether the reference's unused package references (JWT, OpenAPI, Swagger) are kept.
- Line endings written by a forward copy.
