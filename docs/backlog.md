# Backlog

| Slug | Status | What |
|---|---|---|
| F1-VersionOne | Done — 2026-10-08 (summary.md) | First release of the generic blocks: the reference's 11 blocks on .NET 10, cleaned, with the agreed fixes and the two-way sync script. |
| F2-ReviewedChangeRequests | Follow-up | A reviewed path for block changes from any app into this repo (the curation step left out of F1's plain two-way copy). |
| BUG-1-F1-VersionOne-carry-over | Open | F1's under-the-bar MEDIUM: the sync tool's empty-folder deletion helper (`tools/Blocks.Sync/SafeFiles.cs:75`) can follow a junction at a replacement target and delete empty folders outside the app (spec Rule 4). Fix: refuse a linked replacement target in preflight, before any change. See `docs/specs/F1-VersionOne/delivery/review.md` § Merge, no fix round. |
