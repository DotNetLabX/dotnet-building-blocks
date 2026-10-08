# Critic record — F1-VersionOne

## Round 1 — 2026-10-08 — spec — primary (codex)

Header note: the primary is Codex; its verdict (NO-GO, 7 HIGH, 9 MEDIUM, 2 LOW) is persisted verbatim at
`docs/specs/F1-VersionOne/delivery/review-critic-codex.md`, which is this round's primary reader section.
Reader pair: codex + sonnet, picked by the owner (`reader-pair.md`). The second reader's first message was cut
off by the platform after C1; the section below is its full re-send, on request.

## Second reader — round 1 (sonnet)

F1-VersionOne spec review, second reader (round 1, mode: spec review, depth: mechanism): findings C1 to C15 in full. Verdict: REVISE. 2 HIGH (C1, C2), no CRITICAL, so the adversarial re-sweep is not triggered. Ruler: seed only (docs/critic-calibration/ruler.md absent).

Every reference claim in the spec checks out against source, with corrections in C7, C12 and C13. Verified: 11 blocks; the 3 product-wording hits (HttpContextProvider.cs:12,48-49, AssignUserIdPreProcessor.cs:11,14, Redis/Repository.cs:45); the 3 namespace rows (Blocks.FastEnpoints AssignUserIdPreProcessor.cs:5, Blocks.MediatR.Behaviours in 3 files, Blocks.AspNetCore.Middleware RequestDiagnosticsMiddleware.cs:9); the A2 claims bug (HttpContextProvider.cs:24-25); GETUTCDATE (AuditedEntityConfiguration.cs:22); D3 (Repository.cs:55); D4 (FastEndpoints/DomainEventPublisher.cs:9). The reference's own central file has MediatR 13.0.0, AutoMapper 15.0.1 and MassTransit.RabbitMQ 8.5.2. Not opened: sprint-rituals source, conventions-ballot, reference-model, and the bodies of the Hasura, Redis, Messaging and Core Extensions/Cache/GraphQL files. I did not verify licence terms externally.

Coverage ledger (from the first message): Round: 1 · Depth: mechanism · Ruler: seed only · Read: full. COVERED: 11 blocks and Articles.* exclusion; product wording; A2; D2; D3; D4; sync flows, rules and the empty-project criterion; hand-off Q1–Q7; 20 conflicts; reference conventions; convention compliance counts; sibling specs. PARTIAL: namespaces and public names (C12); D1 (C10); licence lines (not verified externally). NOT OPENED: bodies of Hasura, Redis, Messaging, Core Extensions/Cache/GraphQL, ModelBinding, RequestContext/RequestDiagnostics, Domain entity bodies (grep and csproj only); sprint-rituals source; conventions-ballot.md and reference-model.md (not briefed).

[HIGH] C1: The send-back-then-take-forward cycle cannot finish as written
Anchor: seed-0007 · above seed-0022 · below seed-0002
Shape: self-contradicting flow
Consequence: The documented send-back-then-take-forward cycle stops at its last step, because the sent block still differs from the stored baseline.
Evidence (quoted): Flow 2 step 4: "A person commits it here; the app then takes it forward again (Flow 1), which refreshes its lock." Flow 1 step 3: "compares the app's copy with the lock. A block changed in the app stops the run". Flow 2 does not touch the lock, so the sent block still differs from it.
Surfaces: spec § Flows 1 and 2; ideas.md § I
Issue: Read literally, the happy path of the owner's two-way design refuses at its last step. Two builds are plausible: (a) an app copy equal to the manifest-commit block counts as "in step"; (b) Flow 2 writes a "sent" marker into the lock. Nothing picks one.
Impact: The required sync test for send-back, commit, forward forces the builder to invent the lock semantics, and a wrong pick can overwrite later edits.
Fix: State that the forward comparison is three-way, so an app copy equal to the target block counts as in step. Say what the lock then records.

[HIGH] C2: A folder with no lock entry has no defined behaviour, and knowledge-gateway starts in that state
Anchor: seed-0007 · above seed-0022 · below seed-0003
Shape: undefined first-run state
Consequence: A first run over a folder that already exists but has no stored baseline can delete files the app still uses.
Evidence (scenario): Knowledge-gateway already holds same-named blocks with files the reference lacks: Blocks.Core/Cli/CliArgs.cs, and in Blocks.AspNetCore the DeploymentReadiness, FrontDoorOriginGuardMiddleware and SecurityHeadersMiddleware files (comparison § 1.1, § 1.2, § 6). Its Curation project, StoreAssistant and KnowledgeGateway.API use them (§ 7). Flow 1 step 3 compares "the app's copy with the lock" and none exists; step 4 "removes files so the copy equals this repo's block exactly".
Surfaces: spec § Flow 1 steps 3-4, Rule 2; comparison § 6-7
Untouchable: data-loss
Issue: Rule 2 protects an "unsent edit", which presumes a lock baseline. An unlocked existing folder is by definition unsent, so the safe reading is "refuse". Then the first consumer has no documented way in. The spec also gives no behaviour for a block dropped from the manifest, or for a dependency block already present unlocked.
Impact: The unsafe build deletes uncommitted app files. The safe build blocks the adoption the spec exists for.
Fix: Pick one rule and write it down. For example: an unlocked existing folder refuses, and a named adopt path (remove the folder first, or an explicit flag) is documented. State what happens to a block dropped from the manifest.

[MEDIUM] C3: "Changed on both sides" has no exit
Anchor: seed-0022 · above seed-0310 · below seed-0007
Shape: dead-end refusal
Consequence: When both sides changed, both directions refuse and the text gives no way out, so the user improvises by hand-editing the baseline.
Evidence (scenario): App A edits Core. App B's change lands here. A's send-back refuses ("changed here too") and A's forward run refuses ("changed in the app"). Hand-merging does not help, because the lock commit stays old, so the send-back still refuses.
Surfaces: spec § Flow 1 step 3, Flow 2 step 2, Flow 3
Issue: A VCS-level exit exists but is nowhere stated: revert the app block, run Flow 1, re-apply the edit, run Flow 2. This is the idea that motivated the lock ("the second could overwrite the first").
Fix: Require the script's refusal message or the repo read-me to state the exit.

[MEDIUM] C4: What counts as a block's files is undefined
Anchor: seed-0022 · above seed-0310 · below seed-0007
Shape: undefined unit of comparison
Consequence: Build output or test folders inside a block can make every block look edited, so every forward run refuses.
Evidence (scenario): The app builds its copy, which creates bin/ and obj/ inside the block folder, and "a fingerprint of every copied file" plus "changed in the app" then fires. Rule 5 ("tests ... never copied into an app") conflicts with Rule 1 ("copied whole") if tests sit inside the block folder. The spec names no repo layout.
Surfaces: spec § Rules 1, 3, 5; § Flow 1 step 4
Fix: Define the block as its source files. Exclude build output and tests, and state where tests live relative to the block. Say whether a byte-order mark introduced by an app editor counts as a change.

[MEDIUM] C5: "Changed here" has no stated reference point
Anchor: seed-0022 · above seed-0310 · below seed-0007
Shape: ambiguous baseline
Consequence: An uncommitted change here may or may not count as changed here, so a send-back can overwrite it or refuse, depending on the build.
Evidence (quoted): Flow 2 step 2: "checks this repo's block against the lock's commit". Flow 3: "changed here". Flow 1 step 4: "at the manifest's commit". Interpretation A compares the working tree, so an uncommitted send-back from app 1 makes app 2 refuse. Interpretation B compares HEAD, so app 2 overwrites app 1's uncommitted send-back, which breaks Rule 2.
Surfaces: spec § Flow 2 steps 2-3, Flow 3, Rule 2
Fix: Say "working tree versus the lock's commit". Say what happens when the commit named in the manifest or lock is absent from the checkout. Define the role of the manifest's "this repo's address" for a script that runs from a checkout.

