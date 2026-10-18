using System.Text.Json;
using AwesomeAssertions;
using Blocks.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class Part
{
    public string PartName { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class ShelfSpec
{
    public IReadOnlyList<string> Tags { get; set; } = [];

    public IReadOnlyList<Part> Parts { get; set; } = [];
}

public sealed class Shelf : Entity
{
    public List<Part> Parts { get; set; } = [];

    public IReadOnlyList<Part> Labels { get; set; } = [];

    public List<Part> Bins { get; set; } = [];

    public ShelfSpec Spec { get; set; } = new();
}

public abstract class ShelfContextBase<TContext>(DbContextOptions<TContext> options) : DbContext(options)
    where TContext : DbContext
{
    public DbSet<Shelf> Shelves => Set<Shelf>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Shelf>(builder =>
        {
            builder.ToTable("Shelves");
            builder.Property(e => e.Id).ValueGeneratedNever();
            MapColumns(builder);
            builder.Property(e => e.Parts).HasColumnName("Parts");
            builder.Property(e => e.Labels).HasColumnName("Labels");
            builder.Property(e => e.Bins).HasColumnName("Bins");
        });

    protected abstract void MapColumns(EntityTypeBuilder<Shelf> builder);
}

public sealed class PlainShelfContext(DbContextOptions<PlainShelfContext> options) : ShelfContextBase<PlainShelfContext>(options)
{
    protected override void MapColumns(EntityTypeBuilder<Shelf> builder)
    {
        builder.Property(e => e.Parts).HasJsonCollectionConversion();
        builder.Property(e => e.Labels).HasJsonReadOnlyListConversion();
        builder.Property(e => e.Bins).HasJsonListConversion<List<Part>, Part>();
        builder.ComplexProperty(e => e.Spec, spec =>
        {
            spec.Property(s => s.Tags).HasJsonListConversion().HasColumnName("SpecTags");
            spec.Property(s => s.Parts).HasJsonListConversion().HasColumnName("SpecParts");
        });
    }
}

internal sealed class ShelfDatabase<TContext> : IDisposable
    where TContext : DbContext
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Func<DbContextOptions<TContext>, TContext> _create;

    public ShelfDatabase(Func<DbContextOptions<TContext>, TContext> create)
    {
        _create = create;
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TContext CreateContext()
        => _create(new DbContextOptionsBuilder<TContext>().UseSqlite(_connection).Options);

    public string ReadColumn(string column, int id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT {column} FROM Shelves WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        return (string)command.ExecuteScalar()!;
    }

    public void Dispose() => _connection.Dispose();
}

internal static class ShelfSamples
{
    public static Shelf Create(int id) => new()
    {
        Id = id,
        Parts = [new() { PartName = "bolt", Count = 1 }, new() { PartName = "nut", Count = 2 }],
        Labels = new List<Part> { new() { PartName = "red", Count = 3 } },
        Bins = [new() { PartName = "left", Count = 4 }],
        Spec = new ShelfSpec
        {
            Tags = ["steel", "tall"],
            Parts = new List<Part> { new() { PartName = "hinge", Count = 5 } },
        },
    };
}

public sealed class JsonColumnTests : IDisposable
{
    private readonly ShelfDatabase<PlainShelfContext> _database = new(options => new PlainShelfContext(options));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithoutOptions_StoredTextIsTodaysJson()
    {
        var shelf = ShelfSamples.Create(1);
        await using (var context = _database.CreateContext())
        {
            context.Add(shelf);
            await context.SaveChangesAsync(Ct);
        }

        _database.ReadColumn("Parts", 1).Should().Be(JsonSerializer.Serialize(shelf.Parts));
        _database.ReadColumn("Labels", 1).Should().Be(JsonSerializer.Serialize(shelf.Labels));
        _database.ReadColumn("Bins", 1).Should().Be(JsonSerializer.Serialize(shelf.Bins));
        _database.ReadColumn("SpecTags", 1).Should().Be(JsonSerializer.Serialize(shelf.Spec.Tags));
        _database.ReadColumn("SpecParts", 1).Should().Be(JsonSerializer.Serialize(shelf.Spec.Parts));
        _database.ReadColumn("Parts", 1).Should().Contain("\"PartName\":\"bolt\"");
    }

    [Fact]
    public async Task WithoutOptions_EveryColumnReadsBack()
    {
        await SaveSampleAsync(1);

        await using var context = _database.CreateContext();
        var shelf = await context.Shelves.SingleAsync(Ct);
        shelf.Should().BeEquivalentTo(ShelfSamples.Create(1));
    }

    [Fact]
    public async Task ReplacedElement_IsSaved()
    {
        await SaveSampleAsync(1);
        await using (var context = _database.CreateContext())
        {
            var shelf = await context.Shelves.SingleAsync(Ct);
            shelf.Parts[1] = new Part { PartName = "washer", Count = 8 };
            shelf.Bins[0] = new Part { PartName = "right", Count = 9 };
            await context.SaveChangesAsync(Ct);
        }

        await using var fresh = _database.CreateContext();
        var saved = await fresh.Shelves.SingleAsync(Ct);
        saved.Parts.Select(p => p.PartName).Should().Equal("bolt", "washer");
        saved.Bins.Single().PartName.Should().Be("right");
    }

    [Fact]
    public async Task ChangedMemberOfAnElement_IsSaved()
    {
        await SaveSampleAsync(1);
        await using (var context = _database.CreateContext())
        {
            var shelf = await context.Shelves.SingleAsync(Ct);
            shelf.Parts[0].Count = 11;
            shelf.Labels[0].Count = 12;
            shelf.Bins[0].Count = 13;
            shelf.Spec.Parts[0].Count = 14;
            await context.SaveChangesAsync(Ct);
        }

        await using var fresh = _database.CreateContext();
        var saved = await fresh.Shelves.SingleAsync(Ct);
        saved.Parts[0].Count.Should().Be(11);
        saved.Labels[0].Count.Should().Be(12);
        saved.Bins[0].Count.Should().Be(13);
        saved.Spec.Parts[0].Count.Should().Be(14);
    }

    [Fact]
    public async Task UnchangedShelf_SavesNothing()
    {
        await SaveSampleAsync(1);

        await using var context = _database.CreateContext();
        _ = await context.Shelves.SingleAsync(Ct);
        context.ChangeTracker.HasChanges().Should().BeFalse();
        (await context.SaveChangesAsync(Ct)).Should().Be(0);
    }

    private async Task SaveSampleAsync(int id)
    {
        await using var context = _database.CreateContext();
        context.Add(ShelfSamples.Create(id));
        await context.SaveChangesAsync(Ct);
    }

    public void Dispose() => _database.Dispose();
}
