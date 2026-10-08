# Blocks.Exceptions

## Purpose

HTTP-aware exception types. Each one carries the status code it should answer with, so a global error
handler can turn it into a reply without knowing every type:

- `HttpException` — the base: `HttpStatusCode` and `StatusCode`.
- `BadRequestException` (400), `UnauthorizedException` (401), `ForbiddenException` (403),
  `NotFoundException` (404), `ConflictException` (409), `BadGatewayException` (502).
- `SingleOrThrow` — `SingleOrDefault` that throws `NotFoundException` when nothing matches.

## Depends on

- Blocks: none.
- Packages: none.

## Registration

Nothing to register. Throw the types from application code; `Blocks.AspNetCore`'s
`GlobalExceptionMiddleware` maps each to its status.