[MEDIUM] C6: The package-version report lacks inputs, and the empty-project criterion is not tied to an automated check
Anchor: seed-0022 · above seed-0310 · below seed-0007
Shape: missing input and unverifiable criterion
Consequence: The report of missing package versions cannot be produced from the stated inputs, and a partial list leaves the copied block unbuildable.
Evidence (quoted): The manifest holds "this repo's address, the commit to take, the folder the blocks go in, the blocks it uses", with no path to the app's package-versions file. Flow 1 step 5 compares against that file. Criterion 2 says "only the listed package versions". The reference uses CentralPackageTransitivePinningEnabled (Directory.Packages.props), so a list of direct references only would omit pins, and a missing pin gives a downgrade error or a vulnerability warning on restore.
Surfaces: spec § What version one contains item 2; § Flow 1 step 5; § Acceptance Criteria 2
Issue: Three open points: where the app's central file is, whether the list includes transitive pins, and whether criterion 2 is an automated dotnet build test (network restore) or a manual check.
Fix: Add the file location to the manifest, define the list as everything needed to build the copied blocks including pins, and name the check.

[MEDIUM] C7: The EF Core block stays SQL Server-only in two more places, contradicting D2's own rationale
Anchor: seed-0022 · above seed-0041 · below seed-0008
Band: MEDIUM-HIGH; driver: whether the owner's "works with any database" ruling covers the seeding helpers
Shape: principle applied to one instance
Consequence: The data-access block stays tied to one database vendor in two helpers while the stated rule says it works with any.
Evidence (quoted): D2's reason: "a block must work with any database its technology supports"; the hand-off says the same. Source: ManualGenerateIdScope.cs:24,33 runs SET IDENTITY_INSERT ..., with a default schema of "dbo" at :19. DbContextExtensions.Seed.cs:90 runs DBCC CHECKIDENT(...) inside TryReseedTable, which swallows the failure with Console.WriteLine. UseManualGenerateId is used in the reference (Auth.Persistence/Data/Test/Seed.cs:24).
Surfaces: spec § Departures D2; § Acceptance Criteria (table set-up bullet); the reference EF Core Seeding/ and Extensions/
Issue: Criterion 9 only checks "no database-specific default". The plan will keep both helpers as the reference has them. Options are remove them, make them provider-neutral, or declare them SQL Server-only in the read-me. Nothing picks one.
Fix: Owner and architect decide and record it. See C12 for DefaultDateSql.

[MEDIUM] C8: The "internals stay internal" ruling is deferred with no owner attribution
Anchor: seed-0021 · above seed-0041 · below seed-0004
Band: MEDIUM-HIGH; driver: whether the owner ruled the deferral in session
Shape: binding ruling dropped
Consequence: Shipping everything public now makes every public type a promise, so making helpers internal later breaks every app that adopted version one.
Evidence (quoted): Hand-off: "a block keeps its helper types internal, because everything public in a block is a promise to every app that copies it", one of three rulings "ruled by the owner 2026-10-08". Spec § Out of Scope: "Helper types made internal - version two; each of about 200 public types needs a decision." There is no owner attribution, unlike the neighbouring bullets. I counted about 151 top-level public type declarations in Blocks.*, not about 200.
Surfaces: spec § Out of Scope; hand-off "WHAT THE OWNER HAS SETTLED"; reference-conventions.md Y2
Issue: The deferral may be sensible, but it reverses a binding hand-off item. The spec also never says that v1's public surface is provisional, and knowledge-gateway adopts it at once.
Fix: Record who decided. State in the names section that v1 public types may become internal in v2, or mark which ones.

[MEDIUM] C9: "Follows the settled conventions" is narrowed to five items, and the hand-off said all of them bind blocks
Anchor: seed-0022 · above seed-0041 · below seed-0007
Shape: ambiguous scope
Consequence: Two builders read the convention scope differently, so some public methods get renamed and others do not, and the names list drifts.
Evidence (quoted): Spec item 5: "Follows the settled conventions that can be applied without a design decision: no course-note comment markers, no to-do comments, ...". Item 7: "The changes below, and only these, depart". Hand-off: "The code in the blocks follows the conventions settled". If X4 (every async method ends in Async) applies, it renames the public methods TransactionProvider.GetCurrentTransaction (:9), HasuraMetadataService.TrackObjectRelationship (:114) and TrackArrayRelationship (:133).
Surfaces: spec § What version one contains items 5 and 7; hand-off; reference-conventions.md X4, X3, B7, A28
Fix: Say whether the colon list is exhaustive. If X4 applies, list those three renames in the names table. If not, say the other rows wait for v2.

[MEDIUM] C10: D1 adds a behaviour change the owner's drawing did not show
Anchor: seed-0021 · above seed-0041 · below seed-0007
Shape: unratified scope addition
Consequence: One error-mapper behaviour change was never shown to the owner, so it ships under a ruling that did not include it.
Evidence (quoted): Spec D1: "a cancelled request is 499 also when the cancellation is the inner cause". conflicts.drawio item 12 lists four changes: 403/409/502, other HTTP errors use their own status, a 500 hides the message outside development, camelCase. The inner-cause 499 appears only in the sprint-rituals shape (comparison § 2 and § 4 item 12), and the "Why" column gives no reason for it. The reference guards the 499 with !Response.HasStarted (GlobalExceptionMiddleware.cs:36-40); the spec does not say whether that guard stays.
Surfaces: spec § Departures D1; conflicts.drawio item 12; comparison § 4 item 12
Fix: Confirm with the owner or drop the clause. State whether the HasStarted guard stays. State what a 500 outside development shows instead of the message.

[MEDIUM] C11: The claims fix (A2) has no acceptance criterion, and the spec says it tests what it changes
Anchor: seed-0021 · above seed-0310 · below seed-0007
Shape: change without a verification
Consequence: A fix to the claim lookup that feeds role checks has no test, though the document promises to test what it changes.
Evidence (quoted): Spec § Out of Scope: "version one tests the parts it changes and the sync script". The criteria cover D1, D2, D3, D4 and the sync tests, but nothing for A2. The item-5 clean-up (markers, to-dos, commented-out code, member docs) also has no check. Source: HttpContextProvider.cs:24-25 returns GetClaimValues(ClaimTypes.Role) for any name, and GetUserRoles() calls it at :38.
Surfaces: spec § A2; § Acceptance Criteria; § Out of Scope
Fix: Add an A2 criterion that asks for two different claim types and expects the right values. Add one grep-style check for the item-5 clean-up. The spec's "user-claims provider" matches no single type: IUserClaimsProvider is an unrelated interface with a UserRole property, and the bug is in HttpContextProvider, which implements IClaimsProvider. Name it.

[LOW] C12: The names table omits known rows, and its "two namespaces" count is inaccurate
Anchor: seed-0310 · above none (LOW is the floor) · below seed-0021
Band: LOW-MEDIUM; driver: whether the "public surface" comparison covers protected members and the JSON reply shape
Shape: incomplete list
Consequence: Consumers cite a names list that omits a removed overridable member and the changed reply field names, so their code fails to compile or parse.
Evidence (quoted): Missing rows: protected virtual string DefaultDateSql (AuditedEntityConfiguration.cs:22) goes away under D2, and the reply properties StatusCode, Message, TraceId and Details become camelCase under D1 (a wire change). Spec item 3 says "the two namespaces that disagree with their folder". The reference has more: GlobalExceptionMiddleware lives in Middlewares/ but sits in Blocks.AspNetCore (:11, while its siblings sit in .Middlewares and .Middleware); Hasura Sql/ uses Blocks.Hasura.Query; TransactionalDispatchDomainEventsInterceptor sits in Blocks.EntityFrameworkCore while its sibling uses .Interceptors. After the fix an app using the middlewares needs two using lines.
Surfaces: spec § Public names that differ; § Acceptance Criteria (last); reference namespace sweep
Fix: Add the rows, or say the list is scoped to spelling only. Say whether protected members and wire names are in the comparison.

