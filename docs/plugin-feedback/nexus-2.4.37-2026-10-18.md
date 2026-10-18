# nexus 2.4.37 — feedback from dotnet-building-blocks (2026-10-18)

Source: the F3-GatewayFeedback team run (first team run in a separate worktree in this repo).

## 1. Worktree carry-over misses the session's own untracked settings

- **What happened:** the team lead carried the slug's untracked spec/plan folder into the new worktree but not `.claude/verify.json`, which in this repo is untracked as a whole. `verify-run.js` reads it from its working folder, so in the worktree the step close and `--files` runs refused. The developer caught it at Phase 1 (its Q1); the team lead copied the file.
- **Ask:** in agents-workflow § Worktree duties (carry-over), name the untracked files that hooks and runners read from the working folder (`.claude/verify.json`, `.claude/test-times.json`) beside the slug's delivery artifacts — or have `verify-run.js` fall back to the session root's `verify.json` when the tree has none.

## 2. Read-tracker reports worktree reads under the main checkout's path

- **What happened:** during the build the read-tracker warned of developer re-reads of `D:/src/dotnet-building-blocks/tests/...` (the main checkout) while the developer worked only in `D:/src/dotnet-building-blocks-F3-GatewayFeedback`; the main checkout showed no new writes. It also once reported `D:/d/src/...` for a main-session read of a `/d/src/...` path.
- **Ask:** resolve relative paths against the subagent's working folder (or the published `.worktree-target` tree), and normalise Git-Bash `/d/...` paths before recording.

## 3. `verify-run.js` keeps too little of a failing run's output

- **What happened:** the record's `output_excerpt` holds about 1.6k characters (head and tail), so the failing assertion is usually cut off; the developer wrote throwaway probe tests that wrote the error to a file to see it.
- **Ask:** keep the full output in a side file under `.claude/audit/` and point to it from the record, or keep the failing tests' messages (the runner's failure blocks) rather than the raw head and tail.

## 4. A fold's own additions labelled as an owner ruling

- **What happened:** the plan's step 9 read "Test-only packages (owner, 2026-10-13): `Microsoft.Data.SqlClient` and `Npgsql`"; the owner's ruling that day named only PostgreSQL — SqlClient came from the architect's fold of a critic finding. SqlClient's native dependency then failed the licence test, and the owner asked "who introduced this test library? not me!".
- **Ask:** in the architect's fold rules, an owner attribution may cite only what the ruling line says; additions the fold makes are labelled as the architect's. A licence fact in a plan covers the package's whole dependency closure, not the top package.
