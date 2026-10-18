using System.Text.Json;
using AwesomeAssertions;
using Blocks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class CamelShelfContext(DbContextOptions<CamelShelfContext> options) : ShelfContextBase<CamelShelfContext>(options)
{
    public static readonly JsonSerializerOptions Camel = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override void MapColumns(EntityTypeBuilder<Shelf> builder)
    {
        builder.Property(e => e.Parts).HasJsonCollectionConversion(Camel);
        builder.Property(e => e.Labels).HasJsonReadOnlyListConversion(Camel);
        builder.Property(e => e.Bins).HasJsonListConversion<List<Part>, Part>(Camel);
        builder.ComplexProperty(e => e.Spec, spec =>
        {
            spec.Property(s => s.Tags).HasJsonListConversion(Camel).HasColumnName("SpecTags");
            spec.Property(s => s.Parts).HasJsonListConversion(Camel).HasColumnName("SpecParts");
        });
    }
}

public sealed class JsonColumnOptionsTests : IDisposable
{
    private readonly ShelfDatabase<CamelShelfContext> _database = new(options => new CamelShelfContext(options));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithCamelCaseOptions_StoredTextIsCamelCase()
    {
        var shelf = ShelfSamples.Create(1);
        await SaveAsync(shelf);

        foreach (var column in new[] { "Parts", "Labels", "Bins", "SpecParts" })
        {
            var text = _database.ReadColumn(column, 1);
            text.Should().Contain("\"partName\":").And.Contain("\"count\":").And.NotContain("PartName");
        }

        _database.ReadColumn("Parts", 1).Should().Be(JsonSerializer.Serialize(shelf.Parts, CamelShelfContext.Camel));
        _database.ReadColumn("SpecTags", 1).Should().Be("[\"steel\",\"tall\"]");
    }

    [Fact]
    public async Task WithCamelCaseOptions_EveryColumnReadsBack()
    {
        await SaveAsync(ShelfSamples.Create(1));

        await using var context = _database.CreateContext();
        var shelf = await context.Shelves.SingleAsync(Ct);
        shelf.Should().BeEquivalentTo(ShelfSamples.Create(1));
    }

    [Fact]
    public async Task WithCamelCaseOptions_ReplacedElementAndChangedMember_AreSaved()
    {
        await SaveAsync(ShelfSamples.Create(1));
        await using (var context = _database.CreateContext())
        {
            var shelf = await context.Shelves.SingleAsync(Ct);
            shelf.Parts[1] = new Part { PartName = "washer", Count = 8 };
            shelf.Labels[0].Count = 12;
            shelf.Bins[0].Count = 13;
            shelf.Spec.Parts[0].Count = 14;
            await context.SaveChangesAsync(Ct);
        }

        await using var fresh = _database.CreateContext();
        var saved = await fresh.Shelves.SingleAsync(Ct);
        saved.Parts[1].PartName.Should().Be("washer");
        saved.Labels[0].Count.Should().Be(12);
        saved.Bins[0].Count.Should().Be(13);
        saved.Spec.Parts[0].Count.Should().Be(14);
    }

    private async Task SaveAsync(Shelf shelf)
    {
        await using var context = _database.CreateContext();
        context.Add(shelf);
        await context.SaveChangesAsync(Ct);
    }

    public void Dispose() => _database.Dispose();
}

public sealed class JsonConverterBuilderTests
{
    private static List<Part> Sample() => [new() { PartName = "bolt", Count = 1 }];

    [Fact]
    public void ListBuilderWithoutOptions_WritesTodaysJsonAndReadsItBack()
    {
        var converter = BuilderExtensions.BuildJsonListConvertor<List<Part>>();

        var text = (string)converter.ConvertToProvider(Sample())!;

        text.Should().Be(JsonSerializer.Serialize(Sample()));
        ((List<Part>)converter.ConvertFromProvider(text)!).Should().BeEquivalentTo(Sample());
    }

    [Fact]
    public void ReadOnlyListBuilderWithoutOptions_WritesTodaysJsonAndReadsItBack()
    {
        var converter = BuilderExtensions.BuildJsonReadOnlyListConvertor<Part>();

        var text = (string)converter.ConvertToProvider(Sample())!;

        text.Should().Be(JsonSerializer.Serialize(Sample()));
        ((IReadOnlyList<Part>)converter.ConvertFromProvider(text)!).Should().BeEquivalentTo(Sample());
    }

    [Fact]
    public void BuildersWithOptions_WriteAndReadThroughThoseOptions()
    {
        var list = BuilderExtensions.BuildJsonListConvertor<List<Part>>(CamelShelfContext.Camel);
        var readOnly = BuilderExtensions.BuildJsonReadOnlyListConvertor<Part>(CamelShelfContext.Camel);
        const string camel = "[{\"partName\":\"bolt\",\"count\":1}]";

        list.ConvertToProvider(Sample()).Should().Be(camel);
        readOnly.ConvertToProvider(Sample()).Should().Be(camel);
        ((List<Part>)list.ConvertFromProvider(camel)!).Should().BeEquivalentTo(Sample());
        ((IReadOnlyList<Part>)readOnly.ConvertFromProvider(camel)!).Should().BeEquivalentTo(Sample());
    }
}