[LOW] C13: Three criteria cannot be run as written
Anchor: seed-0310 · above none · below seed-0021
Shape: unverifiable wording
Consequence: Three checks cannot be run as written because they rely on a baseline or a name nobody states.
Evidence (quoted): Criterion 1: "no warnings introduced by this feature" (in a new repo everything is introduced by this feature). Criterion 4: "the reference's product name" (not stated in the spec). The reference's IRouteProvider.GetRouteValue returns string but the implementation returns string?, so a warning baseline is real.
Surfaces: spec § Acceptance Criteria 1, 4, 11
Fix: Use "zero warnings, no suppressions" (convention A33), state the product name, and define the word match.

[LOW] C14: Plan must resolve
Anchor: seed-0364 · above none · below seed-0021
Shape: mechanism unresolved at spec depth
Consequence: The plan must pick several mechanism details the spec leaves open, and a wrong pick changes the shipped script or the package list.
Evidence (path and quoted)
Surfaces: spec § Flows; reference csproj files
Route: plan must resolve
- The source of "every block those blocks depend on". ideas.md § A said project references; the spec dropped that.
- The manifest and lock file names and formats, and the script's runtime, since tests run "on temporary repositories".
- The reference's csproj files list packages the code never uses. Blocks.AspNetCore lists JwtBearer, OpenApi and Swashbuckle with no use in code (grep for Swagger|OpenApi|JwtBearer finds nothing). Blocks.FastEndpoints lists FastEndpoints.Swagger the same way. Dropping them would depart from the reference; keeping them pushes unused packages into every app's central file. Whether Swashbuckle 9.x resolves cleanly beside Microsoft.AspNetCore.OpenApi on .NET 10 is unverified.
- Line endings written by a forward copy, and whether a send-back strips a byte-order mark an editor added (criterion 3 forbids it in this repo).

[LOW] C15: Two knowledge-gateway-facing statements are not reconciled with the hand-off
Anchor: seed-0041 · above none · below seed-0021
Shape: unreconciled upstream statement
Consequence: Readers of the earlier hand-off still believe copies are never edited in an app, so their rules contradict the new flow.
Evidence (quoted): Hand-off: "a copy is never edited in an app" and "each stamped with the commit". Rule 6 allows app edits. ideas.md § C dropped the stamp file. The hand-off's "12 conflicts" is 20 in comparison § 4 and the drawing.
Surfaces: spec § Answers for knowledge-gateway; hand-off; ideas.md § C, I
Fix: Add a one-line "supersedes" note in § Answers for knowledge-gateway for both points. Answer hand-off Q3 (which sprint-rituals improvements come in) explicitly; it is only implicit in D1, D3, D4 and the SQLite out-of-scope line.

Gap analysis
- Licence lines: the spec agrees with the hand-off. The reference is already past them: its central file has MediatR 13.0.0 and AutoMapper 15.0.1 (no block references AutoMapper), so v1 is a MediatR downgrade. MassTransit.RabbitMQ 8.5.2 fits "8.x". The test project is not covered by "no other commercial package" (assertion or mocking libraries).
- X2 (PascalCase record members) is moot: Blocks.* has no positional records, so dropping it loses nothing.
- Core ships Blocks.Mapster helpers and an ApplicationDbContext that uses Mapster; Y12 says to use the mapping library only where a block needs it. The spec is silent on whether that is accepted. Core also pulls Newtonsoft.Json and Mapster into every app.
- The GetArticleId() removal breaks Articles.Security/ArticleAccessAuthorizationHandler.cs:26 in the reference app (out of this repo's scope), and the replacement GetRouteValue(key) returns string, not int?.

Open questions
- No licence file or licence choice is mentioned for a public repo whose purpose is to be copied (the reference has a LICENSE). Owner call. Confidence: low on whether it is in scope.
- Repository.cs:61-69 DeleteByIdAsync uses ExecuteSqlInterpolatedAsync($"DELETE FROM {TableName} WHERE Id = {id}"). As I understand EF Core, interpolated values become SQL parameters, so the table name would too and the call would fail. No reference app calls it. Not run. Confidence: medium. The spec's "known bugs fixed" list would then be missing a bug.

Reviewed: D:\src\dotnet-building-blocks\docs\specs\F1-VersionOne\definition\spec.md

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0007 | Send-back then forward cannot finish | self-contradicting flow | the two-way cycle refuses at its last step | spec Flow 1 step 3 vs Flow 2 step 4 | | sync design | spec § Flows | codex 1, C1 |
| R1-02 | codex, sonnet | HIGH | seed-0007 | No rule for a first import, an unlocked folder, a bad lock or a missing commit | undefined first-run state | knowledge-gateway's existing blocks could be overwritten or never adoptable | KG Blocks.Core/Cli, Blocks.AspNetCore extras (comparison § 6–7) | data-loss | sync design | spec § Flow 1, Rule 2 | codex 2, C2, C5 (missing commit) |
| R1-03 | codex, sonnet | HIGH | seed-0007 | File set of a block undefined | undefined unit of comparison | an added file is deleted unseen; build output makes every block look edited | contested: HIGH / MEDIUM — settled HIGH: an unseen added file deleted is data loss | data-loss | sync design | spec § Rules 1, 3, 5 | codex 3, C4 |
| R1-04 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Forward check misses blocks no longer listed | scope gap | an edited, delisted block escapes the "send back first" rule | spec Flow 1 step 2 vs Rule 6 | | sync design | spec § Flow 1, Rule 6 | codex 4 |
| R1-05 | codex, sonnet | MEDIUM | seed-0021 | Helper-types-internal deferral has no owner attribution | binding ruling dropped | the deferral reads as the architect overriding a binding rule | contested: HIGH / MEDIUM — settled MEDIUM: the owner ruled the deferral in this session (version-one question, 2026-10-08); only the record is missing | | record | spec § Out of Scope | codex 5, C8 |
| R1-06 | codex, sonnet | HIGH | seed-0022 | Convention scope contradictory | ambiguous scope | builders disagree on which public methods are renamed | X4, A35, A8/Y20 cases in the reference; spec items 5 and 7 | | conventions | spec § contents | codex 6, C9 — contested: HIGH / MEDIUM — settled HIGH: it decides public names, which are a promise to every app |
| R1-07 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | "Already exists" helper still answers 400 | principle applied to one instance | the owner's 409 ruling is not met by the block's own helper | RepositoryExtensions.cs:37–42 throws BadRequestException | | error mapper | spec § D1 | codex 7 |
| R1-08 | codex, sonnet | MEDIUM | seed-0310 | Public-names table incomplete | incomplete list | consumers cite a list missing renames, a removed override and the reply wire casing | Async renames, RegexExtension, DefaultDateSql, reply property casing, more namespace mismatches | | names | spec § Public names | codex 8, C12 — contested: MEDIUM / LOW — settled MEDIUM: the list is a promised deliverable |
| R1-09 | codex, sonnet | MEDIUM | seed-0022 | Portability check not isolated; package report lacks inputs | unverifiable criterion | a copied block can pass the check yet not build in an app | manifest has no package-file path; transitive pinning | | portability | spec § AC 2, Flow 1 step 5 | codex 9, C6 |
| R1-10 | codex, sonnet | MEDIUM | seed-0022 | Commit and "changed here" baseline undefined | ambiguous baseline | a send-back can overwrite an uncommitted change here, or refuse wrongly | Flow 2 step 2, Flow 3 | | sync design | spec § Flows 2–3 | codex 10, C5 |
| R1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Upsert test can pass without the fix | weak test | the bug fix ships untested | an insert path saves field-backed values already | | tests | spec § AC | codex 11 |
| R1-12 | codex, sonnet | MEDIUM | seed-0021 | No criterion for the claims fix or wrapped cancellation; wrong type named | change without a verification | a role-check fix ships untested | HttpContextProvider.cs:24–25 | | tests | spec § A2, AC | codex 12, C11 |
| R1-13 | codex, sonnet | MEDIUM | seed-0021 | 499 after response start; inner-cause 499 not shown to the owner | unratified scope addition | an impossible status change after headers, or an unruled behaviour | GlobalExceptionMiddleware.cs:36–40 HasStarted guard | | error mapper | spec § D1 | codex 13, C10 |
| R1-14 | codex, sonnet | MEDIUM | seed-0022 | Two seeding helpers stay SQL Server-only | principle applied to one instance | the "any database" reason is only half applied | ManualGenerateIdScope.cs:24,33; DbContextExtensions.Seed.cs:90 | | portability | spec § D2 | codex 14, C7 |
| R1-15 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Refusals do not list the differing files | dropped requirement | the owner's picked idea B loses its diagnostics | ideas.md § B | | sync design | spec § Flows | codex 15 |
| R1-16 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | D4 states the reference bug as proven | overstated evidence | the claim depends on the FastEndpoints version | comparison § 2 row "DomainEventPublisher": not run | | evidence | spec § D4 | codex 16 |
| R1-17 | sonnet | MEDIUM | seed-0022 | "Changed on both sides" has no exit | dead-end refusal | users hand-edit the lock | scenario App A / App B | | sync design | spec § Flows | C3 |
| R1-18 | sonnet | LOW | seed-0310 | Three criteria cannot be run as written | unverifiable wording | checks rely on unstated baselines | AC 1, 4, 11 | | tests | spec § AC | C13 |
| R1-19 | sonnet | LOW | seed-0364 | Mechanism details for the plan | mechanism unresolved at spec depth | wrong picks change the script or the package list | dependency source, file names, unused packages, line endings | | plan | spec § Flows | C14 |
| R1-20 | sonnet | LOW | seed-0041 | Hand-off statements not reconciled; Q3 implicit | unreconciled upstream statement | knowledge-gateway keeps believing copies are read-only | hand-off lines; ideas.md § C, I | | record | spec § Answers | C15 |
| R1-21 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Drawing says 8 validation helpers, source has 7 | wrong count | a small factual error in the owner's picture | Blocks.Core/FluentValidation/Extensions.cs | | evidence | conflicts.drawio k11 | codex 17 |
| R1-22 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Hand-off's licence line wrong for MediatR and AutoMapper | wrong upstream fact | knowledge-gateway may pin the wrong versions | MediatR 12.5.0 Apache-2.0; AutoMapper 14.0.0 MIT | | record | hand-off line 47 | codex 18 |
| R1-23 | sonnet | MEDIUM | seed-0022 | Delete-by-id passes the table name as a SQL parameter | known bug kept | the helper fails on every call | Repository.cs:61–69 ExecuteSqlInterpolatedAsync with {TableName}; graded at merge from an unranked open question: a kept public helper that cannot work | | bug | spec § A-list | C open question 2 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-08)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | | spec § Sync — the three-way rule, Flows 1–2 |
| R1-02 | fixed-real | HIGH | | spec § Sync — first take, Rules 7–8 |
| R1-03 | fixed-real | HIGH | | spec § Sync — what a block's files are, Rule 3 |
| R1-04 | fixed-real | HIGH | | spec § Flow 1 step 2, Rule 6 |
| R1-05 | fixed-real | MEDIUM | | spec § Out of Scope (owner attribution), § Public names (provisional note) |
| R1-06 | fixed-real | HIGH | | owner ruled 2026-10-08 (renames apply in version one); spec § contents item 5, § Public names rows |
| R1-07 | fixed-real | HIGH | | spec § Departures D1 |
| R1-08 | fixed-real | MEDIUM | | spec § Public names (rows added, renames included) |
| R1-09 | fixed-real | MEDIUM | | spec § Sync — manifest; § AC portability |
| R1-10 | fixed-real | MEDIUM | | spec § Sync — the three-way rule |
| R1-11 | fixed-real | MEDIUM | | spec § AC |
| R1-12 | fixed-real | MEDIUM | | spec § A2, § AC |
| R1-13 | fixed-real | MEDIUM | | spec § D1 (the guard stays; the inner-cause case is disclosed to the owner as a fix) |
| R1-14 | fixed-real | MEDIUM | | owner ruled 2026-10-08 (keep, marked SQL Server only); spec § D2, § AC read-me |
| R1-15 | fixed-real | MEDIUM | | spec § Rules 9 |
| R1-16 | fixed-real | MEDIUM | | spec § D4 |
| R1-17 | fixed-real | MEDIUM | | spec § Rules 10, read-me criterion |
| R1-18 | fixed-real | LOW | | spec § AC |
| R1-19 | plan-must-resolve | LOW | | handed to the plan (listed in spec § Handed to the plan) |
| R1-20 | fixed-real | LOW | | spec § Answers for knowledge-gateway |
| R1-21 | fixed-real | LOW | | conflicts.drawio k11 |
| R1-22 | fixed-real | LOW | | spec § Answers for knowledge-gateway (the hand-off itself is read-only from here) |
| R1-23 | fixed-real | MEDIUM | | spec § A-list A4, § AC |

