# Blocks.Hasura

## Purpose

Clients for a Hasura GraphQL engine:

- `AddHasuraGraphQL` — a singleton `GraphQLHttpClient` for `{BaseUrl}/v1/graphql`, sending the admin secret.
- `AddHasuraMetadata` — Refit clients for the metadata and SQL APIs (`IHasuraMetadataApi`, `IHasuraSqlApi`)
  and `HasuraMetadataService`, which tracks tables and their object and array relationships.
- `HasuraMetadataInitService` — a background service that runs the tracking at start-up.
- `HasuraOptions` — `BaseUrl`, `AdminSecret`.

## Depends on

- Blocks: `Blocks.Core`.
- Packages: `GraphQL.Client`, `GraphQL.Client.Serializer.SystemTextJson`, `Refit.HttpClientFactory`,
  `Refit.Newtonsoft.Json`, `Humanizer.Core`, `Microsoft.Extensions.Hosting`.

## Registration

```csharp
services.AddAndValidateOptions<HasuraOptions>(configuration);
services.AddHasuraGraphQL(configuration);
services.AddHasuraMetadata(configuration);
services.AddHostedService<HasuraMetadataInitService>();
```

Both methods read the `HasuraOptions` section.
