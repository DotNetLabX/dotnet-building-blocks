# F4-GatewayHandover — lessons

## Architect Lessons

- **Diff a returned copy against the commit it was taken from, not against today's tree.** Knowledge-gateway's copy was taken at `9a55da0`, before F3; a straight diff against `HEAD` mixed F3's changes with the hand-over's and hid that point 2 was already delivered. Diffing the copy against `git show 9a55da0:…` isolated the hand-over's own change in one pass.
- **Swapping an anonymous reply object for a named record changes who names the wire keys.** Anonymous members are written verbatim; record members go through the serializer's naming policy. Both plan readers caught it; the plan's "wire unchanged" Accept had only checked the default policy. Pin envelope names with `[JsonPropertyName]` whenever a reply type is introduced for documentation.
- **Check the repo's own earlier conventions before recommending a signature.** F3's plan had set "no optional parameter, so method groups compile"; I recommended an optional parameter without searching for it, and the review found it after the owner had ruled. A grep of earlier plans for "optional parameter" at interview time would have put the trade-off in the question.
- **A compatibility Accept must name a project that can see the code.** "The compatibility project still builds" was vacuous: that project does not reference the block. Grep a test project's references before citing it as proof.
- **The approval-scope stamp also gates the coordinator's own delivery records.** Stamp the run's record files (communication log, lessons, review and done-check files, summary) with the source paths, or the coordinator's first log write is refused.

- **A worktree needs the repo's untracked test-command file.** This repo keeps `.claude/verify.json` untracked, so a fresh worktree had none and the step runner refused every step run ("no step run"); step 1 was built blind until the coordinator copied the file in. Copy `.claude/verify.json` into the worktree with the plan artifacts, before the first developer dispatch.
- A worktree lane whose `.claude/verify.json` is not carried into the tree before the first developer dispatch leaves step 1 with no step-run record (the runner refuses), which the light done check must score as a test slip even when the coordinator's tied run passed the same files seconds later. The carry-over duty should copy the runner's settings with the delivery artifacts before the first spawn. **Evidence:** [F4-GatewayHandover]

## Skill Gaps

- **Skill gap — public surface dump and names table:** step 5 (`tools/Blocks.SurfaceDump` → `docs/public-names.md`) maps to no skill, as in F3's Step 11. Repo-local procedure; a project skill would carry it.

## Developer Lessons
- A plan step that says "build the solution in Release, then run a tool on the assemblies" (the surface dump, F3 step 11 and F4 step 5) cannot be followed by a spawned developer: the test-entry gate refuses any separate build. What worked: read the Debug assemblies the step runs already built, with the test projects' output folders as `--probe` folders, through an already-built copy of the tool. A plan should name that route, or mark the dump `Owner: close`.
