using AwesomeAssertions;
using Blocks.Domain;
using Blocks.Entities;
using Blocks.EntityFrameworkCore.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed record RecordTouched(string Source) : IDomainEvent;

public sealed class GuidKeyedRecord : AggregateRoot<Guid>;

public sealed class IntKeyedRecord : AggregateRoot<int>;

public sealed class TenantRecord : AggregateTenantEntity;

public sealed class ExplicitRecord : Entity<int>, IAggregateRoot<int>
{
    private readonly List<IDomainEvent> _events = [];

    public int CreatedById { get; init; }
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public int? LastModifiedById { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    IReadOnlyList<IDomainEvent> IAggregateRoot<int>.DomainEvents => _events;
    void IAggregateRoot<int>.AddDomainEvent(IDomainEvent eventItem) => _events.Add(eventItem);
    void IAggregateRoot<int>.ClearDomainEvents() => _events.Clear();
}

internal sealed class RecordingPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> Published { get; } = [];

    public Task PublishAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        Published.Add(@event);
        return Task.CompletedTask;
    }
}

internal sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuidKeyedRecord>().Ignore(e => e.DomainEvents);
        modelBuilder.Entity<IntKeyedRecord>().Ignore(e => e.DomainEvents);
        modelBuilder.Entity<TenantRecord>().Ignore(e => e.DomainEvents);
        modelBuilder.Entity<ExplicitRecord>();
    }
}

public sealed class DomainEventDispatchTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly RecordingPublisher _publisher = new();

    public DomainEventDispatchTests()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AsyncSave_PublishesTheEventOfAGuidKeyedAggregate()
    {
        var record = new GuidKeyedRecord { Id = Guid.NewGuid() };
        var touched = new RecordTouched(nameof(GuidKeyedRecord));
        record.AddDomainEvent(touched);

        await SaveAsync(record);

        _publisher.Published.Should().Equal(touched);
        record.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task AsyncSave_PublishesTheEventOfAnIntKeyedAggregateWithoutTheMarker()
    {
        var record = new IntKeyedRecord();
        var touched = new RecordTouched(nameof(IntKeyedRecord));
        record.AddDomainEvent(touched);

        await SaveAsync(record);

        _publisher.Published.Should().Equal(touched);
    }

    [Fact]
    public async Task AsyncSave_PublishesTheEventOfATenantAggregate()
    {
        var record = new TenantRecord { TenantId = 3 };
        var touched = new RecordTouched(nameof(TenantRecord));
        record.AddDomainEvent(touched);

        await SaveAsync(record);

        _publisher.Published.Should().Equal(touched);
    }

    [Fact]
    public async Task AsyncSave_PublishesTheEventOfAnAggregateImplementingTheEventMembersExplicitly()
    {
        var record = new ExplicitRecord();
        IAggregateRoot<int> aggregate = record;
        var touched = new RecordTouched(nameof(ExplicitRecord));
        aggregate.AddDomainEvent(touched);

        await SaveAsync(record);

        _publisher.Published.Should().Equal(touched);
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SyncSave_PublishesNothing()
    {
        var record = new GuidKeyedRecord { Id = Guid.NewGuid() };
        record.AddDomainEvent(new RecordTouched(nameof(GuidKeyedRecord)));
        using var context = CreateContext();
        context.Add(record);

        context.SaveChanges();

        _publisher.Published.Should().BeEmpty();
        record.DomainEvents.Should().ContainSingle();
    }

    public void Dispose() => _connection.Dispose();

    private async Task SaveAsync(object record)
    {
        await using var context = CreateContext();
        context.Add(record);
        await context.SaveChangesAsync(Ct);
    }

    private EventsDbContext CreateContext()
        => new(new DbContextOptionsBuilder<EventsDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new DispatchDomainEventsInterceptor(_publisher))
            .Options);
}
