using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using Xunit;

namespace Blocks.Grpc.Tests;

[Service]
public interface IPongClientService
{
    ValueTask<PingReply> PongAsync(PingRequest request, CallContext context = default);
}

public sealed class GrpcClientKeyAndLifetimeTests
{
    public static TheoryData<string, ServiceLifetime> LifetimePerRegistration => new()
    {
        { nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClient), ServiceLifetime.Scoped },
        { nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientWithRetry), ServiceLifetime.Scoped },
        { nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientAsSingleton), ServiceLifetime.Singleton },
    };

    [Theory]
    [MemberData(nameof(LifetimePerRegistration))]
    public void TheDefaultKey_DropsBothClientAndService(string registration, ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();

        Register(registration, services, new GrpcServicesOptions
        {
            Retry = new GrpcRetrySettings { Count = 2, InitialDelayMs = 100 },
            Services = new Dictionary<string, GrpcServiceSettings> { ["IPong"] = new() { Url = "http://localhost:5999" } },
        });

        services.Single(d => d.ServiceType == typeof(IPongClientService)).Lifetime.Should().Be(lifetime);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPongClientService>().Should().NotBeNull();
    }

    public static TheoryData<string> Registrations => new()
    {
        nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClient),
        nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientWithRetry),
        nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientAsSingleton),
    };

    [Theory]
    [MemberData(nameof(Registrations))]
    public void ABlankKey_IsRefusedEvenWhenConfigured(string registration)
    {
        var services = new ServiceCollection();
        var options = new GrpcServicesOptions
        {
            Retry = new GrpcRetrySettings { Count = 2, InitialDelayMs = 100 },
            Services = new Dictionary<string, GrpcServiceSettings> { [" "] = new() { Url = "http://localhost:5999" } },
        };

        var register = () => Register(registration, services, options, " ");

        register.Should().Throw<InvalidOperationException>().WithMessage("Missing GrpcService config for: IPongClientService");
    }

    private static void Register(string registration, IServiceCollection services, GrpcServicesOptions options, string? serviceKey = null)
    {
        _ = registration switch
        {
            nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClient) => services.AddCodeFirstGrpcClient<IPongClientService>(options, serviceKey),
            nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientWithRetry) => services.AddCodeFirstGrpcClientWithRetry<IPongClientService>(options, serviceKey),
            nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientAsSingleton) => services.AddCodeFirstGrpcClientAsSingleton<IPongClientService>(options, serviceKey),
            _ => throw new ArgumentOutOfRangeException(nameof(registration), registration, null),
        };
    }
}
