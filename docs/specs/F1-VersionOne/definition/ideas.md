# Ideas — F1-VersionOne

## Topic: Sync script and manifest (architect)

### A — A manifest per app
- Idea: each app has a `blocks.json` naming the commit and its blocks; the script copies those blocks and the blocks they reference.
- Problem: one readable place says what an app takes and from where.
- Status: picked
- Parked concerns: none
- Critique: the blocks pulled in alongside come from each block's project references, so the app lists only what it uses directly; owner kept it (2026-10-08).

### B — Lock file with a hash per file, refusal on a differing copy
- Idea: a lock file records a hash per copied file; the script refuses to overwrite a copy that differs and lists the files; a check-only mode for the app's build pipeline.
- Problem: a local edit is never silently lost or silently kept.
- Status: picked
- Parked concerns: none
- Critique: the hashes tell which side changed, which is what makes the two-way copy (I) safe; the build-pipeline check that fails on an edit no longer fits once app edits are allowed, so it is left out; owner kept it in that form (2026-10-08).

### C — Stamp file inside each copied block
- Idea: each copied block carries a small file with the repo address and commit.
- Problem: a reader of the app sees where a block came from.
- Status: dropped
- Parked concerns: none
- Reason: repeats what the lock file already says; owner chose the set without it (2026-10-08).

### D — Package versions handled by the script
- Idea: the script adds the package versions an app is missing to its central file and reports, never lowers, the ones it has at another version.
- Problem: a copied block builds without hand edits to packages.
- Status: picked
- Parked concerns: none
- Critique: needed, otherwise a copied block does not build; changed to report only — the script lists the missing versions and the person adds them; owner kept it in that form (2026-10-08).

## Topic: Changes coming back from an app (architect)

### E — Send-back run opening a branch here
- Idea: the script compares an app's copy with its lock and opens a branch in this repo with the app's changes; a session here reviews and merges.
- Problem: work done in an app reaches this repo without retyping.
- Status: parked
- Parked concerns: none
- Reason: reviewed change requests from any app are a follow-up, not this feature (owner, 2026-10-08).

### F — Patch file with a note
- Idea: the script saves an app's changes as a patch file and a short note, in a requests folder here or as an issue; a later session applies it.
- Problem: a change is recorded even when nobody has time to merge it now.
- Status: parked
- Parked concerns: none
- Reason: reviewed change requests from any app are a follow-up, not this feature (owner, 2026-10-08).

### G — Mark a block "being changed" in the app
- Idea: an app's lock can mark a block as being changed; edits are accepted only on marked blocks, and the check reminds until the change is sent back.
- Problem: the app keeps working, but an edit is never forgotten.
- Status: parked
- Parked concerns: none
- Reason: owner chose plain two-way copy for now (2026-10-08).

### H — Issue for a change an app only asks for
- Idea: for a change an app wants but does not make, the script opens an issue naming the block and the request.
- Problem: a wish without code still reaches this repo.
- Status: parked
- Parked concerns: none
- Reason: reviewed change requests from any app are a follow-up, not this feature (owner, 2026-10-08).

### I — The same script copies a whole block in either direction (owner's idea)
- Idea: one script copies a whole block forward, from this repo into an app, or back, from an app into this repo; no review step.
- Problem: a block that advances in an app reaches this repo, and devs are never blocked from fixing a block where they work.
- Status: picked
- Parked concerns: none
- Critique: if two apps send back the same block, the second could overwrite the first; the lock refusal (B) prevents it, since the script refuses when both sides changed. An app may edit its copy, and sends the whole block back before it syncs anything else. Owner kept it (2026-10-08).
