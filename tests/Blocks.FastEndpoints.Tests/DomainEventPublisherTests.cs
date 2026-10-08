using AwesomeAssertions;
using Blocks.Domain;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Blocks.FastEndpoints.Tests;

public sealed record NoteAdded(List<string> Handled) : IDomainEvent;

public sealed record NoteRemoved(List<string> Handled) : IDomainEvent;

public sealed class NoteAddedHandler : IEventHandler<NoteAdded>
{
    public Task HandleAsync(NoteAdded eventModel, CancellationToken ct)
    {
        eventModel.Handled.Add(nameof(NoteAddedHandler));
        return Task.CompletedTask;
    }
}

public sealed class NoteRemovedHandler : IEventHandler<NoteRemoved>
{
    public Task HandleAsync(NoteRemoved eventModel, CancellationToken ct)
    {
        eventModel.Handled.Add(nameof(NoteRemovedHandler));
        return Task.CompletedTask;
    }
}

public sealed class PingEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/ping");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) => Send.OkAsync(ct);
}

public sealed class DomainEventPublisherTests
{
    [Fact]
    public async Task PublishedEvent_ReachesTheHandlerForItsConcreteType()
    {
        using var host = await StartHostAsync();
        IDomainEventPublisher publisher = new DomainEventPublisher();
        var added = new NoteAdded([]);

        await publisher.PublishAsync(added, TestContext.Current.CancellationToken);

        added.Handled.Should().Equal(nameof(NoteAddedHandler));
    }

    [Fact]
    public async Task AnotherEventType_ReachesItsOwnHandler()
    {
        using var host = await StartHostAsync();
        IDomainEventPublisher publisher = new DomainEventPublisher();
        var removed = new NoteRemoved([]);

        await publisher.PublishAsync(removed, TestContext.Current.CancellationToken);

        removed.Handled.Should().Equal(nameof(NoteRemovedHandler));
    }

    private static async Task<IHost> StartHostAsync()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddFastEndpoints(options => options.Assemblies = [typeof(DomainEventPublisherTests).Assembly]);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapFastEndpoints());
                }))
            .Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }
}
