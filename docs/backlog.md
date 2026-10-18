# Backlog

| Slug | Status | What |
|---|---|---|
| F1-VersionOne | Done — 2026-10-08 (summary.md) | First release of the generic blocks: the reference's 11 blocks on .NET 10, cleaned, with the agreed fixes and the two-way sync script. |
| F2-ReviewedChangeRequests | Follow-up | A reviewed path for block changes from any app into this repo (the curation step left out of F1's plain two-way copy). |
| F3-GatewayFeedback | Done — 2026-10-18 (summary.md) | The blocks' answer to knowledge-gateway's findings of 2026-10-10 (`definition/source-feedback.md`, a copy of `D:\src\knowledge-gateway\docs\plugin-feedback\blocks-2026-10-10.md`): the seven block fixes its F201-SharedBase names (events from any key type, audit "who" type, started replies, argument errors, design-time factory, stored-JSON converters, duplicate-key conflicts), the endpoint-library set-up split, and the smaller points. Technical feature, architect-led. |
| BUG-1-F1-VersionOne-carry-over | Open | F1's under-the-bar MEDIUM: the sync tool's empty-folder deletion helper (`tools/Blocks.Sync/SafeFiles.cs:75`) can follow a junction at a replacement target and delete empty folders outside the app (spec Rule 4). Fix: refuse a linked replacement target in preflight, before any change. See `docs/specs/F1-VersionOne/delivery/review.md` § Merge, no fix round. |
| BUG-2-hygiene-scans-vs-cache | Open | Found after F1's close (2026-10-08): opening `Blocks.slnx` in Visual Studio creates `.vs/`, which `.gitignore` does not ignore, and `FileTests.NoFileInTheRepo_SuppressesAWarning` scans it (binary design-time caches) and fails. Fix: ignore `.vs/` and make the hygiene file scan skip ignored and editor folders. |
