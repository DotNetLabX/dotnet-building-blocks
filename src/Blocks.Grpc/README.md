# Blocks.Grpc

## Purpose

Clients for code-first gRPC services (protobuf-net):

- `AddCodeFirstGrpcClient<TClient>` — a scoped client on its own channel.
- `AddCodeFirstGrpcClientWithRetry<TClient>` — the same, with a retry policy on the channel when the service's
  `EnableRetry` is set: `Retry.Count` retries of calls that fail as unavailable, starting `Retry.InitialDelayMs`
  apart.
- `AddCodeFirstGrpcClientAsSingleton<TClient>` — one channel and client for the whole process.

Each method reads the service's settings from `GrpcServicesOptions.Services` under a key: the one you pass, or the
client type's name without `Client` and `Service` (`IOrderService` reads `IOrder`). A missing key refuses the
registration with `Missing GrpcService config for: {client type}`. In a debug build the channel accepts any server
certificate.

The service URL and the retry bounds are not checked at start-up. Their attributes (`[Required]` on `Url`,
`[Range]` on `Count` and `InitialDelayMs`) sit on the nested settings classes, and no member carries the
nested-check marker. A missing URL shows only when the channel is built: at registration for the singleton form,
and when the client is first resolved for the other two.

## Depends on

- Blocks: none.
- Packages: `Grpc.Net.Client`, `protobuf-net.Grpc`, `Microsoft.Extensions.DependencyInjection.Abstractions`.
- Pinned in the central versions file for this block: `protobuf-net`, `protobuf-net.Core` (the sync script's
  package report lists them).

## Registration

```csharp
// appsettings.json: "GrpcServicesOptions": { "Retry": { "Count": 3, "InitialDelayMs": 200 },
//   "Services": { "IOrder": { "Url": "https://orders:5001", "EnableRetry": true } } }
var grpcOptions = configuration.GetSectionByTypeName<GrpcServicesOptions>();   // from Blocks.Core
services.AddCodeFirstGrpcClientWithRetry<IOrderService>(grpcOptions);
```