public sealed class JsonValueComparerTests
{
    private static List<Part> Sample() => [new() { PartName = "bolt", Count = 1 }];

    [Fact]
    public void EqualText_IsEqualAndHashesAlike()
    {
        var comparer = BuilderExtensions.BuildJsonValueComparer<List<Part>>();

        comparer.Equals(Sample(), Sample()).Should().BeTrue();
        comparer.GetHashCode(Sample()).Should().Be(comparer.GetHashCode(Sample()));
    }

    [Fact]
    public void ChangedMember_IsNotEqual()
    {
        var comparer = BuilderExtensions.BuildJsonValueComparer<List<Part>>();
        var changed = Sample();
        changed[0].Count = 2;

        comparer.Equals(Sample(), changed).Should().BeFalse();
    }

    [Fact]
    public void Snapshot_IsAnEqualCopyThatDoesNotFollowLaterChanges()
    {
        var comparer = BuilderExtensions.BuildJsonValueComparer<List<Part>>();
        var value = Sample();

        var snapshot = comparer.Snapshot(value);
        value[0].Count = 9;

        snapshot.Should().NotBeSameAs(value);
        snapshot[0].Should().NotBeSameAs(value[0]);
        snapshot[0].Count.Should().Be(1);
    }

    [Fact]
    public void WithOptions_ComparesAndCopiesThroughTheTextThoseOptionsWrite()
    {
        var withFields = new JsonSerializerOptions { IncludeFields = true };
        var plain = BuilderExtensions.BuildJsonValueComparer<FieldBox>();
        var comparer = BuilderExtensions.BuildJsonValueComparer<FieldBox>(withFields);
        var first = new FieldBox { Label = "a" };
        var second = new FieldBox { Label = "b" };

        plain.Equals(first, second).Should().BeTrue();
        comparer.Equals(first, second).Should().BeFalse();
        comparer.Snapshot(first).Label.Should().Be("a");
        plain.Snapshot(first).Label.Should().BeNull();
    }
}

public sealed class FieldBox
{
    public string? Label;
}

public sealed class FieldBoxSet
{
    public List<FieldBox> Boxes { get; set; } = [];
}

public sealed class Crate : Entity
{
    public List<FieldBox> Boxes { get; set; } = [];

    public IReadOnlyList<FieldBox> Lids { get; set; } = [];

    public List<FieldBox> Bins { get; set; } = [];

    public FieldBoxSet Set { get; set; } = new();
}

public sealed class CrateContext(DbContextOptions<CrateContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions WithFields = new() { IncludeFields = true };

    public DbSet<Crate> Crates => Set<Crate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Crate>(builder =>
        {
            builder.ToTable("Shelves");
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Boxes).HasJsonCollectionConversion(WithFields);
            builder.Property(e => e.Lids).HasJsonReadOnlyListConversion(WithFields);
            builder.Property(e => e.Bins).HasJsonListConversion<List<FieldBox>, FieldBox>(WithFields);
            builder.ComplexProperty(e => e.Set, set => set.Property(s => s.Boxes).HasJsonListConversion(WithFields));
        });
}

public sealed class JsonColumnComparerOptionsTests : IDisposable
{
    private readonly ShelfDatabase<CrateContext> _database = new(options => new CrateContext(options));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ChangedFieldOfAnElement_IsSavedThroughTheGivenOptions()
    {
        await using (var context = _database.CreateContext())
        {
            context.Add(new Crate
            {
                Id = 1,
                Boxes = [new() { Label = "a" }],
                Lids = new List<FieldBox> { new() { Label = "b" } },
                Bins = [new() { Label = "c" }],
                Set = new FieldBoxSet { Boxes = [new() { Label = "d" }] },
            });
            await context.SaveChangesAsync(Ct);
        }

        await using (var context = _database.CreateContext())
        {
            var crate = await context.Crates.SingleAsync(Ct);
            crate.Boxes[0].Label = "a2";
            crate.Lids[0].Label = "b2";
            crate.Bins[0].Label = "c2";
            crate.Set.Boxes[0].Label = "d2";
            await context.SaveChangesAsync(Ct);
        }

        await using var fresh = _database.CreateContext();
        var saved = await fresh.Crates.SingleAsync(Ct);
        saved.Boxes[0].Label.Should().Be("a2");
        saved.Lids[0].Label.Should().Be("b2");
        saved.Bins[0].Label.Should().Be("c2");
        saved.Set.Boxes[0].Label.Should().Be("d2");
    }

    public void Dispose() => _database.Dispose();
}
