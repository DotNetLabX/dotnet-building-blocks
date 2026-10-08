# Blocks.Http.Abstractions

## Purpose

Small helpers over the ASP.NET Core HTTP abstractions for projects that should not take the whole
`Blocks.AspNetCore` block: `IFormFile.GetExtension()` returns an uploaded file's extension.

## Depends on

- Blocks: none.
- Packages: none; the `Microsoft.AspNetCore.App` shared framework (`FrameworkReference`).

## Registration

Nothing to register; call the extension methods.