#### Handed to the plan
- Where the list of dependent blocks comes from (project references); the script's runtime; the manifest and lock file names and formats.
- Whether the reference's unused package references are kept.
- Line endings written by a forward copy.

Reconciled: spec § Sync (manifest, three-way rule, flows, rules) against § Acceptance Criteria and ideas.md § A, B, D, I; § Departures D1 against conflicts.drawio item 12 and § Public names; § Out of Scope against § Public names (provisional note).

## Round 2 — 2026-10-08 — spec — primary (codex)

Header note: delta read, floor MEDIUM (`Read: delta` — round 1's `Coverage:` is `complete` and slug number 1 is not
divisible by 5). Primary only; the second reader joins round 1 alone. Codex's verdict (NO-GO, 3 MEDIUM) is persisted
at `review-critic-codex.md` under its second `## Verdict:` section, the primary reader section for this round.
One fold-time correction made during the round, outside Codex's read: the rename rows for `GenerateNewId`,
`SetSequenceSeed` and `SeedFromJson` were filed under the EF Core block; a plan-time grep shows they are Redis
(`Blocks.Redis/Extensions.cs:21,24,28`, `Blocks.Redis/Repository.cs:52-53`) — fixed in the names table.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | A malformed or inconsistent lock has no rule | undefined state | the script may write over an app on a lock it cannot trust | Rule 7 covers only an absent entry | | round-1 miss: spec § Rules | spec § Rules | codex r2 R1-02 partial |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Naming renames sit outside the "only these" list | contradiction | builders read the permitted API changes two ways | spec items 5 and 7 | | fold | spec § contents | codex r2 R1-06 partial |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | The "both sides changed" way out repeats the refusal | dead-end refusal | following the exit refuses again unless the manifest moves | Rule 10 omits the manifest step | | fold | spec § Rules | codex r2 R1-17 partial |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-08)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | MEDIUM | | spec § Rules 11, § AC sync tests |
| R2-02 | fixed-real | MEDIUM | | spec § contents item 7 |
| R2-03 | fixed-real | MEDIUM | | spec § Rules 10, § AC sync tests |

Reconciled: § Rules 10–11 against Flows 1–2 and the sync-tests criterion; item 7 against items 3–5 and § Public names.
Schedule: round 2's merge holds no CRITICAL, so no round 3 runs. The spec goes Ready.

## Round 1 — 2026-10-08 — plan — primary (codex)

