# dotnet-building-blocks

The one home of the generic .NET building blocks: small libraries named `Blocks.*`, each covering one technology or
concern, with no product knowledge. Apps do not reference them as packages. A sync tool in this repo copies whole
blocks into an app, and copies an app's edits back here.

## Blocks

| Block | What it is for |
|---|---|
| `Blocks.Core` | options binding, guards, a thread-safe memory cache, the request context, general extensions |
| `Blocks.Exceptions` | HTTP error types, each with its status code, and `SingleOrThrow` |
| `Blocks.Domain` | entity, aggregate and domain-event base types |
| `Blocks.AspNetCore` | the error mapper, request diagnostics, claims and route providers, gRPC client registration |
| `Blocks.EntityFrameworkCore` | repositories, entity configurations, interceptors, transactions, seeding |
| `Blocks.FastEndpoints` | the FastEndpoints domain-event publisher and pre-processors |
| `Blocks.MediatR` | MediatR pipeline behaviors |
| `Blocks.Messaging` | MassTransit on RabbitMQ registration |
| `Blocks.Redis` | a Redis OM repository and seeding |
| `Blocks.Http.Abstractions` | small helpers over the ASP.NET Core HTTP abstractions |
| `Blocks.Hasura` | Hasura GraphQL and metadata clients |

Each block's folder under `src/` has a `README.md` with its purpose, its dependencies and how to register it. Tests
live under `tests/` and are never copied into an app. Package versions live in `Directory.Packages.props`.

## Taking blocks into an app

Write `blocks.json` at the app's root:

```json
{
  "source": "https://github.com/DotNetLabX/dotnet-building-blocks",
  "commit": "<a commit of this repo>",
  "blocksFolder": "src/BuildingBlocks",
  "packagesFile": "src/Directory.Packages.props",
  "blocks": ["Blocks.AspNetCore", "Blocks.EntityFrameworkCore"]
}
```

- `source` must be this checkout's `origin`; the tool refuses to run from a checkout of another repo.
- `commit` is the version of this repo to take. If the checkout does not have it, the tool stops with "fetch first".
- `blocksFolder` and `packagesFile` are relative to the app's root.
- `blocks` lists the blocks the app uses; the blocks they depend on are added automatically.

The tool writes `blocks.lock.json` beside the manifest: for each block, the commit it was copied from and a
fingerprint of each of its files. Commit both files in the app. The tool never commits, pushes, or edits the app's
package-versions file.

## The three commands

Run from a checkout of this repo:

```
dotnet run --project tools/Blocks.Sync -- forward --app <path-to-app> [--adopt] [--json]
dotnet run --project tools/Blocks.Sync -- back --app <path-to-app>
dotnet run --project tools/Blocks.Sync -- status --app <path-to-app>
```

- `forward` copies each block whole, at the manifest's commit, into the app's blocks folder, then writes the lock and
  lists the package versions the app's central file lacks or has at another version (`--json` prints that list as
  one JSON object). It stops, writing nothing, if any block was changed in the app: send it back first.
- `back` copies each block the app changed into this checkout's `src/`, without a leading byte-order mark. Commit it
  here, set the app's manifest to that commit, and run `forward`: both sides are equal, so the block is in step and the
  lock is refreshed.
- `status` shows each block as in step, changed in the app, changed here, changed on both sides, no longer listed, or
  not yet taken, and marks a block "newer here" or "uncommitted here" when this checkout holds a later version.

`--source <path>` runs against another checkout of this repo. Exit codes: 0 done, 1 refused, 2 usage or environment
error.

A file counts as changed when it is added, removed, renamed or edited; line endings and a leading byte-order mark
never count. Build output (`bin`, `obj`) and editor files (`.vs`, `.vscode`, `.idea`, `*.user`, `*.suo`,
`.DS_Store`) are not part of a block.

## First take of an existing folder

If the app already has a folder for a block but the lock has no entry for it, `forward` refuses. Run `forward --adopt`
to replace the folder. The tool lists the files it will remove first, and adopts only when git holds every file of
that folder committed in the app's repository (nothing changed, untracked or ignored), so every replaced or removed
file can be recovered from the app's history.

## Changed on both sides

When a block changed in the app and here since the last copy, `forward` and `back` both refuse and list the files on
each side. The way out: set the app's edit aside, set the app's manifest to the commit here that holds the other
change, take the block forward, apply the edit again, then send it back.

A block dropped from the manifest keeps its folder and lock entry; `status` reports it as no longer listed, and an
edit to it still stops `forward` until it is sent back. Removing it is a person's act.
