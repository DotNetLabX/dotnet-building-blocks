# F3-GatewayFeedback — ideas

## Topic: A duplicate key at save becomes the conflict error (architect)

### A — The app registers a small translator for its database
- Problem: the blocks reference no database provider, so they cannot recognise a duplicate-key error themselves.
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage


### B — Use the EntityFramework.Exceptions package
- Problem: it already turns each database's errors into one typed duplicate-key error, so no per-database code is written here. Marked new: neither this repo nor the reference uses it.
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage


### C — The save step reads the database's error code from a short built-in list
- Problem: no extra package, and the two databases the apps use (SQL Server, PostgreSQL) are covered by reading the inner error's code by name.
- Status: picked
- Parked concerns: none
- Owner's ask: the easiest form (2026-10-11)
- Critique: first dropped on the claim that only a race reaches it; the spec review found the blocks check only a numeric id, so any other unique column reaches it in normal use; recognises SQL Server and PostgreSQL by type name and code with no package, other databases keep the 500; owner added it back (2026-10-11)

## Topic: The gRPC packages inside the web block (architect)

### A — Move the gRPC client registration into its own block, Blocks.Grpc
- Problem: a host that takes the web block only for the error handler stops pulling in gRPC.
- Status: picked
- Parked concerns: none
- Owner's condition: only if the extraction is clean (2026-10-11)
- Critique: clean — two files move, nothing else in the web block or any other block uses them; cost is one new block and one using line in the reference's three gRPC services; the debug-build accept-any-certificate setting moves unchanged and is noted; owner kept it (2026-10-11)

### B — Leave it where it is and say so in the read-me
- Problem: no app that already took the web block has to change anything.
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

## Topic: One reply shape for a failed validation (architect)

### A — Both paths write the endpoint library's shape
- Problem: a client sees `{statusCode, message, errors: {field: [messages]}}` whichever endpoint style the service uses.
- Status: picked
- Parked concerns: none
- Critique: Vue form libraries take a per-field map directly; the standard problem-details form has the same map under different outer fields, a bigger change for no gain; knowledge-gateway's web app reads only `message`, so nothing there breaks; the plugin's error-handling skill teaches the flat list and gets a plugin note; owner kept it, outer fields unchanged (2026-10-11)


### B — Both paths write ASP.NET's standard validation problem form
- Problem: a shape every HTTP client library already reads. Marked new: neither this repo nor the reference writes it.
- Status: dropped
- Parked concerns: none
- Reason: the owner picked the per-field map in the interview (2026-10-11)


### C — Keep the two shapes and write in each read-me which path writes which
- Problem: no client that already reads either shape has to change.
- Status: dropped
- Parked concerns: none
- Reason: the owner picked the per-field map in the interview (2026-10-11)