Header note: plan review, round 1. The primary is Codex; its verdict (NO-GO: 1 CRITICAL, 6 HIGH, 10 MEDIUM, 2 LOW) is
persisted verbatim at `review-critic-codex.md` under `# Plan review`, this round's primary reader section. Reader
pair: codex + sonnet (the feature's pick). The second reader's result was truncated by the platform twice; the
section below is assembled verbatim from its header message (header, predictions, ledger), its full F1 re-send, and
its five chunked messages (F2–F13, gap analysis, open questions).

## Second reader — round 1 (sonnet)

Mode: Plan Review. Verdict: REVISE — 2 HIGH, 0 CRITICAL, so the round-1 adversarial re-sweep is not triggered. Built nothing inside the reference, sprint-rituals or knowledge-gateway; all trial builds ran on copies of the reference's tracked files in its scratchpad.

Pre-commitment predictions: rename table incomplete against the spec's namespace rule — found (F2); accept greps weaker than the criteria — found (F6); sync-tool contract holes where it defers to "as the spec states" — found (F8, F9); .NET 10 version and package decisions drift — found, worse than predicted (F1, F3); plan-time facts wrong — mostly not found (counts, line numbers, dependency table, marker counts and sprint-rituals paths correct; one scope slip, F12).

Coverage ledger — Round: 1 · Depth: mechanism · Ruler: seed only · Read: full. PARTIAL: items 1–2 and 6 (F1, F3, F6); item 3 namespaces (F2); item 5 (F6); item 7 and the public-names criterion (F4, F5); D1 (F7); sync script (F8, F9); portability (F10); read-me criterion and build slices (F11); Must-NOT-Change (F4, F5). COVERED: item 4 (gate weak, F6); D2, D3, D4, A1, A2, A4; skill mapping. NOT OPENED: sprint-rituals source other than the three cited files and its central package file; knowledge-gateway docs other than reference-conventions.md and coding-conventions.md § Transitive pin; bodies of reference Hasura, Redis, Messaging and most Core files (grep and build only); ideas.md, conflicts.drawio, prior spec-round review bodies; `dotnet test` with xunit.v3 on SDK 10.

### [HIGH] F1 — Step 2's "zero warnings" cannot be met under its own "no other edit" rule
**Anchor:** seed-0120 · above seed-0130 · below seed-0189
**Band:** HIGH–MEDIUM; driver: whether the builder may change signatures or null handling in step 2
**Shape:** acceptance contradicts a plan constraint
**Consequence:** A mandatory zero-warning check fails in files the step forbids editing, so the builder must change signatures, suppress, or skip the check.
**Evidence:** scenario, measured
- I built the 11 blocks as copied from the reference, with the same csproj and package file, on net9. It gives 22 nullable warnings (CS8600/8601/8603/8604/8619) in 8 files:
  - Core: `ThreadSafeMemoryCache.cs` (8), `EnumExtensions.cs` (3), `ReflectionExtensions.cs` (3), `ObjectExtensions.cs` (2), `JsonExtensions.cs` (1).
  - AspNetCore: `HttpContextProvider.cs` (2), `GenericModelBinderProvider.cs` (1).
  - EFCore: `TenantRepositoryBase.cs` (2).
- The net10 trial (EF 10.0.8, FastEndpoints 8.1.0, MediatR 12.5.0, MassTransit 8.5.10, the three packages dropped) compiles with 0 errors. It adds `CS0618` at `EntityTypeBuilderExtensions.cs:35` and `EF1002` twice at `ManualGenerateIdScope.cs:24,33`. These are the SQL Server helpers the spec says stay.
- Step 1 sets `TreatWarningsAsErrors`.
- Step 2 says "No other edit in this step", with accept "zero warnings".
- Step 4 is "comments only".
- Spec item 7: "only these, change the reference's shapes or behaviour".
- The A4 fix as worded ("escaped identifier via the model") would also trip EF1002 if it builds SQL by interpolation.

**Surfaces:** spec § AC 1 and item 7; plan § Steps 1, 2, 4, 6
**Issue:** The nullable warnings are not caused by the version move. No step owns fixing them. The fixes either change public annotations (a shape change the spec did not list) or use `#nullable`/`NoWarn`/pragma (forbidden by AC 1).
**Impact:** Step 2 stops at its own accept line, and the baseline taken at its end hides whatever was done to get past it.
**Fix:**
- Add an explicit, listed "warning fixes" allowance. It should say whether nullable annotation changes are permitted, require SurfaceDump to print annotations, and require every fix to be recorded in `implementation.md`.
- Name a no-suppression route for EF1002, and one for A4. `ExecuteDeleteAsync` is one option, though it applies query filters.

### [HIGH] F2 — Spec item 3's namespace rule is applied to only two extra files, and its fix direction is unresolved
**Anchor:** seed-0117 · above seed-0130 · below seed-0169
**Band:** HIGH–MEDIUM; driver: whether the owner meant "every namespace" literally
**Shape:** spec requirement with no plan step or gate
**Consequence:** A stated naming rule covers only some of its files, so mixed namespaces ship and the public-names list looks complete when it is not.
**Evidence:** quoted, with a per-file scan of the reference. Spec item 3: "every namespace that disagrees with the other files in its own folder". The plan's own rule, in its Decisions table: "each sits in a folder whose siblings use the folder namespace". Applying that rule to every folder gives these additional cases:
- `Blocks.Core/Extensions`: Assembly, Regex, Type → `Blocks.Core.Extensions` (3); the other 8 → `Blocks.Core`.
- `Blocks.Core/Cache`: `ICacheable`, `MemoryCacheExtensions` → `Blocks.Core.Cache`; `IThreadSafeMemoryCache`, `ThreadSafeMemoryCache` → `Blocks.Core` (a 2–2 tie).
- `Blocks.Domain/Entities`: `IAuditedEntity` → `Blocks.Domain.Entities`; the other 6 → `Blocks.Entities`.
- `Blocks.Domain` (root): `IDomainObject` → `Blocks.Entities`; the other 4 → `Blocks.Domain`.
- `Blocks.Exceptions` (root): `Extensions.cs` → `Blocks.Linq`; the other 4 → `Blocks.Exceptions`.
**Surfaces:** spec item 3 and § Public names; plan § Step 3 and Decisions
**Issue:** The table was not extended by the same scan, and step 3's accept greps only the three named patterns. The step-12 surface diff cannot reveal this, because unchanged namespaces produce no diff. The direction is a real choice. Majority means `Blocks.Core` and `Blocks.Entities`. Folder path means `Blocks.Domain.Entities` for 6 or more public types.
**Impact:** Item 3 is silently unmet, or the owner finds out later that v1 contains an unlisted public rename.
**Fix:** Run the folder scan at plan time and paste its output into step 3. Get an owner ruling on the direction, add the rows to the table, and add an accept: a script asserting that every file's namespace equals its folder's.

### [MEDIUM] F3 — "Transitive pinning off" drops the reference's pins and changes resolved runtime versions
**Anchor:** seed-0130 · above seed-0127 · below seed-0117
**Band:** MEDIUM–HIGH; driver: whether apps rely on protobuf-net 3.x semantics (owner knowledge)
**Shape:** plan decision contradicts spec clause and plan's own rule
**Consequence:** Dropping the pins silently swaps a serialization library to an older major than the source ran on, and no check notices.
**Evidence:** path and restore result.
- The reference central file has `CentralPackageTransitivePinningEnabled` true. It pins `protobuf-net 3.2.56`, `protobuf-net.Core 3.2.56` and `System.ServiceModel.Primitives 8.1.2`.
- sprint-rituals pins `System.ServiceModel.Primitives 10.0.652802`.
- My net10 trial with pinning off resolved `protobuf-net 2.4.8` (via `protobuf-net.Grpc 1.2.2`) and `System.ServiceModel.Primitives 4.5.3`.
- Plan Step 1 says "transitive pinning off" and also "a pin only where restore needs one". With pinning off, a central `PackageVersion` entry is inert.
- Plan Step 2 says "every other third-party package → the reference's major line".
- Spec Flow 1 step 5 expects "the pinned ones this repo's central file holds for them".
- `coding-conventions.md` § Transitive pin vs root pin defines a pin as a direct reference plus `NoWarn="NU1605"`. That is a suppression, which AC 1 forbids.
**Surfaces:** spec § Flow 1 step 5 and item 7; plan § Step 1, Step 2, Decisions row 2
**Issue:** A pin never "needs restore" here; its reason is runtime. The package report has nothing to report, and the tool has no way to link a pin to the blocks that need it. The portability check, built from the report, passes on 2.4.8.
**Fix:** Decide explicitly. Either enable pinning and annotate pins per block so the report can include them, or accept 2.4.8 and 4.5.3 and record that as a reference departure. Say which one in step 1.

### [MEDIUM] F4 — The public-surface baseline is taken after step 2, not from the reference
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** check compares against the wrong baseline
**Consequence:** The "only listed names changed" check compares with a baseline already altered by the move step, so unlisted changes made there are never seen.
**Evidence:** quoted. Spec AC: "checked against a comparison of the public and protected surface of this build and the reference's". Plan Decisions: "A step-2 surface baseline from this repo's own build, not the reference's … build the reference to compare" (rejected).
**Surfaces:** spec § Acceptance Criteria; plan § Step 2, Step 12, Decisions
**Issue:** The plan rejected building the reference, but a scratch copy of its tracked files builds on net9. I did exactly that. Step 2's version-forced edits, and F1's warning fixes, land inside the baseline.
**Fix:** Build a scratch copy of the reference on net9, dump its surface, and diff it with the final build. Keep the step-2 dump as a secondary view.

### [MEDIUM] F4b — The "comments only" and "nothing else changed" pins cannot detect code loss
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** invariant pinned by a check that cannot see it
**Consequence:** Two checks meant to show that nothing but comments changed cannot detect a deleted statement, so accidental code loss passes as a safe edit.
**Evidence:** quoted. Step 4 accept: "`git diff --stat` of the step touches comments only (build output identical surface …)". Must-NOT-Change row 2 relies on SurfaceDump.
**Surfaces:** plan § Step 4, § Must-NOT-Change rows 1–2, § Step 2 (SurfaceDump)
**Issue:** `--stat` counts lines, and SurfaceDump covers signatures only. Untested code is common (full coverage is v2), and step 4 deletes commented-out code, which sits next to live code. SurfaceDump is also untested. Its handling of nullable annotations, `protected internal`, nested types and generic constraints is unspecified.
**Fix:** For step 4, build before and after with `Deterministic=true` and no PDB. Comment-only edits then give byte-identical DLLs. Give SurfaceDump a fixture test, for example rename a method and confirm the dump differs.

### [MEDIUM] F5 — Must-NOT-Change row 3 fails before any work starts
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** check false at baseline
**Consequence:** A safety check that expects clean read-only repositories fails on day 0, so it gets ignored or dropped and nothing guards against edits there.
**Evidence:** quoted and measured. Plan: "`git -C {repo} status --porcelain` empty for each before and after the build". Actual status now:
- reference: `?? .claude/.current-agent` and `?? .claude/audit/`, probably written by tooling run from that folder.
- sprint-rituals: many modified files, for example `client/apps/poker/src/api/sessions.ts`.
- knowledge-gateway: `M .gitignore`, `M docs/backlog.md`.
**Surfaces:** plan § Must-NOT-Change row 3
**Fix:** Record each repo's HEAD plus a hash of its `git status --porcelain` output (or a tracked-file diff hash) at the start, and compare for equality at the end.

### [MEDIUM] F6 — Several accept lines cannot fail, or cannot be run
**Anchor:** seed-0130 · above seed-0137 · below seed-0120
**Shape:** non-mechanical gate
**Consequence:** Several acceptance commands pass whether or not the work was done, so a skipped rename, leftover commented-out code or a stray byte-order mark ships unnoticed.
**Evidence:** scenario, each run or checked.
- (a) Async grep not runnable. The step-3 Async accept is `^\s*(public|protected)…(Task|ValueTask)… (?!\w*Async\b)\w+\(` with literal `…` placeholders. I ran an equivalent over all 116 files. It gives 12 hits: 9 rename rows, 3 `Handle`. There are zero private or internal hits, so the "private and internal also renamed" decision has nothing to apply to and no check.
- (b) `-w` article grep misses identifiers. `grep -rnwiE "articles?|journals?"` on the reference returns only `Redis/Repository.cs:45`. `GetArticleId` (`HttpContextProvider.cs:12,48,49`) and `articleCommand` (`AssignUserIdPreProcessor.cs:11,14`) do not match `-w`. The spec-item-4 variable rename therefore has no gate.
- (c) Commented-out code has no accept. AC 5 says "or a commented-out line of code". Step 4 greps only the three markers. Real instances exist at EF `Repository.cs:66-67`, `Core/Security/JwtOptions.cs:14` and `Hasura/HasuraRegistration.cs:53`.
- (d) No suppression grep. Nothing searches for `NoWarn`, `#pragma warning`, `SuppressMessage` or `WarningsNotAsErrors`.
- (e) BOM gate is narrow and locale-dependent. The BOM accept covers `src tools` only, but the AC says "no file in the repo". On a file with a BOM, `grep -rlP "^\xEF\xBB\xBF"` hits in the default shell and returns nothing under `LC_ALL=en_US.UTF-8`.
- (f) Gates run once, early. Steps 2–4 run the greps once. Steps 5–12 add files under `src/` and `tests/` with no final re-run.
- (g) Step 1's accept is weak. Building the empty `Blocks.slnx` exits 0 with "Unable to find a project to restore", and evaluates neither props file.
**Surfaces:** plan § Steps 1, 2, 3, 4; spec § Acceptance Criteria 3–5
**Fix:** Paste the executed regexes. Use `grep -rniE "article|journal"`. Add one final gate step that re-runs every AC grep repo-wide with `LC_ALL=C` and excludes `bin`/`obj`.

### [MEDIUM] F7 — "Pattern: sprint-rituals" for the error mapper imports unlisted shape and behaviour changes
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** pattern source diverges from reference
**Consequence:** Copying a pattern from another codebase also copies its unlisted changes in shape and behaviour, so the block drifts from the source in ways nobody approved.
**Evidence:** path.
- The reference middleware is `sealed` and takes `IWebHostEnvironment`. It logs only at status 500 or above, and sets `Details` on validation errors in development.
- The sprint-rituals version is not sealed and takes `IHostEnvironment`. It logs every non-validation error at Error level and sets no `Details` on validation errors.
- Spec item 7 allows "only these" changes.
**Surfaces:** plan § Step 5; reference and sprint-rituals `GlobalExceptionMiddleware.cs`
**Impact:** The constructor and `sealed` changes would surface only at step 12. The logging and `Details` differences would stay silent.
**Fix:** State in step 5 that the reference's class shape, log threshold and validation `Details` stay, and that only D1's listed behaviours change. The same applies to the A4 and D3 patterns.

### [MEDIUM] F8 — Flow 3 (status) has no defined "other side"
**Anchor:** seed-0130 · above seed-0127 · below seed-0117
**Band:** MEDIUM–HIGH; driver: whether the status output feeds the forward or send-back decision
**Shape:** two plausible builds, unresolved
**Consequence:** A status report compares against an undefined "other side", so two builders report different states for the same block and the next-step guidance misleads.
**Evidence:** quoted. Spec: "'This repo's side' is the working tree … when sending back; it is the manifest's commit when taking forward." Flow 3 only says "Per block: in step, changed in the app, changed here, …". Plan Step 10 defines forward (manifest commit) and back (working tree), and nothing for status.
**Surfaces:** spec § The sync script (three-way rule, Flow 3); plan § Step 10
**Issue:** "Changed here" means a newer committed block under one reading and uncommitted edits here under the other.
**Fix:** State the rule in step 10 and give each status state one named test. I suggest the manifest-commit view, plus a separate "uncommitted here" line.

### [MEDIUM] F9 — Rule 4 and Rule 7 have no guard for path escape or ignored files
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** invariant stated, mechanism missing
**Untouchable:** data-loss
**Consequence:** Path-escape and ignored-file cases are unguarded, so a mistyped folder or an ignored file can be written or deleted outside what the rules allow.
**Evidence:** scenario.
- Rule 4: "writes only inside the named app's blocks folder, its lock, and this repo's blocks folders". The plan does not require resolving `blocksFolder`, block names or lock paths and rejecting anything that escapes the app or `src/`. No test snapshots the tree before and after.
- `git status --porcelain` in the app can refresh `.git/index`, which is a write outside the allowed set. Use `git --no-optional-locks`.
- Rule 7: the plan's clean check is `git status --porcelain -- <folder>`. That does not list git-ignored files. The file set is everything except `bin/`, `obj/`, `.vs/` and `*.user`, so an ignored file inside a block folder (an `.env`, a local settings file) is deleted on adopt with no recovery. The spec's stated purpose is that "every replaced or removed file can be recovered".
- The excluded "editor folders" are only `.vs/` and `*.user`.
**Surfaces:** spec § Rules 4 and 7; plan § Step 10 (fingerprint and file set, first take)
**Fix:** Add a path-containment check and a "tree outside the allowed set unchanged" test. In the adopt check, also refuse if `--ignored` lists any file in the file set.

### [LOW] F10 — The portability check validates committed HEAD, not the working tree
**Anchor:** seed-0127 · above seed-0153 · below seed-0129
**Band:** LOW–MEDIUM; driver: whether step 11 is run before the commit
**Shape:** check reads stale source
**Consequence:** The portability check reads committed content only, so a dirty tree passes against stale files and reports a block as portable when it is not.
**Evidence:** quoted. Plan: forward "reads the manifest commit (`git ls-tree`/`git show`)". Step 11 says nothing about which commit it names or about a clean tree. The origin check also needs the checkout's real origin passed as the manifest `source`.
**Surfaces:** plan § Step 10, § Step 11
**Fix:** Step 11 should use `HEAD`, assert `git status --porcelain -- src` is empty, and read `source` from `git remote get-url origin`.

### [LOW] F11 — The root read-me criterion has no accept, and the 9|10 slice cut splits it
**Anchor:** seed-0137 · above seed-0153 · below seed-0123
**Shape:** acceptance item with no owner
**Consequence:** An acceptance item (the repo read-me with the sync instructions) has no step that checks it and is split across two builders, so it can be left stale.
**Evidence:** quoted. Step 9: "Root README.md … (written with step 10's commands; fill the commands in step 10 if needed)". Step 9's accept covers the 11 block read-mes and `SQL Server` only. Step 10's accept does not mention the read-me. The slice line cuts between 9 and 10.
**Surfaces:** plan § Step 9, § Step 10, build-slices line
**Fix:** Move the root read-me's sync section into step 10 with an accept: a grep for `blocks.json`, the three commands and the way-out sentence.

### [LOW] F12 — Plan-time fact slip: byte-order-mark count
**Anchor:** seed-0127 · above seed-0153 · below seed-0129
**Shape:** mis-scoped count
**Consequence:** A cited count includes out-of-scope projects, so the reader overstates how many in-scope files need the fix.
**Evidence:** measured. "149 of 151 reference `.cs` files" is the whole `src/BuildingBlocks`, which includes the `Articles.*` projects. In the 11 blocks it is 115 of 116. `RepositoryExtensions.cs` has none. Every csproj has one.
**Surfaces:** plan § Context
**Fix:** Correct the figure.

### [LOW] F13 — The step-6 test for the `ConflictException` change cannot be red-first
**Anchor:** seed-0153 · above seed-0127 · below seed-0129
**Shape:** TDD ordering
**Consequence:** A test written after the code change it covers passes at once, so it cannot prove it would have caught the old behaviour.
**Evidence:** quoted. The `EnsureNotExistsOrThrowAsync → ConflictException` edit is a step-5 bullet. Step 6 says "test it throws that type", and the EF test project only appears in step 6.
**Surfaces:** plan § Step 5, § Step 6
**Fix:** Move that edit into step 6, behind its test.

Gap Analysis
- Details field for non-500 errors: the spec does not say whether `Details` also appears on non-500 and validation replies in development. Plan must resolve it. Step 5's tests should cover whichever is chosen.
- Tool invocation in tests: step 10's CLI has no `--source` option, so the sync tests presumably call an in-process API. Say so, because the exit codes (0, 1, 2) are only testable through the entry point.
- Package report format: the report is parsed by the portability test, but its format is not stated. Name it, for example JSON on stdout.

Open Questions
- Does `xunit.v3` with `dotnet test` on SDK 10 need a `global.json` runner opt-in? (Low confidence; I did not execute it.) Say so in step 1 if it does.
- Would the owner accept protobuf-net 2.4.8 as the unpinned resolution (F3)? Only the owner knows what the apps depend on.

Reviewed: D:\src\dotnet-building-blocks\docs\specs\F1-VersionOne\delivery\plan.md

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| P1-01 | codex, sonnet | CRITICAL | unranked — no anchor fits; Codex severity CRITICAL | No containment contract for the tool's writes | invariant stated, mechanism missing | traversal, junctions or a bad name can write or delete outside the allowed folders | contested: CRITICAL / MEDIUM — settled CRITICAL: an escape deletes files in other repositories | data-loss | sync safety | plan § Steps 10, 11 | codex 1, F9 |
| P1-02 | codex, sonnet | HIGH | seed-0129 | Adopt check ignores ignored files | invariant stated, mechanism missing | an ignored file is deleted with no recovery | `git status --porcelain` omits ignored files | data-loss | sync safety | plan § Step 10 | codex 2, F9 |
| P1-03 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Http.Abstractions package has no 10.x | wrong version claim | step 2 cannot restore as written | NuGet history of Microsoft.AspNetCore.Http.Abstractions | | packages | plan § Step 2 | codex 3 |
| P1-04 | codex, sonnet | HIGH | seed-0129 | Surface baseline taken after migration edits | wrong baseline | unlisted step-2 changes are never seen | contested: HIGH / MEDIUM — settled HIGH: the names list is a promised deliverable | | names | plan § Steps 2, 12 | codex 4, F4 |
| P1-05 | codex, sonnet | HIGH | seed-0117 | Namespace rule applied to only part of the folders | requirement with no step | mixed namespaces ship unlisted | per-folder scan: 5 more folders | | names | plan § Step 3 | codex 5, F2 |
| P1-06 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Portability check tests the wrong commit | check reads stale source | the candidate blocks are never the ones tested | contested: HIGH / LOW — settled HIGH: without it the check proves nothing about this release | | portability | plan § Step 11 | codex 6, F10 |
| P1-07 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | BOM and repo-wide gates locale-bound, narrow, run once | non-mechanical gate | a byte-order mark or a late file ships unseen | `grep -P` misses a BOM under a UTF-8 locale | | gates | plan § Steps 2–4 | codex 7, F6(e)(f) |
| P1-08 | codex, sonnet | HIGH | seed-0120 | Zero warnings impossible under "no other edit" | acceptance contradicts constraint | step 2 stops at its own accept | contested: MEDIUM / HIGH — settled HIGH: measured 22 nullable warnings, CS0618, EF1002 ×2 | | build | plan § Steps 1, 2, 6 | codex 9, F1 |
| P1-09 | codex, sonnet | MEDIUM | seed-0153 | Conflict-helper change precedes its test | TDD ordering | the test cannot be red first | step 5 bullet, step 6 test | | tests | plan § Steps 5, 6 | codex 8, F13 |
| P1-10 | codex, sonnet | MEDIUM | seed-0130 | No suppression or full commercial-package check | missing gate | a suppression or paid package ships | AC 1, item 6 | | gates | plan § Steps 1, 2 | codex 10, F6(d) |
| P1-11 | codex, sonnet | MEDIUM | seed-0130 | Naming and product-word gates cannot fail | non-mechanical gate | a skipped rename passes | literal ellipses; `-w` misses `GetArticleId` | | gates | plan § Step 3 | codex 11, F6(a)(b) |
| P1-12 | codex, sonnet | MEDIUM | seed-0130 | "Comments only" cannot detect code loss; commented-out code has no gate | invariant pinned by a blind check | deleted statements pass as comment edits | `--stat`; Repository.cs:66–67, JwtOptions.cs:14 | | gates | plan § Step 4, Must-NOT-Change | codex 12, F4b, F6(c) |
| P1-13 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Mapper tests skip development details and nested names | missing test | half of D1 untested | D1 text | | tests | plan § Step 5 | codex 13 |
| P1-14 | codex, sonnet | MEDIUM | seed-0137 | Read-me acceptance checks nothing; root read-me split across slices | acceptance with no owner | empty read-mes pass | step 9 accept | | docs | plan § Steps 9, 10 | codex 14, F11 |
| P1-15 | codex, sonnet | MEDIUM | seed-0129 | Editor-folder exclusions incomplete | incomplete policy | `.idea`, `.vscode` enter fingerprints | step 10 file set | | sync design | plan § Step 10 | codex 15, F9 |
| P1-16 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 12 passes while the spec table stays incomplete | deferred reconciliation | the names criterion is not met at done-check | step 12 "Spec delta" | | names | plan § Step 12 | codex 16 |
| P1-17 | codex, sonnet | MEDIUM | seed-0130 | Foreign-repo check false at baseline | check false at baseline | the guard fails on day 0 and gets dropped | measured dirty status in all three | | safety | plan § Must-NOT-Change | codex 17, F5 |
| P1-18 | sonnet | MEDIUM | seed-0130 | Transitive pinning off downgrades protobuf-net to 2.x | plan decision contradicts spec | a serialization library silently drops a major version | measured restore: protobuf-net 2.4.8 | | packages | plan § Steps 1, 2, Decisions | F3 |
| P1-19 | sonnet | MEDIUM | seed-0129 | Sprint-rituals mapper pattern imports unlisted changes | pattern diverges | constructor, sealing, logging and details drift | the two middleware files | | error mapper | plan § Step 5 | F7 |
| P1-20 | sonnet | MEDIUM | seed-0130 | Status has no defined "other side" | two plausible builds | status misleads the next step | spec Flow 3 | | sync design | plan § Step 10 | F8 |
| P1-21 | codex | LOW | unranked — no anchor fits; Codex severity LOW | GlobalUsings claim false | wrong fact | a misleading reason in the mapping note | reference EF GlobalUsings.cs exists | | evidence | plan § Skill Mapping | codex 18 |
| P1-22 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Slice "why" misstates dependencies | wrong reason | none at build | step 12 uses SurfaceDump, not the sync tool | | plan | plan header | codex 19 |
| P1-23 | sonnet | LOW | seed-0127 | BOM count mis-scoped | mis-scoped count | overstated figure | 115 of 116 in the 11 blocks | | evidence | plan § Context | F12 |
| P1-24 | sonnet | LOW | seed-0364 | Report format, test entry point, development details on non-500 replies unstated | mechanism unresolved | two builds possible | gap analysis | | plan | plan § Steps 5, 10, 11 | F gap analysis |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-08)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| P1-01 | fixed-real | CRITICAL | | plan § Step 10 (containment contract, negative tests), § Step 11 (temp root) |
| P1-02 | fixed-real | HIGH | | plan § Step 10 (adopt check with ignored files) |
| P1-03 | fixed-real | HIGH | | plan § Step 2 (FrameworkReference), Decisions |
| P1-04 | fixed-real | HIGH | | plan § Step 2 (reference baseline from a scratch copy), § Step 12, Decisions |
| P1-05 | fixed-real | HIGH | | plan § Step 3 (full table, majority rule), spec item 3, Decisions |
| P1-06 | fixed-real | HIGH | | plan § Step 10 (`--source`), § Step 11 (snapshot commit) |
| P1-07 | fixed-real | HIGH | | plan § Step 13 (hygiene tests, byte reads, repo-wide) |
| P1-08 | fixed-real | HIGH | | plan § Step 2 (warning-fix allowance), § Step 6 (A4 route), Decisions |
| P1-09 | fixed-real | MEDIUM | | plan § Steps 5, 6 |
| P1-10 | fixed-real | MEDIUM | | plan § Step 13 |
| P1-11 | fixed-real | MEDIUM | | plan § Steps 3, 13 |
| P1-12 | fixed-real | MEDIUM | | plan § Step 4 (deterministic build compare), § Step 13, Must-NOT-Change |
| P1-13 | fixed-real | MEDIUM | | plan § Step 5 |
| P1-14 | fixed-real | MEDIUM | | plan § Steps 9, 10, 13 |
| P1-15 | fixed-real | MEDIUM | | plan § Step 10 |
| P1-16 | fixed-real | MEDIUM | | plan § Step 12, spec § Public names (points at the generated list) |
| P1-17 | fixed-real | MEDIUM | | plan § Step 1 (snapshot), Must-NOT-Change |
| P1-18 | fixed-real | MEDIUM | | plan § Steps 1, 2, 10 (pinning on, pins annotated per block), Decisions |
| P1-19 | fixed-real | MEDIUM | | plan § Step 5 |
| P1-20 | fixed-real | MEDIUM | | plan § Step 10 |
| P1-21 | fixed-real | LOW | | plan § Skill Mapping note |
| P1-22 | fixed-real | LOW | | plan header |
| P1-23 | fixed-real | LOW | | plan § Context |
| P1-24 | fixed-real | LOW | | plan § Steps 5, 10, 11 |

