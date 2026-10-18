using AwesomeAssertions;
using Blocks.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class Ticket : AggregateRoot<Guid, string>
{
    public string Subject { get; set; } = string.Empty;
}

internal sealed class TicketConfiguration : AuditedEntityConfiguration<Ticket, Guid, string>
{
    public override void Configure(EntityTypeBuilder<Ticket> builder)
    {
        base.Configure(builder);
        builder.Ignore(e => e.DomainEvents);
    }
}

internal sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new TicketConfiguration());
}

internal sealed class VersionedTicketConfiguration : AuditedEntityConfiguration<Ticket, Guid, string>
{
    protected override bool HasConcurrencyToken => true;

    public override void Configure(EntityTypeBuilder<Ticket> builder)
    {
        base.Configure(builder);
        builder.Ignore(e => e.DomainEvents);
    }
}

internal sealed class VersionedTicketDbContext(DbContextOptions<VersionedTicketDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new VersionedTicketConfiguration());
}

public sealed class TwoTypeAuditTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TwoTypeAuditTests()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TwoTypeAggregate_SavesAndReadsBackItsAuditFields()
    {
        var createdOn = new DateTime(2026, 10, 1, 8, 30, 0, DateTimeKind.Utc);
        var modifiedOn = new DateTime(2026, 10, 2, 9, 45, 0, DateTimeKind.Utc);
        var ticket = new Ticket
        {
            Subject = "printer",
            CreatedById = "auth0|creator",
            CreatedOn = createdOn,
            LastModifiedById = "auth0|editor",
            LastModifiedOn = modifiedOn,
        };
        await using (var context = CreateContext())
        {
            context.Add(ticket);
            await context.SaveChangesAsync(Ct);
        }

        await using var fresh = CreateContext();
        var saved = await fresh.Set<Ticket>().SingleAsync(Ct);

        saved.Id.Should().Be(ticket.Id);
        saved.CreatedById.Should().Be("auth0|creator");
        saved.CreatedOn.Should().Be(createdOn);
        saved.LastModifiedById.Should().Be("auth0|editor");
        saved.LastModifiedOn.Should().Be(modifiedOn);
    }

    [Fact]
    public void TwoTypeAggregate_HasARequiredCreatorAndAnOptionalLastModifier()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Ticket))!;

        entity.FindProperty(nameof(Ticket.CreatedById))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(Ticket.LastModifiedById))!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void TwoTypeAggregate_DefaultsItsCreationTimeToTheCurrentUtcTime()
    {
        var before = DateTime.UtcNow;
        var ticket = new Ticket();
        var after = DateTime.UtcNow;

        ticket.CreatedOn.Kind.Should().Be(DateTimeKind.Utc);
        ticket.CreatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void TwoTypeConfiguration_AddsNoRowVersionByDefault()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Ticket))!;

        entity.FindProperty("RowVersion").Should().BeNull();
    }

    [Fact]
    public void TwoTypeConfiguration_WithAConcurrencyToken_AddsARowVersion()
    {
        using var context = new VersionedTicketDbContext(
            new DbContextOptionsBuilder<VersionedTicketDbContext>().UseSqlite(_connection).Options);
        var rowVersion = context.Model.FindEntityType(typeof(Ticket))!.FindProperty("RowVersion");

        rowVersion.Should().NotBeNull();
        rowVersion!.ClrType.Should().Be<byte[]>();
        rowVersion.IsConcurrencyToken.Should().BeTrue();
        rowVersion.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void OneParameterAggregate_KeepsANullableLastModifierMemberAndColumn()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var entity = context.Model.FindEntityType(typeof(AuditedNote))!;

        typeof(AuditedNote).GetProperty(nameof(AuditedNote.LastModifiedById))!.PropertyType.Should().Be<int?>();
        entity.FindProperty(nameof(AuditedNote.CreatedById))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(AuditedNote.LastModifiedById))!.IsNullable.Should().BeTrue();
    }

    public void Dispose() => _connection.Dispose();

    private TicketDbContext CreateContext()
        => new(new DbContextOptionsBuilder<TicketDbContext>().UseSqlite(_connection).Options);
}
