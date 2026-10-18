using AwesomeAssertions;
using Blocks.Domain;
using FastEndpoints;
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

// Runs on the assembly's one endpoint host (EndpointHost), which the library's static service resolver reads.
public sealed class DomainEventPublisherTests
{
    [Fact]
    public async Task PublishedEvent_ReachesTheHandlerForItsConcreteType()
    {
        IDomainEventPublisher publisher = new DomainEventPublisher();
        var added = new NoteAdded([]);

        await publisher.PublishAsync(added, TestContext.Current.CancellationToken);

        added.Handled.Should().Equal(nameof(NoteAddedHandler));
    }

    [Fact]
    public async Task AnotherEventType_ReachesItsOwnHandler()
    {
        IDomainEventPublisher publisher = new DomainEventPublisher();
        var removed = new NoteRemoved([]);

        await publisher.PublishAsync(removed, TestContext.Current.CancellationToken);

        removed.Handled.Should().Equal(nameof(NoteRemovedHandler));
    }
}