Reconciled: plan § Steps 2, 3, 10–13 against spec § contents items 3 and 7, § The sync script, § Rules 4, 7, § Acceptance Criteria, § Public names; § Must-NOT-Change against Steps 1, 4, 12; the build-slices line against the new step count.

## Round 2 — 2026-10-08 — plan — primary (codex)

Header note: delta read, floor MEDIUM (round 1's `Coverage:` is `complete`; slug number 1 is not divisible by 5).
Primary only. Codex's verdict (NO-GO: 1 HIGH, 4 MEDIUM; 17 of 20 round-1 findings at the floor closed) is persisted
at `review-critic-codex.md` under the plan section's second `## Verdict:`.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| P2-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Hard-linked destination files bypass containment | invariant stated, mechanism missing | an in-place overwrite changes a file outside the allowed folders | containment checks directories only | data-loss | round-1 miss: plan § Step 10 | plan § Step 10 | codex r2 P1-01 partial |
| P2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | "Uncommitted here" compares against the wrong commit | wrong baseline | status reports uncommitted work on a clean, newer checkout | plan § Step 10 status line | | fold | plan § Step 10 | codex r2 new |
| P2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | BOM gate excludes `docs/` | narrowed guarantee | a BOM in generated docs passes | step 13 exclusion list | | fold | plan § Step 13 | codex r2 P1-07 partial |
| P2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Commercial check names only four lines | incomplete gate | another paid dependency passes | step 13 package bullet | | round-1 miss: plan § Step 13 | plan § Step 13 | codex r2 P1-10 partial |
| P2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Foreign-repo guard blind to content of already-modified files | check cannot see the invariant | a write to a modified file keeps its status letter | step 1 snapshot, step 13 compare | | fold | plan § Steps 1, 13, Must-NOT-Change | codex r2 new |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-08)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| P2-01 | fixed-real | HIGH | | plan § Step 10 (no in-place overwrite; hard-link test) |
| P2-02 | fixed-real | MEDIUM | | plan § Step 10 (status marks) |
| P2-03 | fixed-real | MEDIUM | | plan § Step 13 |
| P2-04 | fixed-real | MEDIUM | | plan § Step 13 (licence test over every resolved package) |
| P2-05 | fixed-real | MEDIUM | | plan § Step 1, § Step 13, § Must-NOT-Change (ignored folders: last-write times only, accepted gap) |

Reconciled: plan § Step 10 containment and status against spec Rules 4, 7 and Flow 3; § Steps 1 and 13 against
§ Must-NOT-Change. Schedule: round 2's merge holds no CRITICAL, so no round 3 runs. Plan approved.
