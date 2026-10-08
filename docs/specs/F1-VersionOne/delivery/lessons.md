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
