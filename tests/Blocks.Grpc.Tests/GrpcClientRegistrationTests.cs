using System.Runtime.Serialization;
using AwesomeAssertions;
using Blocks.Grpc;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using Xunit;

namespace Blocks.Grpc.Tests;

[DataContract]
public sealed class PingRequest
{
    [DataMember(Order = 1)]
    public string Text { get; set; } = "";
}

[DataContract]
public sealed class PingReply
{
    [DataMember(Order = 1)]
    public string Text { get; set; } = "";
}

[Service]
public interface IPingService
{
    ValueTask<PingReply> PingAsync(PingRequest request, CallContext context = default);
}

public sealed class GrpcClientRegistrationTests
{
    public static TheoryData<string> Registrations => new()
    {
        nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClient),
        nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientWithRetry),
        nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientAsSingleton),
    };

    [Theory]
    [MemberData(nameof(Registrations))]
    public void AConfiguredServiceKey_ResolvesAClient(string registration)
    {
        var services = new ServiceCollection();

        Register(registration, services, Options(), "Ping");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPingService>().Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Registrations))]
    public void TheDefaultKey_IsTheClientNameWithoutClientAndService(string registration)
    {
        var services = new ServiceCollection();

        Register(registration, services, Options("IPing"), serviceKey: null);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPingService>().Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Registrations))]
    public void AMissingServiceKey_IsRefused(string registration)
    {
        var services = new ServiceCollection();

        var register = () => Register(registration, services, Options(), "Pong");

        register.Should().Throw<InvalidOperationException>().WithMessage("Missing GrpcService config for: IPingService");
        services.Should().BeEmpty();
    }

    [Fact]
    public void TheSingletonRegistration_SharesOneChannel()
    {
        var services = new ServiceCollection();

        services.AddCodeFirstGrpcClientAsSingleton<IPingService>(Options(), "Ping");

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<GrpcChannel>().Target.Should().Be("localhost:5999");
        provider.GetRequiredService<IPingService>().Should().BeSameAs(provider.GetRequiredService<IPingService>());
    }

    private static GrpcServicesOptions Options(string key = "Ping") => new()
    {
        Retry = new GrpcRetrySettings { Count = 2, InitialDelayMs = 100 },
        Services = new Dictionary<string, GrpcServiceSettings>
        {
            [key] = new() { Url = "http://localhost:5999", EnableRetry = true },
        },
    };

    private static void Register(string registration, IServiceCollection services, GrpcServicesOptions options, string? serviceKey)
    {
        _ = registration switch
        {
            nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClient) => services.AddCodeFirstGrpcClient<IPingService>(options, serviceKey),
            nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientWithRetry) => services.AddCodeFirstGrpcClientWithRetry<IPingService>(options, serviceKey),
            nameof(GrpcClientRegistrationExtensions.AddCodeFirstGrpcClientAsSingleton) => services.AddCodeFirstGrpcClientAsSingleton<IPingService>(options, serviceKey),
            _ => throw new ArgumentOutOfRangeException(nameof(registration), registration, null),
        };
    }
}
